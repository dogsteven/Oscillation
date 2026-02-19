using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Oscillation.Core.Utilities
{
    public class DetachedTaskTracker
    {
        private readonly ConcurrentDictionary<Task, int> _tasks;
        private long _allowTrackingFlag;

        public DetachedTaskTracker()
        {
            _tasks = new ConcurrentDictionary<Task, int>();
            _allowTrackingFlag = 0;
        }

        public void Track(Task task)
        {
            if (Interlocked.Read(ref _allowTrackingFlag) == 1)
            {
                return;
            }
            
            _tasks[task] = 0;

            _ = UntrackOnComplete(task);
        }

        private async Task UntrackOnComplete(Task task)
        {
            try
            {
                await task;
            }
            finally
            {
                if (Interlocked.Read(ref _allowTrackingFlag) == 0)
                {
                    _tasks.TryRemove(task, out _);
                }
            }
        }

        public async Task WaitForAllAsync()
        {
            if (Interlocked.CompareExchange(ref _allowTrackingFlag, 1, 0) == 1)
            {
                return;
            }

            try
            {
                await Task.WhenAll(_tasks.Keys.ToArray());
            }
            finally
            {
                _tasks.Clear();
            }
        }
    }
}