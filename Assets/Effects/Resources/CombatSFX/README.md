# Combat and status SFX

18 original synthesized mono WAV files, 22,050 Hz / 16-bit PCM. Generated reproducibly by Effects/Editor/GenerateSfx.py using only Python's standard library, with fixed noise seeds, short edge fades and peak headroom. No downloaded recordings or third-party samples are incorporated.

References for design direction only:
- https://kenney.nl/assets/impact-sounds : short impact categories and metal feedback.
- https://sfxr.me/ : retro game synthesis with noise, tone, pitch and envelopes.

3 hit variations; metallic parry; noise dash; 5 ascending buff motifs; burning, poison and stress onset; heal and cleanse.

Tuning: Assets/Effects/Resources/DefaultCombatAudio.asset. Overall volume also follows the existing in-game master-volume slider. Playback has 10 voices, per-category cooldowns and distance attenuation. DOT ticks and debuff-duration refresh do not make repeated sounds. Buff/cleanse/heal item audio only plays on successful use. Custom CharacterParry.parryClangSound overrides the default parry sound.

Player skills: AttackSwing on each NormalAttack damage-frame event; Skill2Cast on accepted Skill2 input; Skill2Swing on Skill2 damage-frame events. Missing skills and dead actors do not emit swing audio. Existing HP impact feedback is reused without an additional skill-impact layer. Swing/skill levels are adjustable in DefaultCombatAudio.


Sword revision: 3 normal and 3 heavy swing variations with moving filtered-noise bands, low air body and quiet inharmonic steel tails. Round-robin variation and small pitch differences do not modify gameplay random state.

AoE-inspired trial: shorter swing envelopes, sharper impact transients, brief inharmonic metal impact tails and shorter parry. Reference discovery: https://www.youtube.com/watch?v=PpA1J-7-P10 and https://www.ageofempires.com/news/age-empires-definitive-edition-audio-update/ . These are original synthesized files, not extracted game recordings. Direct listening comparison against the video was not available. Previous WAVs: Library/DeveloperTools/SwordAudioBeforeAoEStyle.
