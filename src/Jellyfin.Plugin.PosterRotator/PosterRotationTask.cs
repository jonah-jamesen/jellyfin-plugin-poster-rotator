using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.PosterRotator
{
    public class PosterRotationTask : IScheduledTask
    {
        private readonly PosterRotatorService _service;
        private readonly ILogger<PosterRotationTask> _logger;

        public PosterRotationTask(PosterRotatorService service, ILogger<PosterRotationTask> logger)
        {
            _service = service;
            _logger = logger;
        }

        public string Name => "Rotate Movie Posters (Pool Then Rotate)";
        public string Description => "Fills a local poster pool per movie from metadata providers, then rotates through the pool without redownloading.";
        public string Category => "Library";
        public string Key => "PosterRotator.RotatePostersTask";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var started = DateTimeOffset.UtcNow;
            _logger.LogInformation("PosterRotator: scheduled task started at {Started}", started);

            var plugin = Plugin.Instance;
            if (plugin is null)
            {
                _logger.LogError("PosterRotator: Plugin.Instance is null; aborting.");
                return;
            }

            var cfg = plugin.Configuration ?? new Configuration();

            try
            {
                await _service.RunAsync(cfg, progress, cancellationToken).ConfigureAwait(false);
                var ended = DateTimeOffset.UtcNow;
                _logger.LogInformation("PosterRotator: scheduled task completed in {Duration}", ended - started);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("PosterRotator: scheduled task was cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PosterRotator: scheduled task failed.");
                throw;
            }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerDaily, TimeOfDayTicks = TimeSpan.FromHours(3).Ticks }
            };
        }
    }
}
