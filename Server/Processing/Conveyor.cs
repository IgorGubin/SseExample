using Server.Model;
using System.Collections.Concurrent;

namespace Server.Processing
{
    /// Producer/Consummer conveyor
    public class Conveyor
    {
        /// <summary>
        /// Conveyor from only one item.
        /// </summary>
        internal static ConveyorItem ConveyorItemSingleton = new ConveyorItem();

        /// <summary>
        /// Catalog of all cards, both received and processed by the conveyor.
        /// </summary>
        internal static ConcurrentDictionary<string, FileCard?> TotalFileCardCatalog { get; } = new ConcurrentDictionary<string, FileCard?>();

        /// <summary>
        /// Starts the conveyor
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        internal static async Task HandleAsync(CancellationToken token)
        {
            await new TaskFactory().StartNew(async () => await ConveyorItemSingleton.HandleAsync(token));
        }
    }
}
