using NLog;
using NLog.Extensions.Logging;

using Server.Enums;
using Server.ActionsFilters;
using Server.Utilities;
using Server.Processing;

namespace Server
{
    internal class Program
    {
        private static readonly NLog.ILogger Logger = LogManager.GetCurrentClassLogger();

        private static AppMainStatusEnum appStatus = AppMainStatusEnum.Running;

        private static DateTime _appStart;

        private static void Main(string[] args)
        {
            _appStart = DateTime.Now;

            LogManager.ResumeLogging();
            LogManager.Configuration = new NLogLoggingConfiguration(_cfg.GetNlogConfiguration());

            Logger.Info($"\r\n{new string('*', 33)}\r\n------->>> Server - Start {"{{{"} <<<-------");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Logger.Info($"\r\n{new string('=', 33)}"
                      + $"\r\nUrl: \"{_cfg.Url}\""
                      + $"\r\n{new string('=', 33)}");

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddControllers(o => {
                o.Filters.Add<HttpResponseExceptionsFilter>();
            });
            builder.Services.AddHostedService<CoveyorHostedService>();

            var app = builder.Build();

            app.MapControllers();
            try
            {
                app.Run();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Environment.ExitCode = 1;
            }
            finally
            {
                appStatus = AppMainStatusEnum.Completed;
                try { LogManager.Shutdown(); } catch { }
            }
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (e.ExceptionObject as Exception) ?? new ApplicationException("Fatal Error!");
            if (appStatus != AppMainStatusEnum.Completed || e.ExceptionObject is not NLogRuntimeException)
            {
                try { Logger.Fatal(ex); } catch { }
            }
            try { LogManager.Shutdown(); } catch { }
            Environment.Exit(2);
        }
    }
}
