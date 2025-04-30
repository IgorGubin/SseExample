using NLog;

namespace Server.Processing
{
    public class CoveyorHostedService : BackgroundService
    {
        private static readonly NLog.ILogger Logger = LogManager.GetCurrentClassLogger();

        public CoveyorHostedService(IWebHostEnvironment env)
        {
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            try
            {
                await Conveyor.HandleAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "\r\nError by starting the conveyor.");
            }
        }
    }
}
