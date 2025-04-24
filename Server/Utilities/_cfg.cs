using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Server.Model;

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

            WaitPeriodMs = int.TryParse(cfg["WaitPeriodMs"], out int tmp) ? tmp : 2000;

            SessionDataList = cfg.GetSection("data").Get<List<SessionData>>();
        }

        public static string Url { get; private set; }

        public static int WaitPeriodMs { get; private set; }

        public static List<SessionData>? SessionDataList { get; private set; }


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
