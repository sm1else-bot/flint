# Flint

A fast, minimal Windows browser with a glass aesthetic and a freeform home canvas. Built on .NET 8 + Microsoft WebView2 (Chromium).

---

## Overview

Flint is a personal browser built from scratch using WinForms and WebView2. It has no Electron, no bloat, no telemetry. The window is fully borderless with a DWM Mica/Acrylic backdrop — glass panels, custom hit-testing, and all icons drawn in GDI+. The home page is a freeform pinboard called the Pegboard where you place tiles, not a search bar with cards.

It runs as a single self-contained `.exe` (~148 MB, no installer required).

---

## Requirements

- Windows 10 or 11
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (already installed on most Windows 11 machines)

---

## Installation

Download `Flint.exe` from the latest release and run it. No install step. Profile data is stored at `%LocalAppData%\Flint\`.

To build from source:

```
git clone https://github.com/sm1else-bot/flint.git
cd flint
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

Requires .NET 8 SDK.

---

## Features

### Browser

- **Multi-tab** — independent WebView2 instances sharing a single Chromium environment and user data folder. Tabs show favicons (fetched and cached per hostname), an animated Spark loading indicator, and support mute, reload/stop toggle, and a right-click context menu.
- **Smart address bar** — detects URLs, bare hostnames, and search queries. Autocomplete dropdown shows up to 6 history matches (with cached favicons) and inline search suggestions. Auto-focuses on new tab.
- **Ad blocker** — host-based blocking via the StevenBlack unified hosts list, applied at the WebView2 `WebResourceRequested` level before any network request leaves the process. Downloaded on first run, refreshed every 7 days. Toggleable in Settings.
- **Downloads** — intercepts all downloads via `CoreWebView2.DownloadStarting`, suppresses the default UI, and saves to a configurable folder. A floating dropdown shows real-time progress with KB/MB/GB formatting, a cyan progress bar, and Open/dismiss buttons. An active-download badge appears on the toolbar button.
- **Zoom** — `Ctrl++` / `Ctrl+-` / `Ctrl+0`. Step is 0.1×, range 25%–500%. Each tab remembers its own zoom level.
- **Search engines** — DuckDuckGo (default), Google, Bing, Brave. Configurable in Settings → Search.
- **Fullscreen** — `F11` hides all chrome; WebView2 fills the window.
- **Set as default browser** — Settings → Data → "Set as Default" writes the required registry keys and opens `ms-settings:defaultapps`.

### The Pegboard

`flint://home` is a freeform dark canvas overlaid with a subtle 32px dot grid. Right-click anywhere to open the tile toolbox. Tiles snap to a 32px grid with collision detection — no two tiles can overlap. All state persists to `%LocalAppData%\Flint\pegboard.json`.

| Tile | Description |
|---|---|
| **Note** | Sticky note with a `contenteditable` body, monospace font, freehand drawing overlay (velocity-weighted Bézier strokes), and inline checklist syntax (`/c` + Space inserts a checkbox). Resizable. |
| **Shortcut** | 3×3 tile linking to any URL. Pick from 24 inline SVG icons or use the site's favicon. |
| **Clock** | Live `HH:MM` + date display, updates every second. |
| **Label** | Transparent floating text header. Edits in-place. |
| **Line** | SVG divider line drawn by clicking two points. Thickness cycles on click (1–3px). Rotates H↔V with collision check. |
| **Timer / Stopwatch** | Combined widget — stopwatch to centiseconds, countdown timer with visual flash on zero. |
| **Recent** | Scrollable list of your 15 most recent visits, pulled live from history. |
| **Photo** | Polaroid-style photo tile with random tilt, editable caption, aspect-ratio resize, free z-order stacking. |
| **Weather** | 3-day forecast from Open-Meteo with animated condition SVGs (sun/cloud/rain), °C/°F toggle, and custom location. |
| **System Monitor** | Live CPU load, RAM %, Flint memory footprint (main process + all WebView2 children), and a CPU temperature curve. Reads from Win32 `GetSystemTimes` / `GlobalMemoryStatusEx`. |

### Extensions

Flint supports unpacked Chrome extensions (Manifest V2 and V3) via `CoreWebView2Profile.AddBrowserExtensionAsync`. Extensions are loaded with `AreBrowserExtensionsEnabled = true` and persist in the WebView2 user data folder across restarts.

**Settings → Features → Extensions:**
- **Add from folder** — select any folder containing a `manifest.json` in its root.
- **Open** — opens the extension's popup page (`action.default_popup` / `browser_action.default_popup`) in a borderless 500×800px window, centered on the cursor, clamped to the screen. Shares the browser's WebView2 environment so the extension has full access to its own APIs.
- **Remove** — removes from the list; a toast prompts a restart to fully unload.

Compatible with most content-script and `declarativeNetRequest` extensions. Tested with uBlock Origin.

### Window Chrome

Fully borderless (`FormBorderStyle.None`) with a real DWM Mica/Acrylic backdrop via `SetWindowCompositionAttribute` + `DwmSetWindowAttribute`. Semi-transparent glass panels are painted with GDI+. Custom `WndProc` hit-testing handles 8-direction resize with 8px grip zones. All toolbar icons are drawn programmatically as GDI+ bitmaps.

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

Shortcuts are intercepted via a `WH_KEYBOARD_LL` low-level hook and work even when focus is inside a web page, a form field, or after completing a captcha.

---

## Data & Privacy

All data is local. No analytics, no crash reporting, no external connections except pages you navigate to, favicon fetches (`google.com/s2/favicons`), ad-block list refreshes (`raw.githubusercontent.com/StevenBlack/hosts`), and weather data (`open-meteo.com`) if you use the weather tile.

| File | Contents |
|---|---|
| `%LocalAppData%\Flint\profile.json` | Bookmarks, history (max 500), settings, extension paths |
| `%LocalAppData%\Flint\pegboard.json` | Pegboard tile layout and content |
| `%LocalAppData%\Flint\blocklist.txt` | StevenBlack hosts list |
| `%LocalAppData%\Flint\WebView2\` | WebView2 user data (cookies, cache, extensions) |

---

## Stack

- .NET 8, WinForms
- Microsoft WebView2 `1.0.3967.48`
- No third-party NuGet dependencies
