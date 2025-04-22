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

            ClientSessionId = Guid.NewGuid().ToString("N");

            WaitFileFromServerTimeoutMin = int.TryParse(cfg["WaitFileFromServerTimeoutMin"], out int tmp) ? tmp : 15;
        }

        public static string? SrvApiUrl { get; private set; }

        public static string ClientSessionId { get; private set; }

        public static int WaitFileFromServerTimeoutMin { get; private set; }

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
