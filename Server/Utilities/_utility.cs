using NLog;

namespace Server.Utilities
{
    internal static class _utility
    {
        private static NLog.ILogger Logger = LogManager.GetCurrentClassLogger();

        public static async Task AttemptsAsync(byte count = 5, ushort delay = 100, bool tryIfEx = true, Func<int, Task> stepAction = null, Action errAction = null, Action finAction = null, CancellationToken? token = null)
        {
            if (stepAction == null)
                throw new ArgumentNullException(nameof(stepAction));

            try
            {
                byte i = 0;
                while (i++ < count)
                {
                    try
                    {
                        token?.ThrowIfCancellationRequested();
                        await stepAction.Invoke(i);

                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        if (i < count)
                        {
                            if (delay > 0)
                            {
                                if (token == null)
                                    await Task.Delay(delay);
                                else
                                    await Task.Delay(delay, token.Value);
                            }
                        }
                        else
                        {
                            Logger.Error(ex, $"\r\nОшибка после {i} попыток.\r\n");

                            if (tryIfEx)
                            {
                                if (errAction != null)
                                {
                                    token?.ThrowIfCancellationRequested();
                                    try
                                    {
                                        await new TaskFactory().StartNew(errAction);
                                    }
                                    catch { }
                                }

                                throw ex;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (finAction != null)
                {
                    token?.ThrowIfCancellationRequested();
                    try
                    {
                        await new TaskFactory().StartNew(finAction);
                    }
                    catch { }
                }
            }
        }
    }
}
