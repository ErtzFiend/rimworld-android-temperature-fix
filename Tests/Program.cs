using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Verse;
using RimWorld;

namespace AndroidsTemperatureFix.Tests
{
    class Program
    {
        static int passed = 0;
        static int failed = 0;

        static void Assert(bool condition, string message)
        {
            if (condition)
            {
                Console.WriteLine($"[PASS] {message}");
                passed++;
            }
            else
            {
                Console.WriteLine($"[FAIL] {message}");
                failed++;
            }
        }

        static bool BypassEnsureInitialized() => false;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
            {
                var reqName = new AssemblyName(resolveArgs.Name);
                string candidate = System.IO.Path.Combine(Environment.GetEnvironmentVariable("RIMWORLD_MANAGED") ?? ".", reqName.Name + ".dll");
                if (System.IO.File.Exists(candidate))
                {
                    return Assembly.LoadFrom(candidate);
                }
                return null;
            };

            Console.WriteLine("Running AndroidsHypothermiaFix behavioral test harness...");

            try
            {
                var warnMethod = typeof(Verse.Log).GetMethod("Warning", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (warnMethod != null)
                {
                    var harm = new Harmony("test.log.bypass");
                    var prefix = typeof(Program).GetMethod(nameof(BypassEnsureInitialized), BindingFlags.NonPublic | BindingFlags.Static);
                    harm.Patch(warnMethod, prefix: new HarmonyMethod(prefix));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning patch failed: " + ex.Message);
            }

            try
            {
                var dirtyCacheMethod = typeof(Verse.HediffSet).GetMethod("DirtyCache", BindingFlags.Public | BindingFlags.Instance);
                if (dirtyCacheMethod != null)
                {
                    var harm = new Harmony("test.dirtycache.bypass");
                    var prefix = typeof(Program).GetMethod(nameof(BypassEnsureInitialized), BindingFlags.NonPublic | BindingFlags.Static);
                    harm.Patch(dirtyCacheMethod, prefix: new HarmonyMethod(prefix));
                }
            }
            catch { }

            // 1. Prepare defs and uninitialized instances
            HediffDef hypothermiaDef = (HediffDef)FormatterServices.GetUninitializedObject(typeof(HediffDef));
            hypothermiaDef.defName = "Hypothermia";

            HediffDef heatstrokeDef = (HediffDef)FormatterServices.GetUninitializedObject(typeof(HediffDef));
            heatstrokeDef.defName = "Heatstroke";
            try
            {
                // Try setting field without triggering .cctor if possible or bypass EnsureInitializedInCtor
                typeof(HediffDefOf).GetField(nameof(HediffDefOf.Hypothermia), BindingFlags.Public | BindingFlags.Static)
                    ?.SetValue(null, hypothermiaDef);
                typeof(HediffDefOf).GetField(nameof(HediffDefOf.Heatstroke), BindingFlags.Public | BindingFlags.Static)
                    ?.SetValue(null, heatstrokeDef);
            }
            catch (Exception)
            {
                // Fallback: If cctor fails due to missing Brutal logging assembly in test runner,
                // patch RimWorld.DefOfHelper.EnsureInitializedInCtor or Verse.Log.Warning
            }

            HediffDef fluDef = (HediffDef)FormatterServices.GetUninitializedObject(typeof(HediffDef));
            fluDef.defName = "Flu";

            ThingDef droidDef = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            droidDef.defName = "ChjDroid";

            ThingDef humanDef = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            humanDef.defName = "Human";

            Pawn droidPawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
            droidPawn.def = droidDef;

            Pawn humanPawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
            humanPawn.def = humanDef;

            try
            {
                var deadProp = typeof(Pawn).GetProperty("Dead", BindingFlags.Public | BindingFlags.Instance);
                if (deadProp?.GetGetMethod() != null)
                {
                    var harm = new Harmony("test.pawn.dead.bypass");
                    var prefix = typeof(Program).GetMethod(nameof(BypassEnsureInitialized), BindingFlags.NonPublic | BindingFlags.Static);
                    harm.Patch(deadProp.GetGetMethod(), prefix: new HarmonyMethod(prefix));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Dead patch error: " + ex.Message);
            }

            // Reflect patch methods
            Type severityPatchType = typeof(AndroidsHypothermiaFix.Bootstrap).Assembly.GetType("AndroidsHypothermiaFix.DroidTemperatureSeverityPatch");
            MethodInfo severityPrefix = severityPatchType?.GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            Type existingPatchType = typeof(AndroidsHypothermiaFix.Bootstrap).Assembly.GetType("AndroidsHypothermiaFix.DroidExistingTemperatureConditionsPatch");
            MethodInfo existingPrefix = existingPatchType?.GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            Assert(severityPrefix != null, "Found DroidTemperatureSeverityPatch.Prefix via reflection");
            Assert(existingPrefix != null, "Found DroidExistingTemperatureConditionsPatch.Prefix via reflection");

            // 2. Behavioral tests for DroidTemperatureSeverityPatch
            // Signature: static bool Prefix(Pawn pawn, HediffDef hdDef)
            if (severityPrefix != null)
            {
                // Droid pawn + Hypothermia -> should return false (suppress)
                bool resDroidHypo = (bool)severityPrefix.Invoke(null, new object[] { droidPawn, hypothermiaDef });
                Assert(resDroidHypo == false, "Droid Hypothermia severity adjustment is suppressed (returns false)");

                // Droid pawn + Heatstroke -> should return false (suppress)
                bool resDroidHeat = (bool)severityPrefix.Invoke(null, new object[] { droidPawn, heatstrokeDef });
                Assert(resDroidHeat == false, "Droid Heatstroke severity adjustment is suppressed (returns false)");

                // Droid pawn + Flu -> should return true (unaffected)
                bool resDroidFlu = (bool)severityPrefix.Invoke(null, new object[] { droidPawn, fluDef });
                Assert(resDroidFlu == true, "Droid unrelated hediff (Flu) severity adjustment is unaffected (returns true)");

                // Human pawn + Hypothermia -> should return true (unaffected)
                bool resHumanHypo = (bool)severityPrefix.Invoke(null, new object[] { humanPawn, hypothermiaDef });
                Assert(resHumanHypo == true, "Human Hypothermia severity adjustment is unaffected (returns true)");

                // Human pawn + Heatstroke -> should return true (unaffected)
                bool resHumanHeat = (bool)severityPrefix.Invoke(null, new object[] { humanPawn, heatstrokeDef });
                Assert(resHumanHeat == true, "Human Heatstroke severity adjustment is unaffected (returns true)");

                // Null pawn -> should return true (unaffected, safe guard)
                bool resNullPawn = (bool)severityPrefix.Invoke(null, new object[] { null, hypothermiaDef });
                Assert(resNullPawn == true, "Null pawn returns true (unaffected/safe)");
            }

            // 3. Behavioral tests for DroidExistingTemperatureConditionsPatch
            // Signature: static void Prefix(Pawn_HealthTracker __instance)
            if (existingPrefix != null)
            {
                // Droid tracker
                Pawn_HealthTracker droidTracker = (Pawn_HealthTracker)FormatterServices.GetUninitializedObject(typeof(Pawn_HealthTracker));
                HediffSet droidHediffSet = (HediffSet)FormatterServices.GetUninitializedObject(typeof(HediffSet));
                typeof(Pawn_HealthTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.SetValue(droidTracker, droidPawn);
                droidTracker.hediffSet = droidHediffSet;
                droidHediffSet.pawn = droidPawn;
                droidHediffSet.hediffs = new List<Hediff>();

                // Droid def needs race/body for CacheMissingPartsCommonAncestors
                // Or initialize missingPartsCached etc. if needed
                typeof(HediffSet).GetField("missingPartsCommonAncestorsCached", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(droidHediffSet, new List<Hediff_MissingPart>());

                // Human tracker
                Pawn_HealthTracker humanTracker = (Pawn_HealthTracker)FormatterServices.GetUninitializedObject(typeof(Pawn_HealthTracker));
                HediffSet humanHediffSet = (HediffSet)FormatterServices.GetUninitializedObject(typeof(HediffSet));
                typeof(Pawn_HealthTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.SetValue(humanTracker, humanPawn);
                humanTracker.hediffSet = humanHediffSet;
                humanHediffSet.pawn = humanPawn;
                humanHediffSet.hediffs = new List<Hediff>();
                typeof(HediffSet).GetField("missingPartsCommonAncestorsCached", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(humanHediffSet, new List<Hediff_MissingPart>());

                // Create dummy Hediffs
                Hediff droidHypo = (Hediff)FormatterServices.GetUninitializedObject(typeof(HediffWithComps));
                droidHypo.def = hypothermiaDef;
                droidHypo.pawn = droidPawn;

                Hediff droidHeat = (Hediff)FormatterServices.GetUninitializedObject(typeof(HediffWithComps));
                droidHeat.def = heatstrokeDef;
                droidHeat.pawn = droidPawn;

                Hediff droidFlu = (Hediff)FormatterServices.GetUninitializedObject(typeof(HediffWithComps));
                droidFlu.def = fluDef;
                droidFlu.pawn = droidPawn;

                droidHediffSet.hediffs.Add(droidHypo);
                droidHediffSet.hediffs.Add(droidHeat);
                droidHediffSet.hediffs.Add(droidFlu);

                Hediff humanHypo = (Hediff)FormatterServices.GetUninitializedObject(typeof(HediffWithComps));
                humanHypo.def = hypothermiaDef;
                humanHypo.pawn = humanPawn;

                Hediff humanHeat = (Hediff)FormatterServices.GetUninitializedObject(typeof(HediffWithComps));
                humanHeat.def = heatstrokeDef;
                humanHeat.pawn = humanPawn;

                humanHediffSet.hediffs.Add(humanHypo);
                humanHediffSet.hediffs.Add(humanHeat);

                // Invoke existing conditions patch for droid
                existingPrefix.Invoke(null, new object[] { droidTracker });

                Assert(!droidHediffSet.hediffs.Contains(droidHypo), "Existing Hypothermia removed from ChjDroid");
                Assert(!droidHediffSet.hediffs.Contains(droidHeat), "Existing Heatstroke removed from ChjDroid");
                Assert(droidHediffSet.hediffs.Contains(droidFlu), "Existing Flu preserved on ChjDroid");

                // Invoke existing conditions patch for human
                existingPrefix.Invoke(null, new object[] { humanTracker });

                Assert(humanHediffSet.hediffs.Contains(humanHypo), "Existing Hypothermia preserved on Human");
                Assert(humanHediffSet.hediffs.Contains(humanHeat), "Existing Heatstroke preserved on Human");

                // Null safety check
                try
                {
                    existingPrefix.Invoke(null, new object[] { null });
                    Assert(true, "DroidExistingTemperatureConditionsPatch handles null tracker safely");
                }
                catch (Exception ex)
                {
                    Assert(false, $"DroidExistingTemperatureConditionsPatch threw on null tracker: {ex}");
                }
            }

            // 4. Harmony target resolution and patch/unpatch check
            string harmonyId = "org.androids.droidtemperaturefix.test";
            var harmony = new Harmony(harmonyId);
            try
            {
                // Verify targets can be resolved
                MethodInfo adjustSeverityTarget = typeof(HealthUtility).GetMethod(nameof(HealthUtility.AdjustSeverity));
                MethodInfo healthTickIntervalTarget = typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.HealthTickInterval));

                Assert(adjustSeverityTarget != null, "Harmony target HealthUtility.AdjustSeverity resolved");
                Assert(healthTickIntervalTarget != null, "Harmony target Pawn_HealthTracker.HealthTickInterval resolved");

                // Apply patches
                if (adjustSeverityTarget != null && severityPrefix != null)
                {
                    harmony.Patch(adjustSeverityTarget, prefix: new HarmonyMethod(severityPrefix));
                    var patches = Harmony.GetPatchInfo(adjustSeverityTarget);
                    bool hasPrefix = patches != null && patches.Prefixes.Any(p => p.owner == harmonyId);
                    Assert(hasPrefix, "Harmony successfully applied DroidTemperatureSeverityPatch to AdjustSeverity");
                }

                if (healthTickIntervalTarget != null && existingPrefix != null)
                {
                    harmony.Patch(healthTickIntervalTarget, prefix: new HarmonyMethod(existingPrefix));
                    var patches = Harmony.GetPatchInfo(healthTickIntervalTarget);
                    bool hasPrefix = patches != null && patches.Prefixes.Any(p => p.owner == harmonyId);
                    Assert(hasPrefix, "Harmony successfully applied DroidExistingTemperatureConditionsPatch to HealthTickInterval");
                }
            }
            catch (Exception ex)
            {
                Assert(false, $"Harmony patching encountered an exception: {ex.Message}");
            }
            finally
            {
                // Cleanup Harmony patches
                try
                {
                    harmony.UnpatchAll(harmonyId);
                    Assert(true, "Harmony patches cleaned up successfully (UnpatchAll)");
                }
                catch (Exception ex)
                {
                    Assert(false, $"Harmony unpatch failed: {ex.Message}");
                }
            }

            Console.WriteLine($"\nTest summary: {passed} passed, {failed} failed.");
            if (failed > 0)
            {
                Environment.Exit(1);
            }
        }
    }
}
