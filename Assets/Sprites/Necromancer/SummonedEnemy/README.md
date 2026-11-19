# Necromancer summoned bat

Prefab: Assets/Prefabs/Characters/NecromancerBat.prefab
Controller and clips: Animation/

The supplied Walk/Bat_Flying sprites are used for both idle and movement; no Idle folder is required. Attack and Attack2 supply NormalAttack and HeavyAttack. All clips play at 12 FPS. Existing slicing and 25 pixels per unit were preserved.

Damage events: ExecuteHit / NormalAttack at frame 3; ExecuteHit / HeavyAttack at frame 4 (zero-based). Change the marker positions in the clips as needed. The setup menu preserves existing clips and markers.

EnemyFlight prevents walking sounds, water slowdown and water contact effects. Solid obstacles and line-of-sight rules still apply. Relentless Pursuit uses shared navigation to continuously chase and attack. The bat does not patrol, retreat, strafe or return to a home territory while pursuing a live player. Solid obstacles and line-of-sight checks still apply. The bat prefab has no EnemyBreak component or break bar.

RitualCircle.prefab references this minion. Each ritual creates 2 bats at separate clear positions. NecromancerCombat.Summons Per Cast defaults to 2 and Summon Limit is 0 (no total cap). If there is no room for a complete pair, no partial group is spawned. NecromancerSummonedMinion.Lifetime is 30 seconds of game time. Summons disappear when the caster dies or is disabled; they do not create checkpoint respawn snapshots or award gold/EXP.
