# TapeToTapeStatsMod

MelonLoader mod for Tape to Tape that reports runs, as they are played, to the Tape to Tape Stats API
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
skipGoalReplays = true
```

- `apiKey`: an API key from your Tape to Tape Stats account. While it is empty, runs wait in the outbox
  and are sent once a key is set.
- `apiBaseUrl`: use `https://api.test.tapetotape.axbolduc.com` for preview or `http://localhost:8787` for `pnpm dev`.
- `skipGoalReplays`: skip goal replays as soon as they start.

All three can also be set in-game on the Stats+ tab under Settings: "Stats API key" pastes
the key from the clipboard, "Stats server" cycles Production / Preview / Local (or a custom URL from the cfg)
and "Skip goal replays" toggles On / Off.
In-game changes save to the cfg and apply immediately; edits to the cfg file itself need a game restart.

## What gets reported

The mod reports a run while it is played, not just when it ends. Each time an event below is recorded (picking a
node, finishing a match, choosing a reward or an event card, ...), it waits until nothing has happened for two
seconds, then sends the whole run as it stands with `IsCompleted: false`. The site shows these runs as **LIVE**.
When the run ends, the final payload is sent with `IsCompleted: true` and the result. Each payload replaces the
previous one for that run. The API won't let a late snapshot reopen a finished run, and win rates count finished
runs only.

Every payload has:

- **Run snapshot**: squad, difficulty, game rule (timer or max goals), run time, conceded or not, the full
  roster (lineup, bench, goalie, and departed skaters) with stats, talents, abilities, and bench powers, plus
  the team's relics.
- **Every match**: score, win/loss, shootout, boss, the map node it was played on, and per-skater and goalie stats.
- **Run totals**: per-skater goals, assists, shots, hits, injuries, and goalie stats.
- **Decision timeline**: each event below, in order, with the act and floor it happened on.

| Event                    | Hook                                                          | Data                                            |
| ------------------------ | ------------------------------------------------------------- | ----------------------------------------------- |
| `node_selected`          | `CampaignState.SetCurrentNode`                                | node type, act, floor, map position             |
| `match_ended`            | `UnityAnalyticsService.LogEndOfGameEvent`                     | scores, shots, team stats, lineup, relics, save-scum count |
| `powerup_reward_applied` | `SkaterPowerupRewardMenu.ApplyCurrentSelection`               | chosen talent/ability/bench power, skater, everything offered |
| `relic_reward_applied`   | `RelicRewardMenu.ApplyCurrentSelection`                       | chosen relic, everything offered                |
| `game_reward_selected`   | `UnityAnalyticsService.LogGameRewardSelectedEvent`            | the game's own record of the pick (or skip)     |
| `event_choice_made`      | `EventChoiceMenu.Choose`                                      | node type, chosen card, every card offered      |
| `event_choice_skipped`   | `EventChoiceMenu.LogRewardSkippedAnalyticsEvent`              | node type                                       |
| `event_reward_selected`  | `UnityAnalyticsService.LogEventRewardSelectedEvent`           | the game's own record of the event choice       |
| `choices_rerolled`       | `RerollChoicesNow` / `RerollChoicesInPlace` on the reward and event menus | which menu, reroll count            |
| `unlock_earned`          | `UnityAnalyticsService.LogUnlockEarnedEvent`                  | what unlocked and why                           |
| `run_completed`          | `UnityAnalyticsService.LogRunCompletedEvent`                  | won, time taken                                 |

The `UnityAnalyticsService` hooks mirror the game's built-in analytics events. Those are virtual calls, so
they can't be inlined past the patch, and the game builds them whether or not analytics data leaves the
machine. The menu hooks add detail the analytics strings leave out, like which skater got a talent and every
option that was offered. Some decisions show up under both kinds of event on purpose: if a game update breaks
one hook, the other still records the decision.

Events are appended as they happen to `UserData/TapeToTapeStats/events/<runId>.jsonl`, so a run survives quitting
and resuming. Logs are kept for 30 days after their last event.

Each payload is saved to `UserData/TapeToTapeStats/outbox/<runId>.json` before it is sent (replacing that run's
previous one), and deleted once the API accepts it. Sends go through a single queue, so a run's payloads arrive in
order, and each send reads the run's latest payload, so a backlog (offline, API down) collapses into one request.
Anything still in the outbox is retried with the run's next update or on the next launch. Payloads the API rejects
as invalid (HTTP 400) move to `outbox/rejected/` and aren't retried.

## Debug

Launch the game with `--melonloader.debug` to log each run's JSON payload before it is sent, and each event as it
is recorded. Logs are in `<GamePath>/MelonLoader/Latest.log`.

## Layout

- `Core.cs`: MelonLoader entry point. `Core.ModVersion` is sent with every run.
- `Patches/`: Harmony patches. `RunEndPatches` captures the run when the game ends it (`CampaignEndStep` for runs that end
  in a match, `RunEndHandler` for runs that end on the map); `AnalyticsPatches` and
  `DecisionPatches` record the decision timeline; `SettingsMenuSpikePatch` injects the in-game settings rows and
  Stats+ tab.
- `Tracking/`: the per-run event log, the `RunTracker` the patches record through, and the `RunSnapshotScheduler`
  that sends a snapshot after each burst of events (ticked from `Core.OnUpdate`). Tracking failures are logged
  and never reach the game.
- `Mapping/`: converts Il2Cpp game objects into DTOs. Read game state only on the main thread.
- `Domain/DTOs/`: wire format. Property names must match `runDtoSchema` in `packages/contracts/src/runIngest.ts`.
- `Clients/`, `Services/`: HTTP client, preferences, and background run reporting with the outbox.
- `Polyfills/`: works around Unity's public `NullableAttribute` clashing with the compiler (CS0656).
