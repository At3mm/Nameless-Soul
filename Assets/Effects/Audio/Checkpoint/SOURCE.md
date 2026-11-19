# Checkpoint activation sound

Original: health_restore.wav — Magic Words + Healing Sound Effect.
Author: Julie Damsgaard / Spring Spring / Spring Enterprises.
Source: https://opengameart.org/content/magic-words-healing-sound-effect
File: https://opengameart.org/sites/default/files/health_restore.wav
License: CC0 — https://creativecommons.org/publicdomain/zero/1.0/

The original WAV is retained as CheckpointAwaken.wav. Playback pitch is 0.9; audio samples are unchanged.

DefaultCheckpointAudio.asset controls clip, volume and pitch. CheckpointRuneVisuals plays it at BeginReveal, after the first camera movement; PrepareActivation, CompleteActivation, skip and disable stop the voice. It runs during timeScale=0 and follows master/menu SFX volume. Already-unlocked statues open their normal menu without replaying activation audio.
