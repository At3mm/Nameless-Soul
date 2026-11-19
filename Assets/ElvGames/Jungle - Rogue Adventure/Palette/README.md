# RA_Jungle - integrated Tile Palette

Open Window > 2D > Tile Palette and choose RA_Jungle.

- Left: original 1,221 static tiles, original arrangement preserved.
- Upper right: 54 animated 16x16 tiles (two groups), then 12 animated 16x32 tiles.
- Lower right: 18 animated trees, spaced apart so their previews do not overlap.
- Total: 1,305 unique tiles. All 84 animations retain the supplied frames and speeds (4 FPS for small props; 12 FPS for trees when Tilemap Animation Frame Rate is 1).
- Grid: manual 1x1 world units; source sprites: 16 PPU, Point filtering.
- Preview uses a URP 2D unlit material so palette colors do not depend on scene lights. Painted tiles use the destination Tilemap material.

Paint terrain on Ground, small decoration on GroundDetails/Decorations, blockers on GroundObstacles, and foreground foliage on Overhead. Static tiles retain their imported collider setting; animated tiles retain None. Animated trees are visual tiles and do not automatically block movement; add the desired trunk collision separately. Whole animated trees occupy one brush cell but draw a 4x5-cell sprite around it. Select only the tree's anchor cell when picking it.

Tile animations start automatically after painting; no Animator or character component is required. Map layer lighting must include the destination sorting layer. Existing Wasteland scene placement and gameplay have not been changed by this integration.

Corrected supplied Tree 13 and Tree 16 animations: frame 7 was missing and frame 8 was referenced twice. Both now use all 12 source frames in order.
