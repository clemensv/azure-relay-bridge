// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Relay.Bridge.Configuration
{
    using McMaster.Extensions.CommandLineUtils;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using McMaster.Extensions.CommandLineUtils.Abstractions;
    using McMaster.Extensions.CommandLineUtils.HelpText;

    public class CommandLineSettings
    {
#if NET48
        [Option(CommandOptionType.NoValue, LongName = "svcinstall", ShortName = "I", Description = "Install as Windows Service")]
        public bool? ServiceInstall { get; set; }
        [Option(CommandOptionType.NoValue, LongName = "svcuninstall", ShortName = "U", Description = "Uninstall Windows Service")]
        public bool? ServiceUninstall { get; set; }
        [Option(CommandOptionType.NoValue, LongName = "svc", ShortName = "svc", Description = "Reserved for Windows service control manager")]
        public bool? ServiceRun { get; set; }
#endif
        [Option(CommandOptionType.SingleValue, ShortName = "b", Description = "Source address of forwarding connections.")]
        public string BindAddress { get; set; }
        [Option(CommandOptionType.SingleValue, ShortName = "e", Description = "Relay endpoint URI")]
        public Uri EndpointUri { get; set; }
        [Option(CommandOptionType.SingleValue, ShortName = "f", Description = "Configuration file")]
        public string ConfigFile { get; set; }
        [Option(CommandOptionType.SingleValue, ShortName = "l", Description = "Log file")]
        public string LogFile { get; set; }
        [Option(CommandOptionType.NoValue, ShortName = "g", Description = "Allows remote hosts to connect to local forwarded ports")]
        public bool? GatewayPorts { get; set; }
        [Option(CommandOptionType.SingleValue, ShortName = "K", Description = "Azure Relay shared access policy name")]
        public string SharedAccessKeyName { get; set; }
        [Option(CommandOptionType.SingleValue, ShortName = "k", Description = "Azure Relay shared access policy key")]
        public string SharedAccessKey { get; set; }
        [Option(CommandOptionType.MultipleValue, ShortName = "L", Description = "Local forwarder [address:]port:relay_name")]
        public IEnumerable<string> LocalForward { get; set; }
        [Option(CommandOptionType.MultipleValue, ShortName = "o", Description = "Configuration file option override key:value")]
        public IEnumerable<string> Option { get; set; }
        [Option(CommandOptionType.NoValue, ShortName = "q", Description = "No log output to stdout/stderr")]
        public bool? Quiet { get; set; }
        [Option(CommandOptionType.MultipleValue, ShortName = "R", Description = "Remote forwarder relay_name:[address:]port ")]
        public IEnumerable<string> RemoteForward { get; set; }
        [Option(CommandOptionType.SingleValue, ShortName = "s", Description = "Azure Relay shared access signature token")]
        public string Signature { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "x", Description = "Azure Relay connection string (overridden with -S -K -k -E)")]
        public string ConnectionString { get; internal set; }

        [Option(CommandOptionType.NoValue, ShortName = "v", Description = "Verbose log output")]
        public bool? Verbose { get; internal set; }


        public static void Run(string[] args, Func<CommandLineSettings, int> callback)
        {
            CommandLineApplication<CommandLineSettings> app = new CommandLineApplication<CommandLineSettings>();
            app.ModelFactory = () => new CommandLineSettings();
            app.Conventions.UseDefaultConventions().SetAppNameFromEntryAssembly();
            app.Parse(args);
            callback(app.Model);
        }
    }
}
