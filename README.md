# Flint

A Windows browser that stays out of your way.

---

Flint is a browser built for people who are tired of browsers that think they know better. No Chrome account prompts. No sidebar AI. No update nags. Just a fast, glass-themed window that browses the web and remembers what you tell it to remember.

The home page is a freeform canvas called the Pegboard. You put tiles on it, arrange them however you want, and it persists exactly as you left it. It's a better new tab page than anything shipping in a major browser right now.

---

## Features

Flint uses significantly less RAM than Chrome or Edge under the same workload, and real-world download speeds are faster (fewer background processes competing for bandwidth). Benchmark performance is competitive with mainstream Chromium browsers. The ad blocker is on by default and blocks at the network level before requests leave the machine, so it's not just cosmetic filtering.

The home page is the Pegboard, a freeform canvas where you place tiles: notes with drawing and checklists, shortcuts, a clock, a weather forecast, a system monitor, recent history, photos, timers. It's not a speed dial. It's not a feed. It's a blank surface you build yourself, and it stays exactly how you left it.

**Extensions**
- Load any unpacked Chrome extension from a local folder (Manifest V2 or V3)
- Each installed extension gets an Open button that launches its popup with full functionality
- Tested with uBlock Origin

---

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+T` | New tab |
| `Ctrl+W` | Close tab |
| `Ctrl+Tab` / `Ctrl+Shift+Tab` | Next / previous tab |
| `Ctrl+Shift+T` | Reopen last closed tab |
| `Ctrl+1–8` | Switch to tab by index |
| `Ctrl+9` | Last tab |
| `Ctrl+L` | Focus address bar |
| `Ctrl+R` / `F5` | Reload |
| `Escape` | Stop loading |
| `Alt+Left` / `Alt+Right` | Back / forward |
| `Alt+Home` | Home page |
| `F11` | Toggle fullscreen |
| `Ctrl+D` | Bookmark / unbookmark |
| `Ctrl+B` | Bookmarks |
| `Ctrl+H` | History |
| `Ctrl+J` | Downloads |
| `Ctrl+,` | Settings |
| `Ctrl++` / `Ctrl+-` / `Ctrl+0` | Zoom in / out / reset |


---

## Requirements

- Windows 10 or 11
- Microsoft Edge WebView2 Runtime (pre-installed on most Windows 11 machines, [download here](https://developer.microsoft.com/microsoft-edge/webview2/) if needed)

---

## Installation

Download `FlintSetup.exe` from the latest release and run it. Your profile is stored at `%LocalAppData%\Flint\`.

To build from source, you'll need the .NET 8 SDK:

```
git clone https://github.com/sm1else-bot/flint.git
cd flint
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

---

## Data & Privacy

Everything stays on your machine. Flint makes no external connections except:

- Pages you navigate to
- Favicon lookups (Google's favicon service, for tab icons)
- Ad-block list updates (StevenBlack hosts list on GitHub, refreshed every 7 days)
- Weather data (Open-Meteo, only if you use the weather tile)

No analytics. No crash reports. No accounts.

| What | Where |
|---|---|
| Bookmarks, history, settings | `%LocalAppData%\Flint\profile.json` |
| Pegboard layout | `%LocalAppData%\Flint\pegboard.json` |
| Ad-block list | `%LocalAppData%\Flint\blocklist.txt` |
| Cookies, cache, extensions | `%LocalAppData%\Flint\WebView2\` |
