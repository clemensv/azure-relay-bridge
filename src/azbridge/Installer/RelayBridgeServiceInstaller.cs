// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

#if NET48
namespace azbridge
{
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.ServiceProcess;

    /// <summary>
    /// Installer class for installing HybridConnectionManager as NT Service.
    /// </summary>
    [RunInstaller(true)]
    public class RelayBridgeServiceInstaller : System.Configuration.Install.Installer
    {
        public const string ServiceName = "azbridgesvc";

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            this.Init();
            base.Install(stateSaver);
        }

        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            this.Init();
            base.Uninstall(savedState);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope",
            Justification =
                "serviceInstaller and processInstaller are used internally to install the service. They live for the duration of the installation."
            )]
        void Init()
        {
            var processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.NetworkService
            };

            var serviceInstaller = new ServiceInstaller
            {
                StartType = ServiceStartMode.Automatic,
                DelayedAutoStart = true,
                ServiceName = RelayBridgeServiceInstaller.ServiceName,
                DisplayName = RelayBridgeServiceInstaller.ServiceName,
            };

            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }

        protected override void OnBeforeInstall(System.Collections.IDictionary savedState)
        {
            //Get the existing assembly path parameter
            StringBuilder path = new StringBuilder(Context.Parameters["assemblypath"]);

            //Wrap the existing path in quotes if it isn't already
            if (!path[0].Equals("\""))
            {
                path.Insert(0, "\"");
                path.Append("\"");
            }

            //Add desired parameters
            path.Append(" --svc");

            //Set the new path
            Context.Parameters["assemblypath"] = path.ToString();
        }
    }
}
#endif