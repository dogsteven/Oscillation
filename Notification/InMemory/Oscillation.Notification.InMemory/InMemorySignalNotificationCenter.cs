using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Oscillation.Hosting.Client.Abstractions;
using Oscillation.Hosting.Server.Abstractions;

namespace Oscillation.Notification.InMemory
{
    public class InMemorySignalNotificationCenter
    {
        private readonly Channel<DateTime> _centralChannel;

        public InMemorySignalNotificationCenter()
        {
            _centralChannel = Channel.CreateUnbounded<DateTime>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });
        }

        public InMemorySignalNotificationPublisher GetPublisher()
        {
            return new InMemorySignalNotificationPublisher(_centralChannel.Writer);
        }

        public InMemorySignalNotificationSubscriber GetSubscriber()
        {
            return new InMemorySignalNotificationSubscriber(_centralChannel.Reader);
        }
    }

    public class InMemorySignalNotificationPublisher : ISignalNotificationPublisher
    {
        private readonly ChannelWriter<DateTime> _centralChannelWriter;

        public InMemorySignalNotificationPublisher(ChannelWriter<DateTime> centralChannelWriter)
        {
            _centralChannelWriter = centralChannelWriter;
        }
        
        public void PublishPotentialNextFireTime(DateTime potentialNextFireTime)
        {
            _ = PublishPotentialNextFireTimeAsync(potentialNextFireTime);
        }

        private async Task PublishPotentialNextFireTimeAsync(DateTime potentialNextFireTime)
        {
            await _centralChannelWriter.WriteAsync(potentialNextFireTime);
        }
    }

    public class InMemorySignalNotificationSubscriber : ISignalNotificationSubscriber
    {
        private readonly ChannelReader<DateTime> _centralChannelReader;
        
        private readonly ConcurrentBag<ISignalNotificationHandler> _handlers;
        private int _runningFlag;

        public InMemorySignalNotificationSubscriber(ChannelReader<DateTime> centralChannelReader)
        {
            _centralChannelReader = centralChannelReader;
            
            _handlers = new ConcurrentBag<ISignalNotificationHandler>();
            _runningFlag = 0;
        }
        
        public void RegisterHandler(ISignalNotificationHandler handler)
        {
            if (_runningFlag == 0)
            {
                return;
            }
        
            _handlers.Add(handler);
        }

        public async Task StartSubscribingAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _runningFlag, 1, 0) == 1)
            {
                return;
            }

            await foreach (var potentialNextFireTime in _centralChannelReader.ReadAllAsync(cancellationToken))
            {
                foreach (var handler in _handlers)
                {
                    handler.HandlePotentialNextFireTimePublished(potentialNextFireTime);
                }
            }

            _runningFlag = 0;
        }
    }
}