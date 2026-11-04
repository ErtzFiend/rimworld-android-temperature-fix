# Targeted tests

Targeted behavioral tests against RimWorld 1.7 assemblies and Harmony runtime are located in `Tests/`.
Run with:
```sh
dotnet run --project Tests/AndroidsTemperatureFix.Tests.csproj -p:GameManagedDir=/path/to/game_libs
```

Controls verified:
- `ChjDroid` hypothermia severity adjustment suppressed
- `ChjDroid` heatstroke severity adjustment suppressed
- `ChjDroid` unrelated hediffs (e.g. Flu) retained
- Human hypothermia and heatstroke unaffected
- Existing hypothermia and heatstroke cleared on interval tick
- Null pawn / tracker safety guards
- Harmony target resolution (`HealthUtility.AdjustSeverity`, `Pawn_HealthTracker.HealthTickInterval`) and clean unpatching