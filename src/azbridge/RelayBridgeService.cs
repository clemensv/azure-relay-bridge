// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET48
namespace azbridge
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
#if USE_MDT_EVENTSOURCE
    using Microsoft.Diagnostics.Tracing;
#else
    using System.Diagnostics.Tracing;
#endif
    using System.IO;
    using System.ServiceProcess;
    using System.Threading;
    using Microsoft.Azure.Relay.Bridge;
    using Microsoft.Azure.Relay.Bridge.Configuration;
    using Microsoft.Extensions.Logging;

    public partial class RelayBridgeService : ServiceBase
    {
        Host host;
        ILogger logger = null;

        public RelayBridgeService()
        {
            InitializeComponent();
        }

        public Host Host { get => host; }

        protected override void OnStart(string[] args)
        {
            CommandLineSettings.Run(args, Run);
        }

        int Run(CommandLineSettings settings)
        {
            string svcConfigFileName =
                (Environment.OSVersion.Platform == PlatformID.Unix) ?
                    $"/etc/azbridge/azbridge_config.svc.yml" :
             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                 $"Microsoft\\Azure Relay Bridge\\azbridge_config.svc.yml");

            if (File.Exists(svcConfigFileName))
            {
                settings.ConfigFile = svcConfigFileName;
            }
            Config config = Config.LoadConfig(settings);

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
            var loggerFactory = new LoggerFactory();          
            if ( !string.IsNullOrEmpty(config.LogFileName) )
            {
                loggerFactory.AddFile(config.LogFileName, logLevel);
            }                                      
            logger = loggerFactory.CreateLogger("azbridge");
            
            DiagnosticListener.AllListeners.Subscribe(new SubscriberObserver(logger));
                
            host = new Host(config);
            host.Start();
            return 0;
        }

        protected override void OnStop()
        {
            try
            {
                host?.Stop();
            }
            finally
            {
                host = null;
            }
        }
    }
                    
    
}
#endif
