# Shared pixel animation cadence

`Assets/Resources/DefaultPixelAnimation.asset` is the single presentation clock (12 FPS).

PixelPresentation restores authored world-sprite values before gameplay Update, then holds their rendered values on the shared clock after LateUpdate. Animator evaluation, animation events, hit markers, physics, navigation, dash distance, invulnerability and cooldowns retain their existing timing. Existing animation clips and events are not rewritten. Clips with fewer poses retain those poses; the renderer presents them on the shared 12 FPS cadence.

Sprite renderers are discovered automatically on scene load and every 0.25 seconds (including newly spawned and inactive objects). UI animations sample their own timelines through `PixelAnimationSettings.Sample/Progress` or UIFrameAnimation. Interactive rects, scrolling, slider values and selection colors are never restored by the sprite clock: doing so can overwrite input and layout changes. UI timelines use unscaled time. Water/poison materials read the same clock through `_PixelAnimationFPS`.

New procedural effects should use `PixelAnimationSettings.FPS` / `Sample(time)` rather than independent FPS constants. For custom shader animation, sample time through `_PixelAnimationFPS`. Physics and audio must not use this visual clock.

Procedural sprites that animate while gameplay is paused must register with `PixelPresentation.SetUnscaled(renderer, true)` and unregister on disable. CheckpointRuneVisuals uses this for its unscaled activation reveal. Animator-driven sprites already use the unscaled clock when their Animator has UnscaledTime update mode.

## Dash asset

### Prefab and editable animation

- Reusable prefab: `Assets/Effects/Prefabs/DashEffect.prefab`.
- Animation clip: `Assets/Effects/Animations/Dash/DashEffect.anim` (3 original frames, 12 FPS, 0.25 seconds).
- Animator controller: `Assets/Effects/Animations/Dash/DashEffect.controller`.

Drag the prefab into a scene or instantiate it at the effect position. It plays on enable and deactivates at the end; enable the same instance again to replay. Rotate its Z axis to orient the streak. For the same orientation as the player dash trail, use the direction opposite movement, rounded to 45 degrees. Spawn at the actor's feet plus the `DefaultDashEffect.offset`; match the actor's sorting layer and use sorting order one below the actor. The prefab has no collider, damage, movement or sound logic.

Select a placed prefab and open Animation Window to edit the new effect clip. These new assets are an independent animation snapshot; changes to this clip do not rewrite the player's existing `PlayerDash.anim`, animation events, or the frame array in `DefaultDashEffect`. To change the player's emitted effect, edit that source asset as described below. `Tools → Effects → Create Reusable Dash Effect` finds the saved prefab or creates missing assets; it preserves existing clips and prefab edits.

`Assets/Effects/Resources/DefaultDashEffect.asset` contains the dash frame sequence, unlit material, tint, scale, foot offset and emission interval/spacing. Player prefab → PlayerMovementVFX → Dash Effect references it. Source PNG frames are in `Assets/Effects/Sprites/Dash`; replace/reorder them in the asset to customize the effect. Frames use Point filtering and 32 PPU. Playback follows the shared FPS. Combat/buff/sprint accents still use their own existing art; dash configuration does not change movement logic.
