using HarmonyLib;
using Verse;

namespace AndroidsHypothermiaFix
{
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap() => new Harmony("org.androids.droidtemperaturefix").PatchAll();
    }

    // Keep the temperature givers running: HediffGiver_Heat also applies ambient-air
    // burn damage. Block only severity adjustments for the two temperature illnesses.
    [HarmonyPatch(typeof(HealthUtility), nameof(HealthUtility.AdjustSeverity))]
    internal static class DroidTemperatureSeverityPatch
    {
        static bool Prefix(Pawn pawn, HediffDef hdDef)
        {
            if (pawn?.def?.defName != "ChjDroid") return true;
            return hdDef != RimWorld.HediffDefOf.Hypothermia && hdDef != RimWorld.HediffDefOf.Heatstroke;
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.HealthTickInterval))]
    internal static class DroidExistingTemperatureConditionsPatch
    {
        static void Prefix(Pawn_HealthTracker __instance)
        {
            if (__instance == null || __instance.hediffSet?.pawn?.def?.defName != "ChjDroid") return;
            Hediff cold = __instance.hediffSet.GetFirstHediffOfDef(RimWorld.HediffDefOf.Hypothermia);
            if (cold != null) __instance.RemoveHediff(cold);
            Hediff heat = __instance.hediffSet.GetFirstHediffOfDef(RimWorld.HediffDefOf.Heatstroke);
            if (heat != null) __instance.RemoveHediff(heat);
        }
    }
}
