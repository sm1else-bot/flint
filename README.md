# Flint

A Windows browser that stays out of your way.

---

Flint is a browser built for people who are tired of browsers that think they know better. No Chrome account prompts. No sidebar AI. No update nags. Just a fast, glass-themed window that browses the web and remembers what you tell it to remember.

The home page is a freeform canvas called the Pegboard — you put tiles on it, arrange them however you want, and it persists exactly as you left it. It's a better new tab page than anything shipping in a major browser right now.

---

## Features

**Browsing**
- Multi-tab with per-tab favicon, mute, and zoom
- Smart address bar with history autocomplete
- Built-in ad blocker, on by default
- Downloads with a live progress dropdown and active badge on the toolbar
- Zoom per tab — `Ctrl++` / `Ctrl+-` / `Ctrl+0`, range 25%–500%
- Choice of search engine — DuckDuckGo, Google, Bing, or Brave
- Fullscreen mode
- Set as your default browser from Settings

**The Pegboard** *(home page)*
- Right-click anywhere to open the tile toolbox
- Tiles snap to a grid, can't overlap, and save automatically
- Available tiles: Note, Shortcut, Clock, Label, Divider, Timer/Stopwatch, Recent History, Photo, Weather, System Monitor
- Notes support freehand drawing and inline checklists
- Weather tile pulls from Open-Meteo — animated forecasts, °C/°F, custom location
- System Monitor shows live CPU, RAM, and Flint's own memory footprint

**Extensions**
- Load any unpacked Chrome extension (Manifest V2 or V3) from a local folder
- Each extension shows an Open button that launches its popup — full functionality, no compromises
- Tested with uBlock Origin

**Window**
- Fully borderless with a glass/Mica backdrop
- Drag from anywhere in the chrome bar or tab strip
- Resize from any edge or corner

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

Shortcuts work even when you're focused inside a page, a text field, or a captcha.

---

## Requirements

- Windows 10 or 11
- Microsoft Edge WebView2 Runtime (pre-installed on most Windows 11 machines — [download here](https://developer.microsoft.com/microsoft-edge/webview2/) if needed)

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
- Weather data (Open-Meteo) — only if you use the weather tile

No analytics. No crash reports. No accounts.

| What | Where |
|---|---|
| Bookmarks, history, settings | `%LocalAppData%\Flint\profile.json` |
| Pegboard layout | `%LocalAppData%\Flint\pegboard.json` |
| Ad-block list | `%LocalAppData%\Flint\blocklist.txt` |
| Cookies, cache, extensions | `%LocalAppData%\Flint\WebView2\` |
