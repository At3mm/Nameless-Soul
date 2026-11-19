# Dark Castle tileset for CaveScene

Source: the user's local `Farm Game Dark Castle Tileset` package by ElvGames. Original files, Read Me and License are preserved in `Source`. Credit ElvGames in the game's credits; consult the bundled License for distribution terms.

## Painting CaveScene

Open Window → 2D → Tile Palette and choose a palette in `Assets/Art/DarkCastle/Palettes`:

- `Cave_FG_Dark_Castle`: red castle walls, floors and decorations.
- `Cave_FG_Dark_Castle_Purple`: purple variant.
- `Cave_FG_Extras`: additional props.
- `Cave_FG_Grounds`: grass/ground edge pieces.

The native sheets use 16×16 cells at 16 PPU, fitting CaveScene's existing 1×1 Grid. Paint floors on Ground, small floor accents on GroundDetails, solid objects on Obstacles, walls on Walls_Cliffs and objects above actors on Overhead. Tile assets use Grid collider geometry; collision only exists on scene tilemaps with collider components. Existing scene content, player, exit and camera are not replaced or painted automatically.

Tile positions in each palette follow the original source sheet. Fully transparent cells are omitted. The palette preview uses an unlit material so the dark art remains visible; gameplay tilemap materials are unchanged.

All imported images use Point filtering, no mipmaps and no compression. RPG Maker size variants remain separate in Source, with 32/48 PPU where appropriate. Native Tilesets are sliced into 16×16 sprites. Map animations are now available in `Palettes/Cave_DarkCastle_Animated.prefab` and `Prefabs/MapAnimations`; see `MAP_ANIMATIONS.md`. Character and item animation/gameplay are not configured.

Tools → World → Create Missing Dark Castle Palettes creates missing tiles/palettes and preserves existing tile and palette edits. It does not replace CaveScene's grid or draw a map.
