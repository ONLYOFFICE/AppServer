using System;
using System.Threading;
using System.Threading.Tasks;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Mail.Aggregator.CollectionService.Console;
using ASC.Mail.Aggregator.CollectionService.Service;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ASC.Mail.Aggregator.CollectionService
{
    [Singletone]
    class AggregatorServiceLauncher : IHostedService
    {
        private ILog Log { get; }
        private AggregatorService AggregatorService { get; }
        private ConsoleParameters ConsoleParameters { get; }

        private ManualResetEvent ResetEvent;

        public AggregatorServiceLauncher(
            IOptionsMonitor<ILog> options,
            AggregatorService aggregatorService,
            ConsoleParser consoleParser)
        {
            Log = options.Get("ASC.Mail.MainThread");
            AggregatorService = aggregatorService;
            ConsoleParameters = consoleParser.GetParsedParameters();

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.FatalFormat("Unhandled exception: {0}", e.ExceptionObject.ToString());
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                Log.Info("Start service\r\n");

                if (Environment.UserInteractive || ConsoleParameters.IsConsole)
                {
                    Log.Info("Service Start in console-daemon mode");
                    AggregatorService.StartTimer(true);
                    ResetEvent = new ManualResetEvent(false);
                    System.Console.CancelKeyPress += (sender, e) => StopAsync(cancellationToken);
                    ResetEvent.WaitOne();
                }
                else
                {
                    AggregatorService.StartTimer(true);
                }
            }
            catch (Exception ex)
            {
                Log.FatalFormat("Unhandled exception: {0}", ex.ToString());
                StopAsync(cancellationToken);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                Log.Info("Stoping service\r\n");

                AggregatorService.StopService();

            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Stop service Error: {0}\r\n", ex.ToString());
            }
            finally
            {
                Log.Info("Stop service\r\n");

                if (ResetEvent != null)
                    ResetEvent.Set();
            }

            return Task.CompletedTask;
        }
    }
}
