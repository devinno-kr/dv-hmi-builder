# Third-party notices

The dv-hmi plugin ships its command-line tool (`dv-hmi-cli`) in `plugins/dv-hmi/bin/`. To draw pictures of the screens, the tool carries these libraries, which are redistributed here under their own licenses. The full license texts are in `licenses/`.

| Component | Files | License | Full text |
|---|---|---|---|
| SkiaSharp 3.119.4 (Microsoft) | `SkiaSharp.dll` | MIT | `licenses/SkiaSharp-LICENSE.txt` |
| SkiaSharp native library 3.119.4: Skia and the third-party code it includes (ANGLE, FreeType, HarfBuzz, libpng, zlib and others) | `libSkiaSharp.dll` | BSD-3-Clause, MIT and others, per component | `licenses/SkiaSharp-THIRD-PARTY-NOTICES.txt` |
| Font Awesome Free 6.1.2 (Fonticons, Inc.), embedded in `Devinno.UI.dll` | `Devinno.UI.dll` | Fonts: SIL OFL 1.1, icons: CC BY 4.0 | `licenses/Devinno.UI-THIRD-PARTY-NOTICES.md` |

Devinno.UI 0.4.3 (`Devinno.UI.dll`) and `dv-hmi-cli` itself are Devinno's, under the MIT license of this repository (`LICENSE`).

Programs that dv-hmi creates get Devinno.UI and Devinno.Communication from nuget.org as packages; those packages carry their own notices.
