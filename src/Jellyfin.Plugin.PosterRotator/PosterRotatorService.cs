using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Jellyfin.Plugin.PosterRotator
{
    public class PosterRotatorService
    {
        private static readonly string[] DefaultGlobs = new[] {
            "*-poster*.jpg","*-poster*.jpeg","*-poster*.png","*-poster*.webp",
            "poster*.jpg","poster*.jpeg","poster*.png","poster*.webp",
            "cover*.jpg","cover*.jpeg","cover*.png","cover*.webp",
            "*alt*.jpg","*alt*.jpeg","*alt*.png","*alt*.webp"
        };

        private readonly HttpClient _http;

        public PosterRotatorService()
        {
            _http = new HttpClient();
        }

        public async Task RunAsync(Configuration cfg, IProgress<double> progress, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(cfg.ServerUrl) || string.IsNullOrWhiteSpace(cfg.ApiKey))
                throw new InvalidOperationException("ServerUrl and ApiKey must be set in plugin configuration.");
            
            var baseUrl = cfg.ServerUrl.TrimEnd('/');
            var headers = new Dictionary<string, string> { ["X-Emby-Token"] = cfg.ApiKey };
            
            var vfs = await GetAsync<List<VirtualFolder>>($"{baseUrl}/Library/VirtualFolders", headers, ct);
            var libraryIds = FilterLibraries(vfs, cfg.Libraries);

            var itemsCount = 0;
            var changed = 0;
            foreach (var libId in libraryIds)
            {
                int start = 0;
                const int pageSize = 200;
                while (true)
                {
                    var url = $"{baseUrl}/Items?IncludeItemTypes=Movie&Recursive=true&Fields=Path,ProviderIds,MediaType,ParentId&ParentId={libId}&StartIndex={start}&Limit={pageSize}";
                    var page = await GetAsync<ItemsResult>(url, headers, ct);
                    var items = page.Items ?? Array.Empty<Item>();
                    if (items.Length == 0) break;

                    foreach (var it in items)
                    {
                        ct.ThrowIfCancellationRequested();
                        itemsCount++;
                        if (await RotateOrFillAsync(baseUrl, headers, it, cfg, ct))
                            changed++;
                        progress?.Report((double)changed / Math.Max(1, itemsCount) * 100.0);
                    }
                    start += pageSize;
                }
            }
        }

        private static List<string> FilterLibraries(List<VirtualFolder> vfs, List<string> desired)
        {
            if (desired == null || desired.Count == 0)
                return vfs.Select(v => v.Id).ToList();
            var set = desired.Select(d => d.Trim().ToLowerInvariant()).ToHashSet();
            return vfs.Where(v => set.Contains((v.Name ?? "").Trim().ToLowerInvariant()))
                      .Select(v => v.Id).ToList();
        }

        private async Task<bool> RotateOrFillAsync(string baseUrl, Dictionary<string,string> headers, Item item, Configuration cfg, CancellationToken ct)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Path)) return false;
            var moviePath = item.Path!;
            var movieDir = Directory.Exists(moviePath) ? moviePath : Path.GetDirectoryName(moviePath)!;
            if (string.IsNullOrWhiteSpace(movieDir) || !Directory.Exists(movieDir)) return false;

            var poolDir = Path.Combine(movieDir, "poster_pool");
            Directory.CreateDirectory(poolDir);

            var patterns = (cfg.ExtraPosterPatterns ?? new()).Concat(DefaultGlobs).ToList();
            var localCandidates = EnumerateByGlobs(poolDir, patterns);

            if (localCandidates.Count < cfg.PoolSize)
            {
                var needed = Math.Max(0, cfg.PoolSize - localCandidates.Count);
                var remoteListUrl = $"{baseUrl}/Items/{item.Id}/RemoteImages?type=Primary";
                var remotes = await GetAsync<List<RemoteImage>>(remoteListUrl, headers, ct) ?? new List<RemoteImage>();

                int downloaded = 0;
                foreach (var r in remotes)
                {
                    if (downloaded >= needed) break;
                    if (string.IsNullOrWhiteSpace(r.Url)) continue;

                    var attachUrl = $"{baseUrl}/Items/{item.Id}/RemoteImages/Download?type=Primary&imageUrl={Uri.EscapeDataString(r.Url)}";
                    if (!cfg.DryRun)
                    {
                        await PostNoBodyAsync(attachUrl, headers, ct);
                    }

                    try
                    {
                        var bytes = await _http.GetByteArrayAsync(r.Url, ct);
                        var fname = $"poster_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{downloaded}.jpg";
                        var full = Path.Combine(poolDir, fname);
                        if (!cfg.DryRun)
                            await File.WriteAllBytesAsync(full, bytes, ct);
                        downloaded++;
                    }
                    catch
                    {
                    }
                }

                localCandidates = EnumerateByGlobs(poolDir, patterns);
            }

            if (localCandidates.Count == 0) return false;

            var statePath = Path.Combine(poolDir, "rotation_state.json");
            var state = LoadState(statePath);
            var nextPath = cfg.SequentialRotation
                ? PickNextSequential(localCandidates, state, item.Id)
                : PickRandom(localCandidates);

            if (cfg.LockImagesAfterFill && localCandidates.Count >= cfg.PoolSize && !cfg.DryRun)
            {
                // TODO: Implement PATCH /Items/{id} to set IsLocked / LockedFields once confirmed for your server version.
            }

            if (!cfg.DryRun)
            {
                await UploadPrimaryAsync($"{baseUrl}/Items/{item.Id}/Images/Primary", headers, nextPath, ct);
            }

            state.LastIndexByItem[item.Id] = (state.LastIndexByItem.TryGetValue(item.Id, out var idx) ? idx + 1 : 1);
            SaveState(statePath, state);
            return true;
        }

        private static RotationState LoadState(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<RotationState>(json) ?? new RotationState();
                }
            }
            catch { }
            return new RotationState();
        }
        private static void SaveState(string path, RotationState state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private static string PickNextSequential(List<string> files, RotationState state, string itemId)
        {
            var idx = 0;
            if (state.LastIndexByItem.TryGetValue(itemId, out var last))
                idx = last % files.Count;
            return files[idx];
        }

        private static string PickRandom(List<string> files)
        {
            var r = new Random();
            return files[r.Next(files.Count)];
        }

        private static List<string> EnumerateByGlobs(string dir, List<string> patterns)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pat in patterns)
            {
                foreach (var f in Directory.GetFiles(dir, pat))
                    set.Add(f);
            }
            return set.OrderBy(f => f).ToList();
        }

        private async Task<T> GetAsync<T>(string url, Dictionary<string,string> headers, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            foreach (var kv in headers) req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
            using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions{ PropertyNameCaseInsensitive = true })!;
        }

        private async Task PostNoBodyAsync(string url, Dictionary<string,string> headers, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            foreach (var kv in headers) req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
            using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();
        }

        private async Task UploadPrimaryAsync(string url, Dictionary<string,string> headers, string filePath, CancellationToken ct)
        {
            using var form = new MultipartFormDataContent();
            var contentType = GuessContentType(filePath);
            using var fs = File.OpenRead(filePath);
            var streamContent = new StreamContent(fs);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(streamContent, "image", Path.GetFileName(filePath));

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            foreach (var kv in headers) req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

            using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();
        }

        private static string GuessContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".jpeg" => "image/jpeg",
                ".jpg" => "image/jpeg",
                _ => "application/octet-stream"
            };
        }
    }

    public class ItemsResult
    {
        public Item[] Items { get; set; } = Array.Empty<Item>();
    }
    public class Item
    {
        public string Id { get; set; } = "";
        public string Path { get; set; } = "";
    }
    public class VirtualFolder
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }
    public class RemoteImage
    {
        [JsonPropertyName("ProviderName")] public string? ProviderName { get; set; }
        [JsonPropertyName("Url")] public string Url { get; set; } = "";
    }
    public class RotationState
    {
        public Dictionary<string,int> LastIndexByItem { get; set; } = new();
    }
}
