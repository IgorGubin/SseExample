using Client.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Client.Utilities
{
    internal static partial class _cfg
    {
        static _cfg()
        {
            var cfg = new ConfigurationBuilder()
                .Add(new JsonConfigurationSource { Path = "config.json" })
                .Build();

            SrvApiUrl = cfg["srvApiUrl"];

            WaitFileFromServerTimeoutMin = int.TryParse(cfg["waitFileFromServerTimeoutMin"], out int tmp) ? tmp : 15;

            var curSessionId = cfg["curSessionId"];

            var sessionDataList = cfg.GetSection("data").Get<List<SessionData>>();

            SessionData = sessionDataList.FirstOrDefault(i => i.SessionId == curSessionId);
        }

        public static string? SrvApiUrl { get; private set; }

        public static int WaitFileFromServerTimeoutMin { get; private set; }

        public static SessionData? SessionData { get; private set; }

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
