# Building

- Set `ValheimPath` in `src/AdvancedPlanting/Local.props` (or the `VALHEIM_INSTALL` environment variable).
- `dotnet build src/AdvancedPlanting -c Release` builds the DLL and the Thunderstore package in `dist/`.
- `-p:CopyToGame=true` also copies the build into `BepInEx/plugins/AdvancedPlanting`.
- Tests: `dotnet test tests/AdvancedPlanting.Core.Tests`.
