using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Web.Api.Toolkit.Queues.Application.Services
{
    public sealed class QueueService<T> : IQueueService<T> where T : class
    {
        private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();
        private readonly ConcurrentDictionary<T, byte> _buffer = new();

        public async ValueTask EnqueueAsync(T job)
        {
            _buffer.TryAdd(job, 0);
            await _channel.Writer.WriteAsync(job);
        }

        public async IAsyncEnumerable<T> DequeueAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                _buffer.TryRemove(item, out _);
                yield return item;
            }
        }

        public IAsyncEnumerable<QueueItemHandle<T>> DequeueWithHandleEnumerable(CancellationToken cancellationToken)
        {
            return DequeueWithHandleInternal();

            async IAsyncEnumerable<QueueItemHandle<T>> DequeueWithHandleInternal()
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var item = await _channel.Reader.ReadAsync(cancellationToken);
                    yield return new QueueItemHandle<T>(item, RemoveFromBuffer);
                }
            }
        }

        private void RemoveFromBuffer(T item)
        {
            _buffer.TryRemove(item, out _);
        }

        public bool Contains(Func<T, bool> predicate)
        {
            return _buffer.Keys.Any(predicate);
        }
    }

    public sealed class QueueItemHandle<T> : IAsyncDisposable where T : class
    {
        public T Item { get; }
        private readonly Action<T> _onDispose;
        private bool _disposed;

        public QueueItemHandle(T item, Action<T> onDispose)
        {
            Item = item;
            _onDispose = onDispose;
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _onDispose?.Invoke(Item);
                _disposed = true;
            }
            return ValueTask.CompletedTask;
        }
    }
}
