# Necromancer effects

Character prefab: `Assets/Prefabs/Characters/Necromancer.prefab` (Boss).
Character and effect sprites retain their existing slicing and pixels per unit.
All new clips and effect playback use 12 FPS.

- Attack01: `ExecuteHit` with String `NormalAttack`. The slash is already in the character sprites.
- Attack02: `SpawnSkillEffect` with String `HeavyAttack`. Spawns `FallingBlast.prefab` at the target's position on release, then hits on effect frame 3 (zero-based).
- Summon: `SpawnSkillEffect` with String `Summon`. Spawns `RitualCircle.prefab` at the target's position on release, then activates on effect frame 5 (zero-based).

The target can move away after release to dodge. Each cast releases its effect once.
Effect assets are under `Assets/Prefabs/Effects/Necromancer/`. Edit their Frames, Activation Delay, Lifetime and optional Summoned Enemy in the Inspector. The Summoned Enemy slot references NecromancerBat.prefab. The ritual summons a bat instead of applying area damage. Each ritual creates 2 bats; Summon Limit is 0 (no total cap). Summons expire after 30 seconds or when their caster dies or is disabled, and do not yield currency or respawn at checkpoints.

To adjust the release frame, move the character clip's Animation Event. To adjust the actual impact, change the effect's Activation Delay. Do not add ExecuteHit to Attack02 or Summon: these effects call the shared damage system themselves.

Tools > Enemies > Create Missing Necromancer Assets creates missing assets only and preserves existing clips, markers, controller and prefabs.

Verified in Play Mode: one release per cast, dodging the locked position, melee and both spell impacts, cancellation on death/break, pause, and blocked line of sight. Test actors were removed and save data restored.
