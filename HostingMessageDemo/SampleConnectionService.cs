using Hydra4NET;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HostingMessageDemo
{
    internal class SampleConnectionService : BackgroundService
    {
        private IHydra _hydra;
        private ILogger<SampleConnectionService> _logger;

        public SampleConnectionService(IHydra hydra, ILogger<SampleConnectionService> logger)
        {
            _hydra = hydra;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                //since this code may be called before Hydra is finised initializing, GetRedisConnectionAsync() will ensure that initialization is complete before returning the connection.
                var redis = await _hydra.GetRedisConnectionAsync();
                _logger.LogInformation($"Redis client name: {redis.ClientName}");
                redis = await _hydra.GetRedisConnectionAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "SampleConnectionService failed");
            }
        }
    }
}
