using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.PosterRotator
{
    public class PosterRotationTask : IScheduledTask
    {
        private readonly PosterRotatorService _service;

        public PosterRotationTask(PosterRotatorService service)
        {
            _service = service;
        }

        public string Name => "Rotate Movie Posters (Pool Then Rotate)";
        public string Description => "Fills a local poster pool per movie from metadata providers, then rotates through the pool without redownloading.";
        public string Category => "Library";
        public string Key => "PosterRotator.RotatePostersTask"; // required

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var cfg = Plugin.Instance.Configuration; // 👈 read config here
            await _service.RunAsync(cfg, progress, cancellationToken).ConfigureAwait(false);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[] {
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerDaily, TimeOfDayTicks = TimeSpan.FromHours(3).Ticks }
            };
        }
    }
}
