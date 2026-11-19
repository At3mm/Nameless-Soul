# Jungle animated trees: cell painting

RA_Jungle now displays all 18 animated trees as complete 4x5-cell pictures.
Each palette cell is a separate 16x16 AnimatedTile with the original 12 frames.
The original six-column, three-row arrangement is retained. No separate Root
or Canopy picture is displayed in the palette.

Select an individual cell or drag-select a group of cells. Paint the lower
two rows onto Trees_Roots and the upper three rows onto Trees_Canopy, keeping
the selected portions aligned to the same four-by-five tree grid. Selecting
the whole block paints a complete animated tree onto the active Tilemap.

Each cell keeps its exact source texture and pixel coordinates; SyncAnimation
is enabled and all cells use the same fixed speed. Both scene tree Tilemaps
currently have animationFrameRate = 1.

Assets: Assets/ElvGames/Jungle - Rogue Adventure/Animations/TreeCells.
The original whole-tree animations and source textures are preserved for
existing scene references. Existing scene trees still use their previously
configured root/canopy pairing. JungleTreeLayerPainter handles those legacy
whole-tree references only; it does not reroute the new individual cells.

Validation: 360 cell tiles, 4320 frame sprites, exact 16x16 source rectangles,
matching pixel pivots, original texture references and synchronized timing.
