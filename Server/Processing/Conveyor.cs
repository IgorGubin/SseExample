using System.Collections.Concurrent;

using Server.Enums;
using Server.Model;
using Server.Utilities;

namespace Server.Processing
{
    /// Producer/Consummer conveyor
    public class Conveyor
    {
        /// <summary>
        /// Catalog of all file cards.
        /// </summary>
        internal static ConcurrentDictionary<string, FileCard?> TotalFileCardCatalog { get; } = new ConcurrentDictionary<string, FileCard?>();

        /// <summary>
        /// Starts the conveyor
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        internal static async Task HandleAsync(CancellationToken token)
        {
            await new TaskFactory().StartNew(async () =>
            { // Simulation of files processing
                while (!token.IsCancellationRequested)
                {
                    Parallel.ForEach(TotalFileCardCatalog.Values.ToArray(), async fc =>
                    {
                        if (token.IsCancellationRequested)
                            return;

                        switch (fc?.State)
                        {
                            case Enums.FileCardStateEnum.Nothing:
                                fc.State = Enums.FileCardStateEnum.New;
                                break;
                            case Enums.FileCardStateEnum.New:
                                fc.State = Enums.FileCardStateEnum.InProcessing;
                                break;
                            case Enums.FileCardStateEnum.InProcessing:
                                await Task.Delay(3000);
                                fc.State = Enums.FileCardStateEnum.Сompleted;
                                break;
                        }
                    });

                    if (!token.IsCancellationRequested)
                        await Task.Delay(_cfg.WaitPeriodMs);
                }
            });
        }
    }
}
