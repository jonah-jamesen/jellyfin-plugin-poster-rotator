# Jellyfin Poster Rotator

Rotate movie posters in Jellyfin by building a small local pool of images next to each movie. The plugin fills the pool from your enabled metadata image providers, then cycles through those posters on a schedule without redownloading every time.

---

## Features

- Builds a per-movie `poster_pool` folder next to the media file or movie folder  
- Downloads posters from providers like TMDb and Fanart when available  
- Saves a snapshot of the current primary poster as `pool_currentprimary.*`  
- Option to lock the pool after it reaches the target size  
- Sequential or random rotation with a configurable cooldown  
- Best-effort cache bust so clients notice the change  

---

## Requirements

- Jellyfin **10.10.3** or newer  
- **.NET 8 runtime** on the server  
- At least one remote image provider enabled in Jellyfin (e.g., TMDb or Fanart)  

---

## Install

1. In your Plugin Catalog, Add the Repo: https://raw.githubusercontent.com/jonah-jamesen/jellyfin-plugin-poster-rotator/refs/heads/main/manifest.json
2. In My Plugins, install the Poster Rotator plugin
3. Click the plugin tile to edit the settings
4. Restart Jellyfin
4. (Optional) In Scheduled Tasks, run the Rotate Movie Posters (Pool Then Rotate) task to seed your Poster Pools.

## Manual Install

1. Download the latest release `.zip` from the **Releases** page.  
2. Stop Jellyfin.  
3. Extract and copy `Jellyfin.Plugin.PosterRotator.dll` to the server plugins folder:  
   - **Windows:** `C:\ProgramData\Jellyfin\Server\plugins`  
   - **Linux:** `/var/lib/jellyfin/plugins` or `/var/lib/jellyfin/plugins/local`  
   - **Docker:** bind mount a plugins folder and place the `.dll` there  
4. Start Jellyfin.  
5. Go to **Dashboard → Plugins → Poster Rotator → Settings**.  

---

## How it Works

When the scheduled task runs:  

- The plugin looks for or creates `<movie directory>/poster_pool`.  
- It tops up the pool using your enabled metadata image providers until the pool size setting is reached.  
- If no remote images are found, it copies the current primary poster into the pool once.  
- It rotates to the next image in the pool and updates the **Primary poster** file.  
- The plugin touches the file time and nudges Jellyfin so clients refresh.  

**Files created per movie:**  
- `poster_pool/pool_currentprimary.<ext>` → snapshot of the current primary  
- `poster_pool/pool_<timestamp>.<ext>` → downloaded candidates  
- `poster_pool/rotation_state.json` → rotation state and cooldown tracking  
- `poster_pool/pool.lock` → created when **Lock After Fill** is enabled  

---

## Settings

- **Pool Size** → number of posters to keep in the pool  
- **Lock Images After Fill** → stop metadata refreshes from downloading once pool reaches size  
- **Sequential Rotation** → rotate in stable order; otherwise, random  
- **Min Hours Between Switches** → cooldown (default: 24)  Note: update this and the Scheduled Task Rotate Movie Posters (Pool Then Rotate) to process more frequently in order to cycle more often.
- **Extra Poster Patterns** → additional filename globs to include  
- **Dry Run** → log actions without changing files

💡 *Tip: The scheduled task can run more often than your cooldown. The plugin skips items still within the Min Hour window.*  

---

## Running the Task

Go to: **Dashboard → Scheduled Tasks → Rotate Movie Posters (Pool Then Rotate)**  

- Run on demand, or schedule daily/hourly.  
- Cooldown prevents over-rotation.  

---

## Troubleshooting

**Pool stays at 1 and does not download more**  
- Ensure TMDb and Fanart are installed and enabled  
- Verify server can reach provider endpoints  
- Check server log for provider attempts  

**Posters do not appear to change**  
- For testing, set `Min Hours Between Switches` to 1 and run the task twice  
- Force refresh the browser or check **Images** tab in the movie  

**No providers detected**  
- Restart server after installing provider plugins  
- Ensure the item is a Movie and has provider IDs  

---

## Build from Source

```bash
dotnet build src/Jellyfin.Plugin.PosterRotator/Jellyfin.Plugin.PosterRotator.csproj -c Release
