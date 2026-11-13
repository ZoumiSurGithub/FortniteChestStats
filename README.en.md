# FortniteStats

[🇫🇷 Français](README.md) · 🇬🇧 English

Windows app (WPF, .NET 10) that reads the **Fortnite replays** on your PC and shows, after every match, everything you
searched (chests, ammo boxes, other containers) along with your match stats. An **overlay** on top of the game shows
the latest match, and a dedicated tab tracks the **treasure hunt**.

> Unofficial project, not affiliated with Epic Games. Everything is analysed **locally** on your PC.

## Features

- **Games**: total searched, chests / ammo / others, placement, eliminations, duration, team chests, treasures received, history and running totals.
- **Overlay** on top of the game (click-through): `Ctrl+F7` hide / show, `Ctrl+F8` quit.
- **Treasure hunt**: chests searched, rewards with images, per-chest odds and cumulative chance, rewards you can tick by hand.
- **My account** (optional): lifetime account stats through the public [fortnite-api.com](https://fortnite-api.com) API (free key, no Epic password).
- Day / night mode, animations, **French / English / Spanish**.
- Automatic monitoring: one check at start-up, then only the latest replay.

## Install

1. Download `FortniteStats.exe` from [**Releases**](../../releases) (no installer, .NET included).
2. In Fortnite, enable replays: **Settings → Game → Replays**.
3. Start the app, click **Start**, play a match: stats show up as soon as it ends.

> Windows SmartScreen may warn on first launch (unsigned executable): “More info” → “Run anyway”.

## Build it yourself

Requirements: Windows 10/11 and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bat
CREER_EXE.bat
```

or

```bat
dotnet publish App\FortniteStats.csproj -c Release -o publish
```

The executable is `publish\FortniteStats.exe`.

## Data and privacy

| Data | Where | Sent to |
|---|---|---|
| Fortnite replays | `%LOCALAPPDATA%\FortniteGame\Saved\Demos` (read-only) | nobody |
| History, totals, settings | `%APPDATA%\FortniteStats` | nobody |
| Reward images | downloaded once from GitHub, cached in `%APPDATA%\FortniteStats\images` | — |
| Username + API key (optional “My account” tab) | `settings.json` (plain text) | fortnite-api.com only |

## Limitations

- Only replays still on disk can be read (Fortnite deletes the oldest ones).
- Epic's official API exposes neither chests nor treasures: that data only comes from replays.
- A treasure is detected only if it drops from a golden chest during the match; other rewards can be ticked by hand.
- Displayed odds do not account for “Treasure Hours”.

## Credits

The analysis engine is [ChestCounter](https://github.com/LED-N/Fortnite-ChestCounter) by **LED-N** (MIT), based on
FortniteReplayDecompressor. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Licence

[GPL-3.0-or-later](LICENSE): the engine includes OozSharp (GPL-3). Other third-party components (MIT) keep their own licences, see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
