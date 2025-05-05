using System.Collections.Concurrent;
using System.Threading.Channels;

using NLog;

using Server.Interfaces;
using Server.Model;

namespace Server.Processing
{
    /// Producer/Consummer conveyor
    internal class Conveyor : IStaticHandle
    {
        private static readonly NLog.ILogger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Conveyor from only one item.
        /// </summary>
        internal static ConveyorItem ConveyorItemSingleton = new ConveyorItem();

        /// <summary>
        /// Catalog of all cards, both received and processed by the conveyor.
        /// </summary>
        internal static ConcurrentDictionary<string, FileCard?> TotalFileCardCatalog { get; } = new ();
        /// <summary>
        /// Session channels of changes of file card states.
        /// </summary>
        internal static ConcurrentDictionary<string, Channel<StateInfo>> FileCardStateChanges = new();

        internal static bool TryGetSessionChannel(string? sessionId, out Channel<StateInfo>? channel)
        {
            channel = null;
            if (sessionId == null)
            {
                Logger.Error(new ArgumentNullException(nameof(sessionId)));
                return false;
            }

            var res = FileCardStateChanges.TryGetValue(sessionId, out channel);
            if (!res)
            {
                var cannel = Channel.CreateBounded<StateInfo>(
                    new BoundedChannelOptions(100)
                    {
                        SingleWriter = false,
                        SingleReader = true,
                        AllowSynchronousContinuations = false,
                        FullMode = BoundedChannelFullMode.Wait
                    }
                );
                FileCardStateChanges.TryAdd(sessionId, cannel);
                res = true;
            }
            
            return res;
        }

        /// <summary>
        /// Starts the conveyor
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        internal static async Task HandleAsync(CancellationToken token)
        {
            await new TaskFactory().StartNew(async () => { await ConveyorItemSingleton.HandleAsync(token); });
        }
    }
}
