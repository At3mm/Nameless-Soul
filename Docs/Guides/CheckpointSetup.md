# Placing checkpoints

Drag Assets/Prefabs/World/CheckpointStatue.prefab into a scene, or duplicate an
existing statue. The Editor assigns a unique persistent ID automatically.
Inactive checkpoints are included. Renaming or moving a statue keeps its ID.
Existing scene IDs are adopted unchanged so previously saved unlocks remain valid.

Set CheckpointMenu.checkpointName to a recognizable destination name and adjust
the statue's child RespawnPoint to an unobstructed location. The checkpoint ID
is read-only in the Inspector; display names are independent of save identity.

Uniqueness is scoped to a scene, matching the existing scenePath + checkpointId
save keys. Prefab asset IDs are templates, not identities for new scene instances.
The Editor repairs missing/duplicate scene IDs, retaining the established owner.
Build scene validation rejects any remaining missing/duplicate ID and identifies
its hierarchy path. Build validation never silently rewrites published save IDs.

Runtime-created checkpoints still require an explicitly assigned persistent ID;
Editor generation applies to checkpoints authored in scenes.

Regression checks: Tools > Diagnostics > Test Automatic Checkpoint Identities.
Tests run in a temporary scene, verify prefab creation, inactive duplication,
existing-owner preservation, collision/build rejection, rename/move stability,
and save/reload persistence, then remove the temporary scene.
