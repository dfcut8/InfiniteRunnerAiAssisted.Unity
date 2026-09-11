# Art provenance

`UnicornRuinsSource.png` is original artwork generated with the built-in ImageGen tool for this project. The user's first reference defined the runner concept; the second reference defined coarse pixels, mossy ruins, dark backgrounds, and limited color. Neither screenshot is included in the game's assets.

The production PNG sprites are prepared by `RunnerArtBuilder`: each source cell is fitted to a 32×32 canvas, sampled with nearest-neighbor sampling, mapped to the eight colors below, and given binary transparency. Running poses share a bottom-center pivot. Terrain tiles fill their cell for seamless repeating surfaces. Relics occupy a smaller region of their canvas. Original 5×7 font glyphs are encoded in the builder and baked as a static TMP font asset.

| Color | Hex |
| --- | --- |
| Near-black | `#101419` |
| Charcoal | `#252d32` |
| Stone gray | `#59615d` |
| Dark brown | `#594b36` |
| Ochre | `#b58a43` |
| Moss green | `#628548` |
| Pale green | `#b5cf83` |
| Ivory | `#f3efd9` |

## ImageGen prompt

Create a production pixel-art sprite sheet for an original side-view white unicorn endless runner in mossy castle ruins. Strict 8 color palette only: #101419 near black, #252d32 charcoal, #59615d stone gray, #594b36 dark brown, #b58a43 ochre gold, #628548 moss green, #b5cf83 pale green, #f3efd9 ivory. Transparent background, hard square pixel edges, no gradients or antialiasing. Logical canvas 256x128 pixels, shown enlarged nearest-neighbor 4x to 1024x512. EXACT uniform grid of 8 columns by 4 rows; each cell logically 32x32 (128x128 output). NO labels, text, grid lines, shadows, ground, borders. Top row 8 consecutive frames of a galloping WHITE UNICORN facing RIGHT, prominent horn, flowing pale-green mane/tail, consistent body/pivot placement, hooves near bottom of every cell, about 28 logical pixels wide and 24 high. Second row: same unicorn jumping (col0), falling (col1), horizontal magical dash (col2), stumbling/death (col3), four small separate ivory/moss particle sprites in cols4-7. Third row: col0 modular gray brick platform body tile, col1 moss topped brick platform tile with flat horizontal top, col2 left moss ledge edge, col3 right moss ledge edge, col4 tall 24x30 breakable ochre rune stone obelisk, col5 cracked variant, col6 floating small 10x12 gold diamond relic, col7 small debris fragments. Bottom row: 8 modular dark masonry/ruin decorative tiles including arch stones, pillar, vines, wall bricks, floor stones. This sheet will be sliced into equal cells. Each frame and item must be fully contained in its cell. Keep consistent actual coarse pixel size. Reference mood: classic 8-bit moss-covered gray castle platformer on black background, bright ivory player. Original art, not copied game assets.
