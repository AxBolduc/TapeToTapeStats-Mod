# TapeToTapeStatsMod

MelonLoader mod for Tape to Tape that reports finished runs to the Tape to Tape Stats API
(`POST /api/v1/ingest/runs`). Not part of the pnpm/turbo graph.

## Prerequisites

- .NET SDK 8 or newer (the mod targets `net6.0`, which is what MelonLoader's CoreCLR host runs).
- Tape to Tape with MelonLoader 0.7.x installed. Launch the game once so MelonLoader generates
  `MelonLoader/Il2CppAssemblies/`; the project references every assembly in that folder.

## Point the build at your game install

The project defaults to `C:\Program Files (x86)\Steam\steamapps\common\Tape to Tape`. Override it with any of:

- the `TAPE_TO_TAPE_DIR` environment variable,
- `dotnet build -p:GamePath="D:\SteamLibrary\steamapps\common\Tape to Tape"`,
- a gitignored `TapeToTapeStatsMod/TapeToTapeStatsMod.csproj.user`:

  ```xml
  <Project>
    <PropertyGroup>
      <GamePath>D:\SteamLibrary\steamapps\common\Tape to Tape</GamePath>
    </PropertyGroup>
  </Project>
  ```

## Build

```sh
dotnet build apps/mod/TapeToTapeStats.sln
```

Each build copies `TapeToTapeStatsMod.dll` into `<GamePath>/Mods`. Pass `-p:DeployToGame=false` to skip that,
e.g. while the game is running and has the DLL locked.

After a game update, relaunch the game so MelonLoader regenerates the Il2Cpp assemblies, then rebuild. Patches
that target renamed game methods fail to compile instead of failing silently at runtime.

If regeneration fails with `Error Generating Interop Assemblies!` (a `NullReferenceException` inside
`Il2CppInterop.Generator`), delete `<GamePath>/MelonLoader/Dependencies/Il2CppAssemblyGenerator/Cpp2IL/cpp2il_out`
and relaunch. MelonLoader doesn't clear that folder between game versions, so stale dumps break generation,
especially after switching Steam branches ([LavaGang/MelonLoader#1192](https://github.com/LavaGang/MelonLoader/issues/1192)).

## Configure

On first launch the mod adds a section to `<GamePath>/UserData/MelonPreferences.cfg`:

```toml
[TapeToTapeStats]
apiKey = ""
apiBaseUrl = "https://api.tapetotape.axbolduc.com"
```

- `apiKey`: an API key from your Tape to Tape Stats account. Runs are not reported while it is empty.
- `apiBaseUrl`: use `https://api.test.tapetotape.axbolduc.com` for preview or `http://localhost:8787` for `pnpm dev`.

Both can also be set in-game under Settings, on the System tab or the Stats+ tab: "Stats API key" pastes the
key from the clipboard and "Stats server" cycles Production / Preview / Local (or a custom URL from the cfg).
In-game changes save to the cfg and apply immediately; edits to the cfg file itself need a game restart.

## Debug

Launch the game with `--melonloader.debug` to log each run's JSON payload before it is sent. Logs are in
`<GamePath>/MelonLoader/Latest.log`.

## Layout

- `Core.cs`: MelonLoader entry point.
- `Patches/`: Harmony patches. `RunEndHandlerPatch` captures the run when the game ends it; `SettingsMenuSpikePatch`
  injects the in-game settings rows and Stats+ tab.
- `Mapping/`: converts Il2Cpp game objects into DTOs. Read game state only on the main thread.
- `Domain/DTOs/`: wire format. Property names must match `runDtoSchema` in `packages/contracts/src/runIngest.ts`.
- `Clients/`, `Services/`: HTTP client, preferences, and background run reporting.
- `Polyfills/`: works around Unity's public `NullableAttribute` clashing with the compiler (CS0656).
