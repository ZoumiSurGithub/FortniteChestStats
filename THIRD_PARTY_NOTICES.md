# Third-party notices

FortniteStats includes or uses the following open-source components.

| Component | Author | Licence | Where |
|---|---|---|---|
| [ChestCounter](https://github.com/LED-N/Fortnite-ChestCounter) (replay analysis engine, treasure-hunt catalogue `content.json`) | LED-N | MIT | `Engine/`, `lib/` — licence in `LICENSE-ChestCounter-LED-N-MIT.txt` |
| FortniteReplayDecompressor (patched by LED-N) | Shiqan and contributors | MIT | `lib/` — licence in `lib/LICENSE-FortniteReplayDecompressor`, changes in `lib/PATCHES.md` |
| OozSharp (Kraken/Oodle decompression), C# port of [ooz](https://github.com/powzix/ooz) | Powzix, converted by SL-x-TnT | **GPL-3.0-or-later** | `lib/OozSharp` (licence header in `Kraken.cs`) |
| [SkiaSharp](https://github.com/mono/SkiaSharp) (WebP → PNG conversion of reward images) | Microsoft / Mono | MIT | NuGet package |

## Not included

- **Reward images** of the treasure hunt are **not** distributed in this repository. They are downloaded once on the
  user's PC from the ChestCounter repository (or the address given by the catalogue) and cached locally. They belong
  to their respective owners.
- Fortnite, Epic Games and all related names and images are trademarks of Epic Games, Inc. This project is not
  affiliated with, endorsed by or sponsored by Epic Games.

## Why this project is GPL-3.0

`lib/OozSharp` is licensed under the GNU GPL v3 (or later). The application's executable links it, so the combined
work is distributed under **GPL-3.0-or-later** (see `LICENSE`). The MIT-licensed components above are GPL-compatible
and keep their own copyright notices.
