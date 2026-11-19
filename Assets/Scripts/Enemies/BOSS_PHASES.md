# Boss phases

BossPhaseController is attached to the Necromancer and OrcRider boss prefabs. Other enemies keep their original movesets. Add this component to another Boss to configure it similarly.

In the Inspector, choose skills using the checkboxes under Phase 1 Skills and Phase 2 Skills. Names come from that object's SkillDamageManager. Lists are independent: phase 2 may include all skills or a completely different subset. An empty list intentionally disables all attacks in that phase.

Phase 2 HP Threshold defaults to 0.5 (50% remaining HP). The boss switches once per encounter at or below this value. Partial healing does not reverse the phase. Disabling/re-enabling the boss, or returning home and fully healing through EnemyClassification.OnReturnedHome, resets it for a new encounter.

New attacks, random openings and combo followups must pass the phase selection. Combos containing locked skills are not selected. On transition, the current attack, combo, knockback, exhaustion stun and break are cancelled. The boss loops its existing damage-reaction animation and stays protected for Transition Duration (default 5 seconds of game time, paused with the game). Attacks, incoming hits, DOT, buildup and physical displacement are blocked until recovery. Cooldowns and original animation clips/events are preserved.

Defaults: first SkillDamageManager skill in phase 1, all configured skills in phase 2. Change the checkboxes on the prefab or on individual scene instances. Runtime Current Phase is shown in the Inspector.

PhaseTransition references the same original clip as TakeDamage (NecromancerHurt / OrcRiderTakeDmg). Its sprite animation is sampled at 12 FPS repeatedly until the transition ends, then the boss resumes idle and combat. Root scale is not changed. The original clips and markers are preserved; their unlock events cannot release the transition lock.
