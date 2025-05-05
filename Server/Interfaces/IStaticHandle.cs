namespace Server.Interfaces
{
    public interface IStaticHandle
    {
        static async Task HandleAsync(CancellationToken token)
        {
            await Task.CompletedTask;
        }
    }
}
