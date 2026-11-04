# Androids Droid Temperature Fix (RimWorld 1.7)

A standalone Harmony mod for Androids (`ChJees.Androids14`). It targets pawns whose exact defName is `ChjDroid`: temperature severity adjustment for hypothermia and heatstroke is blocked, and existing Hypothermia and Heatstroke hediffs are removed during health interval ticks. It does not change ambient temperature or general temperature stats, and does not target other pawn defs.

## Install and use

1. Install RimWorld 1.7 and Androids.
2. Place this mod folder in RimWorld's `Mods` directory. The folder needs `About/About.xml` and `Assemblies/AndroidsHypothermiaFix.dll`.
3. Enable Androids and this fix in the game launcher/mod list, with this fix after Androids. Restart if prompted.
4. Load a save normally. Existing Hypothermia and Heatstroke on ChjDroid pawns are removed during health interval ticks; other conditions are intended to remain unaffected.

This repository includes the compiled mod DLL. It does not include RimWorld or Harmony binaries; install Harmony separately.

## Build

Requires the .NET 10 SDK and local RimWorld 1.7 assemblies. Set `RIMWORLD_MANAGED` to a local directory containing `Assembly-CSharp.dll`, `UnityStubs.dll`, and the Harmony mod's `0Harmony.dll`. In RimWorld 1.7 the game DLLs are in the game installation root, while Harmony's DLL is in its Workshop folder; copy the needed references to a local build-only directory, then run:

```sh
dotnet build -c Release -p:GameManagedDir="$RIMWORLD_MANAGED"
```

The project targets `net10.0` and emits `bin/Release/net10.0/AndroidsHypothermiaFix.dll`. For a local mod install/package, copy the DLL to `Assemblies/AndroidsHypothermiaFix.dll` alongside `About/`. Do not include proprietary game assemblies.

## Verification status

Compilation has been performed against local game assemblies. The targeted tests described in `Tests/README.md` (`dotnet run --project Tests/AndroidsTemperatureFix.Tests.csproj -p:GameManagedDir=/path/to/game_libs`) exercise patch behavior and Harmony target binding against RimWorld 1.7 assemblies (19/19 passed) but do not simulate gameplay. No in-game gameplay or live mod-list compatibility is claimed.
