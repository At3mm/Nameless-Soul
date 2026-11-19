# PaperBook — All Icons palette

Animated sequences are now available separately in `Palettes/Animated/PaperBook_AnimatedIcons.prefab` and `PaperBook_AnimatedBook.prefab`. See `Animations/README.md` and `Animations/AnimationCatalog.json`. This palette retains static snapshots of every Content source sprite for manual layout and state selection.

Open `Window > 2D > Tile Palette` and choose **PaperBook_AllIcons**.

- Palette: `Palettes/Static/PaperBook_AllIcons.prefab`.
- Atlas: `SpriteSheets/Icons/PaperBook_Icons_Atlas.png` (220 sliced sprites).
- Crop/source mapping: `SpriteSheets/Icons/IconSlices.json`.
- Tile assets: `Tiles/Static/Icons/<category>/`.
- Palette category positions: `Palettes/Static/PaperBook_AllIcons_Layout.json`.

All 220 PNGs from `Sprites/Content` are included. Sections run from top to bottom: Icons, Items, Equipment, Sidetabs, Signs, Buttons, TreasureChest, DustBin, Highlighter, DayAndNightCycle, ProgressBars, Holders. Heart states and animation frames from Content are included as individual paintable tiles. Large frames and progress bars remain whole pieces.

Each sprite is cropped to its nontransparent bounds with up to one transparent pixel retained around it. Pixel dimensions are preserved; artwork is not scaled. The atlas has two pixels of separation between slices, Point filtering, no compression or mipmaps, centered pivots and 100 pixels per unit. Palette grid cells represent 16 pixels (0.16 Unity units); larger artwork occupies more than one cell.

The atlas and this palette are derived assets. Original PNGs, the PaperBook_Settings palette and existing menu references were left unchanged. Every visible source pixel was compared against the generated atlas and matched exactly.

`Tools/UI/Build-PaperBookIconAtlas.ps1` generates the PNG atlas and crop metadata from the source PNGs. If the source artwork changes, atlas slice metadata and tile layout must also be reimported/rebuilt in Unity.

