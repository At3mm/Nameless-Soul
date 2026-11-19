# OrcLair — unified Tile Palette

Only the later simplified Wasteland-style artwork is used: muted gray-brown colors, low detail, Point filtering, 16 pixels per unit and 16x16 tiles.

## Paint in Unity

1. Open Window > 2D > Tile Palette and choose **OrcLair**.
2. Choose the destination Tilemap as Active Tilemap.
3. Select tiles in the palette and paint.

Palette arrangement:
- Left block, columns 0–7: cave floor/rock rim, entrance, horizontal and vertical spike barricades, Orc hide tent. Most whole modules occupy 2x2 cells.
- Two empty columns separate the groups.
- Right block, columns 10–17: four ruined buildings. Each whole building occupies a 4x4 block. Drag a rectangular selection over all 4x4 cells to paint a complete building, or use individual fragments.

Use Ground for floors, GroundObstacles or Walls_Cliffs for blocking pieces, and Overhead for upper pieces that should cover characters. Tiles retain their original collider settings; collisions require a TilemapCollider2D on the destination Tilemap. CaveEntrance is artwork only, without scene-transition logic.

Paint a floor base under the central cave area before placing rim pieces. Existing Wasteland contents are unchanged.

## Contents

- `Palettes/OrcLair.prefab`: the only Tile Palette, with all 128 tiles.
- `Sprites/`: the two simplified 128x128 pixel atlases.
- `Tiles/Terrain`: 64 terrain/prop fragments.
- `Tiles/Ruins`: 64 ruined-building fragments.
- `Source/`: the later simplified generated source atlases only.

The previous two separate palettes and eight unused standalone prop/building prefabs have been removed. Before removal, all scene dependencies and loaded prefab instances were checked; none used those prefabs.

## Art references

Internet references were used for general subject ideas; their pixels are not included:
- https://comshadow.itch.io/goblin-lair-topdown-pixel-art-tileset
- https://comshadow.itch.io/post-apocalyptic-forest-pixel-art-tileset

Primary style reference: existing project `Assets/ElvGames/Wastelands - Rogue Adventure/Sprites/RA_Wasteland.png`. New artwork was made with built-in image_gen; prompts are in GenerationPrompts.md. Images are packed and sliced using native Unity processing.

Palette visibility fix: the OrcLair palette uses Materials/PalettePreview_Unlit.mat (Sprite-Unlit-Default), so preview does not depend on scene lighting. Grid uses manual 1x1 cells with zero Z size. Painted map tiles retain destination Tilemap materials. Wasteland Global Light 2D now includes GroundObstacles (sorting ID 1012); this fixes black silhouettes on that new layer without changing painted tile content.
