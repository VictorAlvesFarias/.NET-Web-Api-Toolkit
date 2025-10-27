namespace Packages.Queues.Application.Services
{
    public interface IQueueService<T> where T : class
    {
        ValueTask EnqueueAsync(T job);

        IAsyncEnumerable<T> DequeueAsync(CancellationToken cancellationToken);

        IAsyncEnumerable<QueueItemHandle<T>> DequeueWithHandleEnumerable(CancellationToken cancellationToken);

        bool Contains(Func<T, bool> predicate);
    }
}
