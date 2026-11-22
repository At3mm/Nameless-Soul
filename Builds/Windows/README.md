# Nameless Soul Windows Release

- Release date: 2026-11-22
- Source: `main` at commit `97ae4464`
- Unity Editor: `6000.3.9f1`
- Target: Windows 64-bit

## Run

Clone this branch with Git LFS and run `Builds/Windows/NamelessSoul.exe`:

```powershell
git lfs install
git clone --branch windows-release-2026-11-22 https://github.com/At3mm/Nameless-Soul.git
cd Nameless-Soul
git lfs pull
.\Builds\Windows\NamelessSoul.exe
```

Keep the complete `Builds/Windows` directory together. The executable depends on its adjacent `NamelessSoul_Data`, `MonoBleedingEdge`, and runtime DLL files; do not move or copy only the `.exe`.

This is the Release build, not the isolated Playtest build. It uses the game's normal `Noname` save identity. Back up existing saves before testing it alongside another Release installation.

Do not merge this distribution branch into source branches. The source of record remains `main`.
