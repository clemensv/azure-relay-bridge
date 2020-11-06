// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET48
namespace azbridge
{
    using System;
    using System.Collections;
    using System.Configuration;
    using System.Configuration.Install;
    using System.IO;
    using System.ServiceProcess;

    static class ServiceLauncher
    {
        const string ServiceName = "azbridgesvc";        

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        internal static void Run(string[] args)
        {
            var hcs = new RelayBridgeService();

            if (IsInstalled())
            {
                ServiceBase[] servicesToRun = new ServiceBase[] { hcs };
                ServiceBase.Run(servicesToRun);
            }
            else
            {
                Console.WriteLine(Strings.ServiceIsNotInstalled);
            }              
            
        }

        /// <summary>
        /// Installs the service
        /// </summary>
        internal static void InstallService()
        {
            if (IsInstalled())
                return;

            try
            {
                using (AssemblyInstaller installer = GetInstaller())
                {
                    IDictionary state = new Hashtable();
                    try
                    {
                        installer.Install(state);
                        installer.Commit(state);
                    }
                    catch
                    {
                        try
                        {
                            installer.Rollback(state);
                        }
                        catch { }
                        throw;
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        internal static void UninstallService()
        {
            if (!IsInstalled())
                return;
            using (AssemblyInstaller installer = GetInstaller())
            {
                IDictionary state = new Hashtable();
                installer.Uninstall(state);
            }
        }

        internal static void StartService()
        {
            if (!IsInstalled())
                return;

            using (ServiceController controller =
                new ServiceController(ServiceName))
            {
                if (controller.Status != ServiceControllerStatus.Running)
                {
                    controller.Start();
                    controller.WaitForStatus(ServiceControllerStatus.Running,
                        TimeSpan.FromSeconds(10));
                }
            }
        }

        internal static bool IsInstalled()
        {
            using (ServiceController controller = new ServiceController(ServiceName))
            {
                try
                {
                    ServiceControllerStatus status = controller.Status;
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        internal static bool IsRunning()
        {
            using (var controller = new ServiceController(ServiceName))
            {
                if (!IsInstalled())
                    return false;
                return (controller.Status == ServiceControllerStatus.Running);
            }
        }

        internal static AssemblyInstaller GetInstaller()
        {
            AssemblyInstaller installer = new AssemblyInstaller(
                typeof(RelayBridgeService).Assembly, null) {UseNewContext = true};
            return installer;
        }

        internal static void StopService()
        {
            if (!IsInstalled())
                return;
            using (ServiceController controller =
                new ServiceController(ServiceName))
            {
                try
                {
                    if (controller.Status != ServiceControllerStatus.Stopped)
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped,
                             TimeSpan.FromSeconds(10));
                    }
                }
                catch
                {
                    throw;
                }
            }
        }
    }
}
#endif