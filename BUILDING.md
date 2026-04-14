# Building Instructions

## Overview
This repository is intended to let reviewers verify the distributed `ZZZxSteel-405SlotBackpack` mod package.

The repository contains:
- the distributable mod files
- the compiled release DLL included in the packaged mod
- the decompiled C# source files exported from the DLL
- the exported project file (`0xSteel-450SlotBackpack.csproj`)
- build and packaging documentation
- reference SHA256 hashes

## Build environment
- OS: Windows 10/11
- IDE: Visual Studio 2022 (recommended)
- Target framework: `net48`
- Build configuration: `Release`
- Language version in project: `14.0`

## Required references
The project references local game / Unity assemblies that are not redistributed in this repository.
Reviewers should reference these from their own game installation if rebuilding is required.

Project references listed in the exported `.csproj`:
- `System.Runtime.Serialization`
- `0Harmony`
- `Assembly-CSharp`
- `UnityEngine.CoreModule`
- `UnityEngine.InputLegacyModule`

Do not commit proprietary game assemblies unless redistribution is allowed.

## Source location
The exported source and project are located here:

`src/0xSteel-450SlotBackpack/`

## Build steps
1. Open `src/0xSteel-450SlotBackpack/0xSteel-450SlotBackpack.csproj` in Visual Studio.
2. Re-add any required local references from the game installation folder if Visual Studio does not resolve them automatically.
3. Build in `Release` mode.
4. Copy the resulting DLL into:
   `mod/ZZZxSteel-405SlotBackpack/`

## Packaging steps
1. Confirm the DLL is present in:
   `mod/ZZZxSteel-405SlotBackpack/0xSteel-450SlotBackpack.dll`
2. Confirm all XML/config/script files are present.
3. Zip the contents of `mod/ZZZxSteel-405SlotBackpack/` for release.

## Configuration script behavior
Users can edit `config.json` and then run:
- `apply_config.bat`

This launches:
- `apply_config.ps1`

The script updates:
- `Config/XUi/windows.xml`
- `Config/entityclasses.xml`
- `Config/progression.xml`

In `progression.xml`, the script updates the `perkPackMule` progression values so they stay synchronized with the configured backpack size.

## Verification hashes
Reference hashes are stored in:
- `builds/SHA256.txt`
