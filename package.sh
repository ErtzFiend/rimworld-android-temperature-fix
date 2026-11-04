#!/usr/bin/env sh
set -eu

ROOT=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
OUT="$ROOT/bin/Release/net10.0/AndroidsHypothermiaFix.dll"
DEST="$ROOT/Assemblies/AndroidsHypothermiaFix.dll"
PROJECT="$ROOT/AndroidsHypothermiaFix.csproj"

if [ ! -f "$PROJECT" ]; then
  printf '%s\n' 'Project file AndroidsHypothermiaFix.csproj is not present.' >&2
  exit 2
fi

if [ -z "${RIMWORLD_MANAGED:-}" ]; then
  printf '%s\n' 'Set RIMWORLD_MANAGED to a directory containing the RimWorld 1.7 game DLLs and 0Harmony.dll first.' >&2
  exit 2
fi
if [ ! -f "$RIMWORLD_MANAGED/Assembly-CSharp.dll" ] || [ ! -f "$RIMWORLD_MANAGED/0Harmony.dll" ]; then
  printf '%s\n' "Required game/Harmony references missing from: $RIMWORLD_MANAGED" >&2
  exit 2
fi

dotnet build "$ROOT/AndroidsHypothermiaFix.csproj" -c Release -p:GameManagedDir="$RIMWORLD_MANAGED"
mkdir -p "$ROOT/Assemblies"
install -m 0644 "$OUT" "$DEST"
printf 'Packaged mod assembly: %s\n' "$DEST"
