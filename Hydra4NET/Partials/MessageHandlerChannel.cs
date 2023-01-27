using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Hydra4NET
{
    public partial class Hydra
    {
        //allows message hander events to be stored and flushed at process exit
        //we could allow them to set max handler concurrency by making this bounded
        private Channel<Task> _eventsChannel = Channel.CreateUnbounded<Task>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        private Task? _eventsChannelProcessTask;

        private void ConfigureEventsChannel()
        {
            //when cts is cancelled, the channel will stop receiving messages and EventsChannelProcess() will be able to complete
            _cts.Token.Register(() => _eventsChannel.Writer.TryComplete());
            _eventsChannelProcessTask = GetEventsChannelProcess();
        }

        private async Task GetEventsChannelProcess()
        {
            await foreach (var task in _eventsChannel.Reader.ReadAllAsync())
            {
                //swallows uncaught exceptions in event handler actions.
                try
                {
                    await task;
                }
                catch { }
            }
        }

        /// <summary>
        /// Waits for remaining message handler tasks to complete
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        async ValueTask FlushMessageEvents(CancellationToken ct = default)
        {
            if (_eventsChannelProcessTask == null)
                return;
            Task flushTask = _eventsChannelProcessTask;
            _eventsChannelProcessTask = null;
            await Task.Run(() => flushTask, ct);
        }

        private ValueTask AddMessageChannelAction(Task action) => _eventsChannel.Writer.WriteAsync(action);
    }
}
