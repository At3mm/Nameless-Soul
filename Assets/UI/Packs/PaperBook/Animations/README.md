# PaperBook animations

34 sequences / 331 source frames are configured. Every sequence has an AnimatedTile and an AnimationClip targeting UI Image.m_Sprite. Original source sprites keep their common frame canvas, avoiding movement caused by independently trimmed frames.

## Organization

- `Palettes/Static`: PaperBook_Settings and PaperBook_AllIcons, preserving their existing GUIDs.
- `Palettes/Animated/PaperBook_AnimatedIcons.prefab`: 18 animated icons/effects.
- `Palettes/Animated/PaperBook_AnimatedBook.prefab`: 16 book/tab effects.
- `Tiles/Static/Settings` and `Tiles/Static/Icons`: existing static tiles.
- `Tiles/Animated/Icons` and `Tiles/Animated/Book`: animated tile assets.
- `Animations/Icons` and `Animations/Book`: clips for UI Images.
- `AnimationCatalog.json`: source folders, frame counts, playback speed, loop behavior and asset paths.

## Configured sequences

- Hearts: MiddleHeart, Small/Red and Small/Yellow each have Empty, Recover and Shine (9 sequences).
- Props: opening treasure chest and moving dustbin (2).
- Weather: Dawn, Day, Noon, Night, Raining, Lightning1 and Lightning2 (7).
- Book opening/closing: Style1 and Style2 (4).
- Page flipping: left/right for Style1, Style2 and Style3 (6).
- Tabs: Appear/Disappear, Withicons/Withouticons (4).
- Content reveal: Style1 and Style2 (2).

Weather, Shine and DustBin loop. Empty/Recover, chest opening and book transitions play once and hold their final frame. Color/style variants, UI frame pieces and health-bar fill stages remain static.

## Usage

Choose PaperBook_AnimatedIcons or PaperBook_AnimatedBook in Tile Palette, then paint onto a PaintedLayout layer. SettingsMenuToggle creates a UIFrameAnimation for each AnimatedTile when rebuilding the UI. Playback uses unscaled time, including while the game is paused. Reopening the menu restarts one-shot effects.

Live Menu Preview also samples these animations in Edit Mode. For direct UI authoring, use UIFrameAnimation on an Image, or assign the supplied .anim clip to an Animator. Painting a book transition adds a visual effect; navigation and input behavior remain controlled by SettingsMenuToggle/PauseBookNavigation.

For tiles, Min Speed / Max Speed represent frames per second when the target Tilemap Animation Frame Rate is 1 (the supplied palettes use 1). Keep both speeds equal for consistent UI playback. LoopOnce and UnscaledTime flags control one-shot behavior and paused playback. Some one-shot reveal effects end with a fully transparent frame; their tile cell can appear empty after completion, so locate them by asset name when needed.
