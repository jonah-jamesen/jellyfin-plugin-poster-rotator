# Poster Rotator (Jellyfin Plugin Skeleton)

A scheduled-task plugin that **fills a poster pool** for each movie from metadata providers, then **rotates** the Primary poster from that local pool without redownloading.

**Your requested behavior**
- Store downloaded posters **next to the movie** in `./poster_pool/`
- Option to **lock images after pool is established**
- On fill: **Let Jellyfin fetch & attach internally**, then **also save a copy** into the pool folder
- Rotate by re-uploading a pool image as **Primary**

## Build
1) Install .NET 8 SDK.
2) If package versions in the csproj don’t match your server, adjust them.
3) Build:
```bash
dotnet build src/Jellyfin.Plugin.PosterRotator/Jellyfin.Plugin.PosterRotator.csproj -c Release
```

## Install
Copy the published output to your Jellyfin plugins folder (create a `PosterRotator` subfolder), e.g.:
- Linux: `~/.local/share/jellyfin/plugins/PosterRotator/`
- Windows: `%AppData%\Jellyfin\plugins\PosterRotator\`

Restart Jellyfin. Find the task in **Dashboard → Scheduled Tasks**.

## Configure
Open the plugin config page (this skeleton includes a static `Web/config.html` demo). Set:
- **ServerUrl** (e.g., `http://localhost:8096`)
- **ApiKey** (Dashboard → API Keys)
- **Libraries** (comma-separated names; empty = all)
- **PoolSize** (default 5)
- **SequentialRotation** or random
- **SaveNextToMedia** (writes `./poster_pool`)
- **LockImagesAfterFill** (skeleton leaves a TODO to PATCH item lock)
- **DryRun**

## Internals
- Enumerates libraries: `GET /Library/VirtualFolders`
- Lists movies: `GET /Items?IncludeItemTypes=Movie...`
- Lists provider posters: `GET /Items/{id}/RemoteImages?type=Primary`
- Attaches a remote: `POST /Items/{id}/RemoteImages/Download?type=Primary&imageUrl=...`
- Uploads local Primary: `POST /Items/{id}/Images/Primary` (multipart; use `image/jpeg` for JPGs)
- Rotation state per movie: `poster_pool/rotation_state.json`

## Next steps
- Wire `Web/config.html` to Jellyfin’s config binding (IPluginConfigurationPage)
- Implement image/metadata lock via `/Items/{id}` (IsLocked / LockedFields)
- Add “Prune” action to delete non-pool images by index
