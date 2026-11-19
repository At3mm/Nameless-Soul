# Paper Book — Settings UI assets

Imported from `Super Asset Bundle #2 - Adventure Time v1.5/Updated Paper Book`.

- `Sprites/Content`: buttons, progress bars, icons, holders, highlights and side tabs.
- `Sprites/InventoryBook`: book pages, opening/closing, page flips and tab animation frames.
- `Sprites/BookDesk`: desk backgrounds.
- `SpriteSheets`: original combined sheet, retained as a reference.
- `Source/Aseprite`: editable source artwork.

Numbered PNGs use a category prefix and zero-padded frame number, such as `Buttons_01.png` and `Open_01.png`. Folder names omit spaces and category ordering numbers. `ImportManifest.json` maps every original file to its imported location and records its SHA-256 hash. All 521 PNGs and 4 Aseprite files retain their original contents.

Unity PNG import: Sprite (2D and UI), Single, Full Rect, Point filtering, no compression, no mipmaps, alpha transparency, 100 pixels per unit. The combined sheet uses a maximum size of 8192 to preserve the original resolution. Use individual sprites with UI Image and Preserve Aspect; use integer display scaling for crisp pixel art.

This folder supplies artwork for the Settings UI. Interactive settings controls, saved preferences and book animation clips have not been created as part of this import.

