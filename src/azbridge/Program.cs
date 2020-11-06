// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace azbridge
{
    using System;
    using System.IO;
    using System.Threading;
    using Microsoft.Azure.Relay.Bridge;
    using Microsoft.Azure.Relay.Bridge.Configuration;
    using System.Diagnostics;
#if USE_MDT_EVENTSOURCE
    using Microsoft.Diagnostics.Tracing;
#else
    using System.Diagnostics.Tracing;
#endif
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Azure.Relay;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;

    partial class Program
    {
        static ILogger logger = null;

        static void Main(string[] args)
        {
            // before we localize, make sure we have all the error
            // messages in en-us
            CultureInfo.CurrentUICulture =
                CultureInfo.DefaultThreadCurrentUICulture =
                    CultureInfo.GetCultureInfoByIetfLanguageTag("en-us");

            try
            { 
                CommandLineSettings.Run(args, (c) => Run(c, args));
            }
            catch(CommandParsingException exception)
            {
                Console.WriteLine(exception.Message);
            }
            catch (ConfigException exception)
            {
                Console.WriteLine($"{exception.FileName}: {exception.Message}");
            }
        }

        static int Run(CommandLineSettings settings, string[] args)
        {
            try
            {
#if NET48
                if (settings.ServiceInstall.HasValue && settings.ServiceInstall.Value)
                {
                    ServiceLauncher.InstallService();
                    return 0;
                }
                else if (settings.ServiceUninstall.HasValue && settings.ServiceUninstall.Value)
                {
                    ServiceLauncher.UninstallService();
                    return 0;
                }
                else if (settings.ServiceRun.HasValue && settings.ServiceRun.Value)
                {
                    ServiceLauncher.Run(args);
                    return 0;
                }
#endif

                if ( !string.IsNullOrEmpty(settings.ConfigFile) && !File.Exists(settings.ConfigFile))
                {
                    Console.WriteLine($"The config file was not found: {settings.ConfigFile}");
                    return 3;
                }

                Config config = Config.LoadConfig(settings);
                if (config.LocalForward.Count == 0 &&
                     config.RemoteForward.Count == 0)
                {
                    Console.WriteLine("You must specify at least one -L or -R forwarder.");
                    return 2;
                }

                var globalCxn = config.AzureRelayConnectionString;
                if ( globalCxn == null &&
                    (config.LocalForward.Any((f)=>f.ConnectionString == null) ||
                    config.RemoteForward.Any((f) => f.ConnectionString == null)))
                {
                    Console.WriteLine("Connection string(s) undefined; -x/AzureRelayConnectionString. azbridge -h for help.");

                    return 3;
                }

                LogLevel logLevel = LogLevel.Error;
                if (!settings.Quiet.HasValue || !settings.Quiet.Value)
                {
                    if (config.LogLevel != null)
                    {
                        switch (config.LogLevel.ToUpper())
                        {
                            case "QUIET":
                                logLevel = LogLevel.None;
                                break;
                            case "FATAL":
                                logLevel = LogLevel.Critical;
                                break;
                            case "ERROR":
                                logLevel = LogLevel.Error;
                                break;
                            case "INFO":
                                logLevel = LogLevel.Information;
                                break;
                            case "VERBOSE":
                                logLevel = LogLevel.Trace;
                                break;
                            case "DEBUG":
                            case "DEBUG1":
                            case "DEBUG2":
                            case "DEBUG3":
                                logLevel = LogLevel.Debug;
                                break;
                        }
                    }
                }
                else
                {
                    logLevel = LogLevel.None;
                }

                var loggerFactory = LoggerFactory.Create((builder) =>
                {
                    if (string.IsNullOrEmpty(config.LogFileName))
                    {
                        builder.AddConsole();
                    }
                });
                if (!string.IsNullOrEmpty(config.LogFileName))
                {
                    loggerFactory.AddFile(config.LogFileName, logLevel);
                }
                logger = loggerFactory.CreateLogger("azbridge");
                DiagnosticListener.AllListeners.Subscribe(new SubscriberObserver(logger));

                Host host = new Host(config);
                host.Start();

                EventWaitHandle _closing = new EventWaitHandle(false, EventResetMode.AutoReset);
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    host.Stop();
                    loggerFactory.Dispose();
                    _closing.Set();
                };
                _closing.WaitOne();
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Configuration file not found:" + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return 0;
        }
    }
}
