# Nexus Review Notes

This repository was prepared to support review and verification of the distributed mod package.

## Included for review
- Full distributable mod files
- Config script files (`.bat` + `.ps1`)
- Decompiled DLL source files (`.cs`)
- Exported project file (`.csproj`)
- Reference SHA256 hashes
- Build instructions

## Why antivirus scanners may flag the package
Possible reasons include:
- presence of a compiled DLL
- presence of a batch file
- presence of a PowerShell script

These files are included to support mod functionality and user configuration.

## Hash verification
Reviewers can compare a locally built DLL and/or repackaged mod archive against the SHA256 values in `builds/SHA256.txt`.
