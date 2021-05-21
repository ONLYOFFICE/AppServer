/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/


using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Common.Utils;
using ASC.Core;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ASC.Radicale
{
    [Scope]
    public class RadicaleServiceLauncher : IHostedService
    {
        private Process Proc { get; set; }
        private ProcessStartInfo StartInfo { get; set; }
        private ILog Logger { get; set; }
        //private ConfigurationExtension ConfigurationExtension { get; }
        //private IHostEnvironment HostEnvironment { get; set; }

        public RadicaleServiceLauncher(
            IOptionsMonitor<ILog> options,
            ConfigurationExtension configurationExtension,
            IHostEnvironment hostEnvironment)
        {
            Logger = options.CurrentValue;
            //ConfigurationExtension = configurationExtension;
            //HostEnvironment = hostEnvironment;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var pythonName = "python";

                var configPath = Path.GetFullPath(CrossPlatform.PathCombine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "radicale.config"));

                if (WorkContext.IsMono)
                {
                    pythonName = "python3";
                }

                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = false,
                    UseShellExecute = false,
                    FileName = pythonName,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = $"-m radicale --config \"{configPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                };

                StartRedicale();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopRadicale();
            return Task.CompletedTask;
        }

        private void StartRedicale()
        {
            StopRadicale();
            Proc = Process.Start(StartInfo);
        }

        private void StopRadicale()
        {
            try
            {
                if (Proc != null && !Proc.HasExited)
                {
                    Proc.Kill();
                    if (!Proc.WaitForExit(10000))
                    {
                        Logger.Warn("The process does not wait for completion.");
                    }
                    Proc.Close();
                    Proc.Dispose();
                    Proc = null;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Radicale failed stop", e);
            }
        }
    }
}
