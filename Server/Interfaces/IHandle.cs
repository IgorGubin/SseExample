namespace Server.Interfaces
{

    public interface IHandle
    {
        Task HandleAsync(CancellationToken token);
    }
}
