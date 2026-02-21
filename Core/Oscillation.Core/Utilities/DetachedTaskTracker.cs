using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Oscillation.Core.Utilities
{
    public class DetachedTaskTracker
    {
        private readonly Channel<Task> _requestChannel;
        private readonly TaskCompletionSource<bool> _channelCompletionSource;
        private volatile TaskCompletionSource<bool>? _allTasksCompletionSource;

        private long _numberOfUnsettledTasks;

        public DetachedTaskTracker()
        {
            _requestChannel = Channel.CreateUnbounded<Task>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            _channelCompletionSource = new TaskCompletionSource<bool>();
            _allTasksCompletionSource = null;

            _numberOfUnsettledTasks = 0;
        }

        public async Task StartAsync()
        {
            await foreach (var task in _requestChannel.Reader.ReadAllAsync())
            {
                if (task.IsCompleted)
                {
                    continue;
                }
                
                Interlocked.Increment(ref _numberOfUnsettledTasks);
                _ = UntrackOnComplete(task);
            }

            _channelCompletionSource.TrySetResult(true);
        }

        public void Track(Task task, TimeSpan timeout)
        {
            _requestChannel.Writer.TryWrite(Task.WhenAny(task, Task.Delay(timeout)));
        }

        private async Task UntrackOnComplete(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                // ignored
            }   
            finally
            {
                if (Interlocked.Decrement(ref _numberOfUnsettledTasks) == 0)
                {
                    if (_channelCompletionSource.Task.IsCompleted)
                    {
                        if (Interlocked.Read(ref _numberOfUnsettledTasks) == 0)
                        {
                            _allTasksCompletionSource?.TrySetResult(true);
                        }
                    }
                }
            }
        }

        public async Task WaitForAllAsync()
        {
            _allTasksCompletionSource = new TaskCompletionSource<bool>();
            _requestChannel.Writer.TryComplete();
            await _channelCompletionSource.Task;

            if (Interlocked.Read(ref _numberOfUnsettledTasks) > 0)
            {
                await _allTasksCompletionSource.Task;
            }
        }
    }
}