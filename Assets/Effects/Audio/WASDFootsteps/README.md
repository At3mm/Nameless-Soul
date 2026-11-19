# WASD walking and running audio
Source: https://wasd-sound.itch.io/free-integrated-footstep-sfx-bundle
Author: WASD Sound / Leon Regula. Free Bundle v3. Included license retained.
48 original WAV clips: 6 variants x Walk/Run x Dirt/Grass/Stone/Wood.
No bundled integration scripts or demo scenes imported.

Runtime: DefaultFootstepAudio controls player volume, enemy multiplier (0.4), audible distance and four surface sets. CharacterFootsteps is automatically added to player and Orc actors by CharacterHealth.Awake, including spawned enemies. Default surface: Dirt; surface can be selected on CharacterFootsteps. No automatic grass/stone tile classification is assumed. Water and poison now use separate sets from LiquidFootsteps; WASD remains the source for dry ground.

Footsteps follow real displacement with distance/cadence gates, stop while idle/dead/stunned/broken/dashing/leaping/in an attack or menu, and ignore teleports. Enemy attacks use the existing Hove swing at the existing damage-frame event. Enemy swing and outgoing hit gain use DefaultCombatAudio.enemyVolume (0.4); combat voices are reserved for player feedback.
