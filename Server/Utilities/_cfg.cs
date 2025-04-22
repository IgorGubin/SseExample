using Microsoft.Extensions.Configuration.Json;

namespace Server.Utilities
{
    public class _cfg
    {
        static _cfg()
        {
            var appSettingsFileName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                ? "appsettings.Development.json"
                : "appsettings.json";

            IConfigurationRoot cfg = new ConfigurationBuilder()
                .Add(new JsonConfigurationSource { Path = appSettingsFileName })
                .Build();

            Url = cfg["Url"];

            ConsumptionMaxParallelDegree = int.TryParse(cfg["ConsumptionMaxParallelDegree"], out int tmp) ? tmp : Environment.ProcessorCount;
            WaitWhenAnyMs = int.TryParse(cfg["WaitWhenAnyMs"], out tmp) ? tmp : 500;
        }

        public static string Url { get; private set; }

        public static int ConsumptionMaxParallelDegree { get; private set; }

        public static int WaitWhenAnyMs { get; private set; }

        public static IConfigurationSection GetNlogConfiguration()
        {
            var cfg = new ConfigurationBuilder()
                    .Add(new JsonConfigurationSource { Path = "nlog.json" })
                    .Build();

            var res = cfg.GetSection("nlog");
            return res;
        }
    }
}
