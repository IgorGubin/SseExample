using System.Collections.Concurrent;

using NLog;

using Server.Enums;
using Server.Interfaces;
using Server.Model;
using Server.Utilities;

namespace Server.Processing
{
    internal class ConveyorItem : IHandle
    {
        private static NLog.ILogger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Entry queue
        /// </summary>
        public BlockingCollection<FileCard> In { get; } = new BlockingCollection<FileCard>();

        /// <summary>
        /// Starts the process of consuming the input queue.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task HandleAsync(CancellationToken token)
        {
            Func<FileCard, CancellationToken?, Task> produceInToOut = (fc, t) =>
            {
                var res = new Task(() =>
                {
                    while (fc.State != Enums.FileCardStateEnum.Сompleted)
                    { // Simulate file processing
                        switch (fc.State)
                        {
                            case Enums.FileCardStateEnum.Nothing:
                                fc.State = Enums.FileCardStateEnum.New;
                                break;
                            case Enums.FileCardStateEnum.InProcessing:
                                Task.Delay(3000);
                                fc.State = Enums.FileCardStateEnum.Сompleted;
                                break;
                        }
                    }
                });

                return res;
            };

            //await _utility.AttemptsAsync(stepAction: async (i) =>
            //{
                // Starting the input queue consumption process
                await Consumption(In, produceInToOut, _cfg.ConsumptionMaxParallelDegree, _cfg.WaitWhenAnyMs, token);
            //});
        }

        /// <summary>
        /// Provides an implementation of a multitasking input queue consumption process,
        /// applying a predefined transformation to each queue element.
        /// </summary>
        /// <typeparam name="T">Queue element type.</typeparam>
        /// <param name="queue">Consumer queue.</param>
        /// <param name="taskFactory">Queue element transformation method.</param>
        /// <param name="maxParallelDegree">Maximum number of parallel tasks.</param>
        /// <returns></returns>
        /// <remarks>
        /// When specifying taskFactory, do not use async/await - in some cases it does not work correctly.
        /// </remarks>
        private async Task Consumption(
            BlockingCollection<FileCard> queue,
            Func<FileCard, CancellationToken?, Task> taskFactory,
            int maxParallelDegree,
            int waitWhenAnyMs,
            CancellationToken token
        )
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));
            if (taskFactory == null)
                throw new ArgumentNullException(nameof(taskFactory));

            var tasks = new Dictionary<Task, FileCard>();

            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                while (tasks.Count < maxParallelDegree)
                {
                    if (token.IsCancellationRequested)
                        break;

                    if (queue.TryTake(out FileCard? fileCard))
                    { /* queue is not empty */
                        fileCard.State = FileCardStateEnum.InProcessing;
                        var task = taskFactory(fileCard, token);

                        tasks.Add(task, fileCard);

                        if (task.Status == TaskStatus.Created)
                        {
                            task.Start();
                        }
                    }
                    else
                    { /* queue is empty */
                        if (tasks.Count > 0)
                            break;
                        else
                            await Task.Delay(100);
                    }
                }

                var start = DateTime.Now;
                while ((DateTime.Now - start).TotalMilliseconds < waitWhenAnyMs)
                {
                    var completedTask = await Task.WhenAny(tasks.Keys).ConfigureAwait(true);
                    if (completedTask == null)
                        break;

                    var fileCard = tasks[completedTask];
                    tasks.Remove(completedTask);

                    if (completedTask.IsFaulted)
                    {
                        Logger.Error(completedTask.Exception, $"---Fault[{fileCard.FileId}/{fileCard.SessionId}]");
                    }
                    else if (completedTask.IsCanceled)
                    {
                        Logger.Info($"---Cancelled[{fileCard.FileId}/{fileCard.SessionId}]");
                    }
                    else
                    {
                        Logger.Info($"---Success[{fileCard.FileId}/{fileCard.SessionId}]");
                    }
                }
            }
        }
    }
}
