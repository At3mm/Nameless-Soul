# Fantasy UI

Source folder: `C:/Assets File/File/FantasyUI/`.

This pack is separate from `../PaperBook`. Four PNGs were copied without changing their image contents; the macOS `.DS_Store` file was excluded. See `ImportManifest.json` for original names and SHA-256 hashes.

- `SpriteSheets/FantasyUI_Atlas.png`: the original 1024x1560 atlas, retained as a single reference sprite. Individual components can be sliced in Sprite Editor when needed.
- `SpriteSheets/HealthBars/DragonHealthBar_01.png`: original `Dragonhpbar.png`.
- `SpriteSheets/HealthBars/DragonHealthBar_02.png`: original `DragonHpBar2.png`.
- `SpriteSheets/HealthBars/DragonHealthBar_03.png`: original `DragonHpBar3.png`.

Each 896x32 health-bar sheet is sliced into eight 112x32 sprites, named `DragonHealthBar_XX_Stage_00` through `Stage_07`, left to right. These are visual fill stages, not automatically played animation clips.

Import settings: Sprite (2D and UI), Point filtering, no compression or mipmaps, alpha transparency, Full Rect, 100 pixels per unit. No Fantasy UI artwork has been substituted into the existing PaperBook pause menu.
