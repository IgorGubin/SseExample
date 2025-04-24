using NLog;
using NLog.Extensions.Logging;

using Client.Enums;
using Client.Utilities;
using Client.Processing;

namespace Client
{
    internal class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private static AppMainStatusEnum appStatus = AppMainStatusEnum.Running;

        private static async Task Main(string[] args)
        {
            var retCode = 0;

            LogManager.ResumeLogging();
            LogManager.Configuration = new NLogLoggingConfiguration(_cfg.GetNlogConfiguration());

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Logger.Info($"\r\n{new string('*', 77)}\r\n------->>> SSE.Client.Main - Start {"{{{"} <<<-------");

            var cancelationTokenSource = new CancellationTokenSource();

            try
            {
                Logger.Info($"\r\n{new string('=', 33)}" 
                          + $"\r\nUrl: \"{_cfg.SrvApiUrl}\""
                          + $"\r\nSessionId: \"{_cfg.SessionData.SessionId}\""
                          + $"\r\nWaitFileFromServerTimeoutMin: \"{_cfg.WaitFileFromServerTimeoutMin}\""
                          + $"\r\n{new string('=', 33)}");

                var taskStatePolling = WebClientApiSse.StatePolling(_cfg.SrvApiUrl, cancelationTokenSource.Token);

                retCode = await WebClientApiSse.Do(_cfg.SessionData.SessionId, _cfg.SrvApiUrl, cancelationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                retCode = 1;
            }
            finally
            {
                appStatus = AppMainStatusEnum.Completed;
                Logger.Info($"\r\n{new string('*', 77)}\r\n------->>> SSE.Client.Main - End {"}}}"} <<<-------");
            }

            Environment.Exit(retCode);
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (appStatus != AppMainStatusEnum.Completed || e.ExceptionObject is not NLogRuntimeException)
            {
                try
                {
                    Logger.Fatal(e.ExceptionObject); // output to file only
                }
                catch { }

                // output to console
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e.ExceptionObject?.ToString() ?? $"Fatal Error {sender}");
                Console.ResetColor();
            }

            Environment.Exit(2);
        }
    }
}