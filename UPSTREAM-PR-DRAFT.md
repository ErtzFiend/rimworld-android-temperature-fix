## Summary

This draft proposes a narrowly targeted workaround for ChjDroid pawns that develop hypothermia/heatstroke. The underlying cause has not been confirmed; the change should not be presented as a root-cause fix.

## Changes

- Block hypothermia and heatstroke severity adjustments for pawns with the exact defName `ChjDroid`.
- Remove existing Hypothermia and Heatstroke hediffs from those pawns during health interval ticks.
- Do not target other pawn defs.

## Verification

The mod assembly was compiled against local RimWorld assemblies. Targeted tests are described in `Tests/README.md`; they are not a substitute for live gameplay testing. No in-game behavior, save compatibility, or compatibility with a full active mod list is claimed as verified. Please review the patch/API compatibility and test in-game before merging.

## Notes for review

The root cause of the observed temperature illnesses remains uncertain. This patch targets illness severity rather than establishing why the behavior occurs.
