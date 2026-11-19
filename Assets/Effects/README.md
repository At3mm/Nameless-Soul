# Effects

Shared location for combat, movement, healing, buff/debuff and water effects.

- Scripts: visual playback, CharacterBuffs/CharacterDebuffs and default configuration classes.
- Sprites: healing and debuff source sprites.
- Animations: healing clip/controller and status overlay materials.
- Prefabs: reusable healing effect.
- Materials / Shaders: water, poison surface and character overlays.
- Resources: DefaultStatusEffects, DefaultWaterEffects, DefaultLiquidSurfaces. Keep these names and the Resources directory: code loads them by name.
- Editor: debuff sprite rebuild tool; writes into Effects/Sprites/Debuffs.

Blood, parry, dash and sprint frames are generated at runtime by Scripts/PixelCombatImpact.cs. They do not require PNG files or prefab attachment.

BuffUseVFX: five color-coded glyph bursts after a successful buff-item transaction. Four frames at 6 FPS, beside the torso with transparent center, following the actor. Reusing an item refreshes the visual instead of adding overlapping copies. No continuous aura or action lock.
