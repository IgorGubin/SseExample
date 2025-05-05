using NLog;
using NLog.Extensions.Logging;

using Server.Enums;
using Server.ActionsFilters;
using Server.Utilities;
using Server.HostedServices;

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

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddControllers(o => {
                o.Filters.Add<HttpResponseExceptionsFilter>();
            });
            builder.Services.Configure<HostOptions>(options =>
            {
                options.ServicesStartConcurrently = true;
                options.ServicesStopConcurrently = true;
            });
            builder.Services.AddHostedService<CoveyorHostedService>();
            builder.Services.AddHostedService<LifecycleHostedService>();

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
                LogManager.Shutdown();
            }
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (e.ExceptionObject as Exception) ?? new ApplicationException("Fatal Error!");
            if (appStatus != AppMainStatusEnum.Completed || ex is not NLogRuntimeException)
            {
                Logger.Fatal(ex);
            }
            LogManager.Shutdown();
            Environment.Exit(2);
        }
    }
}
