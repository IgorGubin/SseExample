using NLog;
using Server.Utilities;

namespace Server.HostedServices
{
    public class LifecycleHostedService : IHostedService, IHostedLifecycleService
    {
        private static readonly NLog.ILogger Logger = LogManager.GetCurrentClassLogger();
        private static DateTime _appStart;

        public Task StartingAsync(CancellationToken cancellationToken)
        {
            _appStart = DateTime.Now;
            Logger.Info($"\r\n{new string('*', 33)}\r\n------->>> Server - Start {"{{{"} <<<-------");
            Logger.Info($"\r\n{new string('=', 33)}"
                      + $"\r\nUrl: \"{_cfg.Url}\""
                      + $"\r\n{new string('=', 33)}");
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StartedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StoppingAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.Info($"\r\n{new string('*', 33)}\r\n------->>> Server - End[{Environment.ExitCode}:{(DateTime.Now - _appStart)}] {"}}}"} <<<-------");
            return Task.CompletedTask;
        }

        public Task StoppedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
