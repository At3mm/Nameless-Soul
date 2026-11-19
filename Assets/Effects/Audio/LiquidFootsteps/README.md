# Liquid footsteps
Water: Footsteps on different surfaces, mastered by congusbongus.
https://opengameart.org/content/footsteps-on-different-surfaces
Derived from Water Footsteps by EminYILDIRIM and footstep-concrete.wav by swuing, CC BY 3.0. Full original attribution in Water/license.txt. Credit these authors and link https://creativecommons.org/licenses/by/3.0/ in distributed game credits.

Poison: Mud_Mono_01..05, Footsteps Mini Sound Pack by MGW Sound Design.
https://mgw-sound-design.itch.io/footsteps-mini-sound-pack
Author confirms royalty-free personal/commercial use; attribution optional in the pack's page comments.

Five variants for each liquid. Existing footstep cadence, volume and enemy 40% gain are retained. Surface selection follows trigger contacts, with Poison taking priority over Water. It never uses the active poison debuff to choose ground sound. Water and poison share their source clips between walking and running, using the existing faster run cadence.

Update: Lakes now exclusively uses CleanWater/WaterStep1 and WaterStep2, derived from Peludo's splash recordings (CC0). The old water samples remain assigned only as poisonSplash for the 60% mud / 40% splash poison blend, so the two pools have distinct sound identities.
Poison duration is held while a live PoisonZone exposes the actor. DOT continues at normal percent-HP rate. Countdown resumes upon leaving all exposing pools, disabling the source, or losing valid collider contact. Multiple pools are tracked independently. Other continuous status sources can use CharacterDebuffs.SetExposure.
