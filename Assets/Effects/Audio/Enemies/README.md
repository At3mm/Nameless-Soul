# Enemy sound effects

Selected CC0 recordings: 39 clips. Sources and authors are in SOURCES.txt, also shipped in StreamingAssets/ThirdPartyNotices/EnemySFX.txt. Original archives and page snapshots remain in References/Audio/EnemySFX outside the Unity build assets.

EnemyAudio on each enemy prefab references an EnemyAudioProfile under Profiles/. Orc and EliteOrc share Orc.asset; Slime, Werewolf, Werebear, OrcRider, Necromancer and NecromancerBat have separate profiles.

Profiles expose Attacks, Hurt, Death, Phase Transition, per-skill overrides, Volume, Pitch, Attack Interval and Hurt Interval. Attack vocals follow existing ExecuteHit events. Necromancer spell cues follow SpawnSkillEffect. Death, confirmed HP damage and boss phase entry trigger their own cues. Existing animation frames and markers were not modified.

Existing weapon swings and HP impact sounds remain separate from creature vocals. Each actor throttles repeated attack/hurt cues. An independent pool limits creature voices to 8, attenuates distant enemies, and pauses with gameplay. Death and phase cues may replace the oldest creature voice when the pool is full. These cues follow DefaultCombatAudio Volume and Enemy Volume plus the game's master volume.

Imported short cues use mono PCM / Decompress On Load. No new sound loops are attached to the repeating damage animation used during phase transitions: its phase cue plays once on entry.

Re-run Tools > Enemies > Create Missing Enemy Audio Profiles only to create missing profiles/bindings; existing profile settings and overrides are preserved.

Verification completed 2026-11-13: all 39 clips decoded with non-zero signal; 7 profiles bound to 8 prefabs. Attack, hurt and death hooks verified, including actual Skill1 names on EliteOrc and Werebear. Necromancer HeavyAttack and Summon animation events played their configured magic clips. The 8-voice cap, critical-voice replacement, distance culling, per-actor throttling and pause were checked in Play Mode. Temporary actors were removed and the pre-test save state restored. These are functional playback checks; no claim of a full listening/mix review is made.

## Expansion — 2026-11-16

41 additional clips from six packs are configured in 12 profiles for 13 main prefabs, including Skeleton and Black Knight variants. These packs use mixed licenses; see Expansion/README.md and StreamingAssets/ThirdPartyNotices/EnemySFXExpansion.txt. The original CC0 source notes above apply only to the original recordings. Weapon profiles can replace the shared swing to avoid stacking.

Play Mode regression passed: decoding, mono signal, attack/hurt/death/phase hooks, skill overrides, friendly-to-hostile Black Knight audio, weapon swing replacement and pause. No runtime errors were captured; the original save and start scene were restored. Animation clips and markers were not edited.
