# UI asset organization

- `Packs/PaperBook`: the Paper Book theme, its source artwork, sprites, sheets, tiles and palette. The pause menu uses this pack.
- `Packs/FantasyUI`: the separate Fantasy UI theme, including its UI atlas and dragon health-bar sheets.
- `Fonts/PixelifySans`: shared UI font and its license.

Working UI prefabs live in `Assets/Prefabs/UI`; runtime UI behavior lives in `Assets/Scripts/UI`; editor tools live in `Assets/Editor`. Theme artwork stays inside its own pack rather than being mixed into feature folders.

Existing PaperBook assets were moved through Unity AssetDatabase, preserving their GUIDs and references. Each pack has its own import manifest and usage notes. Future derived sprites, palettes and animations should remain inside the pack they belong to.
