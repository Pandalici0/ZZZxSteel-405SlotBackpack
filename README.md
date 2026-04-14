# ZZZxSteel-405SlotBackpack

This repository contains the distributable mod files for `ZZZxSteel-405SlotBackpack`, the decompiled C# code for the included DLL, and documentation for verification and reproducible review.

## What is included
- `mod/ZZZxSteel-405SlotBackpack/` - the packaged mod files
- `src/0xSteel-450SlotBackpack/` - decompiled DLL code and exported project file
- `builds/SHA256.txt` - reference hashes for verification
- `BUILDING.md` - build and packaging instructions
- `docs/NEXUS-REVIEW-NOTES.md` - notes for Nexus review

## Included source files
The `src/0xSteel-450SlotBackpack/` folder includes:
- `BackpackConfig.cs`
- `BackpackPatches.cs`
- `BackpackPatches_PlayerInventoryBridge.cs`
- `ModApi.cs`
- `PlayerInventoryCtorPatch.cs`
- `AssemblyInfo.cs`
- `0xSteel-450SlotBackpack.csproj`

## Mod features
- Configurable backpack pages/tabs
- Configurable slots per page
- Automatic update of:
  - `Config/XUi/windows.xml`
  - `Config/entityclasses.xml`
  - `Config/progression.xml`
- `apply_config.bat` launcher using PowerShell
- No Python required

## Configuration
Edit:

`mod/ZZZxSteel-405SlotBackpack/config.json`

Then run:

`mod/ZZZxSteel-405SlotBackpack/apply_config.bat`

## Important note for reviewers
The batch file only launches the PowerShell script:
- `apply_config.bat` -> `apply_config.ps1`

The PowerShell script updates XML values based on `config.json` and also adjusts `perkPackMule` progression to match the configured backpack size.
