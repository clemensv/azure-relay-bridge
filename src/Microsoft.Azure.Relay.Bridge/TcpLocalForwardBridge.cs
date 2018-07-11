﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Relay.Bridge
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using Microsoft.Azure.Relay;

    sealed class TcpLocalForwardBridge : IDisposable
    {
        private readonly Config config;
        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        readonly HybridConnectionClient hybridConnectionClient;
        private EventTraceActivity listenerActivity;
        Task<Task> acceptSocketLoop;

        TcpListener tcpListener;

        public TcpLocalForwardBridge(Config config, RelayConnectionStringBuilder connectionString)
        {
            this.config = config;
            this.hybridConnectionClient = new HybridConnectionClient(connectionString.ToString());
        }

        public event EventHandler NotifyException;

        public DateTime LastAttempt { get; private set; }

        public Exception LastError { get; private set; }

        internal bool IsOpen { get; private set; }

        public HybridConnectionClient HybridConnectionClient => hybridConnectionClient;

        public static TcpLocalForwardBridge FromConnectionString(Config config,
            RelayConnectionStringBuilder connectionString)
        {
            return new TcpLocalForwardBridge(config, connectionString);
        }

        public void Close()
        {
            BridgeEventSource.Log.LocalForwardListenerStopping(listenerActivity, tcpListener);

            try
            {
                if (!this.IsOpen)
                {
                    throw BridgeEventSource.Log.ThrowingException(new InvalidOperationException(), this);
                }
                this.IsOpen = false;
                this.cancellationTokenSource.Cancel();
                this.tcpListener?.Stop();
                this.tcpListener = null;
            }
            catch (Exception ex)
            {
                BridgeEventSource.Log.LocalForwardListenerStoppingFailed(listenerActivity, ex);
                throw;
            }
            BridgeEventSource.Log.LocalForwardListenerStop(listenerActivity, tcpListener);
        }

        public void Dispose()
        {
            this.Close();
        }

        public IPEndPoint GetIpEndPoint()
        {
            return this.tcpListener?.LocalEndpoint as IPEndPoint;
        }

        public void Run(IPEndPoint listenEndpoint)
        {
            if (this.IsOpen)
            {
                throw BridgeEventSource.Log.ThrowingException(new InvalidOperationException(), this);
            }

            this.listenerActivity = BridgeEventSource.NewActivity("LocalForwardListener");

            try
            {
                this.IsOpen = true;
                BridgeEventSource.Log.LocalForwardListenerStarting(listenerActivity, listenEndpoint);
                this.tcpListener = new TcpListener(listenEndpoint);
                this.tcpListener.Start();
                this.acceptSocketLoop = Task.Factory.StartNew(AcceptSocketLoopAsync);
                this.acceptSocketLoop.ContinueWith(AcceptSocketLoopFaulted, TaskContinuationOptions.OnlyOnFaulted);
                BridgeEventSource.Log.LocalForwardListenerStart(listenerActivity, tcpListener);
            }
            catch (Exception exception)
            {
                BridgeEventSource.Log.LocalForwardListenerStartFailed(listenerActivity, exception);
                this.LastError = exception;
                throw;
            }
        }

        async Task AcceptSocketLoopAsync()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                var socketActivity = BridgeEventSource.NewActivity("LocalForwardSocket");
                var socket = await this.tcpListener.AcceptTcpClientAsync();
                var endpointInfo = socket.Client.LocalEndPoint.ToString();
                BridgeEventSource.Log.LocalForwardSocketAccepted(socketActivity, socket);

                this.LastAttempt = DateTime.Now;

                BridgeSocketConnectionAsync(socket)
                    .ContinueWith((t, s) =>
                    {
                        if (t.Exception != null)
                        {
                            BridgeEventSource.Log.LocalForwardSocketError(socketActivity, endpointInfo, t.Exception);
                        }
                        socket.Dispose();
                    }, TaskContinuationOptions.OnlyOnFaulted)
                    .ContinueWith((t, s) =>
                    {

                        try
                        {
                            BridgeEventSource.Log.LocalForwardSocketComplete(socketActivity, endpointInfo);
                            socket.Close();
                            BridgeEventSource.Log.LocalForwardSocketClosed(socketActivity, endpointInfo);
                        }
                        catch (Exception e)
                        {
                            if (Fx.IsFatal(e))
                            {
                                throw;
                            }
                            BridgeEventSource.Log.LocalForwardSocketCloseFailed(socketActivity, endpointInfo, e);
                            socket.Dispose();
                        }
                    }, TaskContinuationOptions.OnlyOnRanToCompletion)
                    .Fork();
            }
        }

        void AcceptSocketLoopFaulted(Task<Task> t)
        {
            BridgeEventSource.Log.LocalForwardSocketAcceptLoopFailed(listenerActivity, t.Exception);
            this.LastError = t.Exception;
            this.NotifyException?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        async Task BridgeSocketConnectionAsync(TcpClient tcpClient)
        {
            EventTraceActivity bridgeActivity = BridgeEventSource.NewActivity("LocalForwardBridgeConnection");
            try
            {
                BridgeEventSource.Log.LocalForwardBridgeConnectionStarting(bridgeActivity, tcpClient, HybridConnectionClient);

                tcpClient.SendBufferSize = tcpClient.ReceiveBufferSize = 65536;
                tcpClient.SendTimeout = 60000;
                var endpointInfo = tcpClient.Client.LocalEndPoint.ToString();
                var tcpstream = tcpClient.GetStream();
                var socket = tcpClient.Client;
                
                using (var hybridConnectionStream = await HybridConnectionClient.CreateConnectionAsync())
                {
                    // read and write 4-byte header
                    hybridConnectionStream.WriteTimeout = 60000;
                    byte[] preamble =
                    {
                        /*major*/ 1, 
                        /*minor*/ 0, 
                        /*features*/ 0,
                        /*reserved*/ 0
                    };
                    await hybridConnectionStream.WriteAsync(preamble, 0, preamble.Length);
                    for (int read = 0; read < preamble.Length; read += await hybridConnectionStream.ReadAsync(preamble, read, preamble.Length - read));


                    BridgeEventSource.Log.LocalForwardBridgeConnectionStart(bridgeActivity, tcpClient, HybridConnectionClient);

                    try
                    {
                        await Task.WhenAll(
                            StreamPump.RunAsync(hybridConnectionStream, tcpstream,
                                () => socket.Shutdown(SocketShutdown.Send), cancellationTokenSource.Token)
                                .ContinueWith((t)=>cancellationTokenSource.Cancel(), TaskContinuationOptions.OnlyOnFaulted),
                            StreamPump.RunAsync(tcpstream, hybridConnectionStream,
                                () => hybridConnectionStream?.Shutdown(), cancellationTokenSource.Token))
                                .ContinueWith((t) => cancellationTokenSource.Cancel(), TaskContinuationOptions.OnlyOnFaulted);
                    }
                    catch
                    {
                        hybridConnectionStream.Dispose();
                        socket.Disconnect(false);
                        tcpstream.Dispose();
                        throw;
                    }

                    using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                    {
                        await hybridConnectionStream.CloseAsync(cts.Token);
                    }                
                }
                BridgeEventSource.Log.LocalForwardBridgeConnectionStop(bridgeActivity, endpointInfo, HybridConnectionClient);
            }
            catch (Exception e)
            {
                BridgeEventSource.Log.LocalForwardBridgeConnectionFailed(bridgeActivity, e);
            }
        }
    }
}