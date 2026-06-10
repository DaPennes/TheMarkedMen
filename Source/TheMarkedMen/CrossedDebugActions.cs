using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    public static class CrossedDebugActions
    {
        private const string DebugCategory = "The Marked Men";

        [DebugAction(DebugCategory, "Start scheduled raid now", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 1000)]
        public static void StartScheduledRaidNow()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Report(Component?.DebugFireRaidNow() ?? false, "DevMode: Started a Marked Men raid now.", "DevMode: Could not start Marked Men raid. Load a player home map with free colonists.");
        }

        [DebugAction(DebugCategory, "Move next raid to 1 hour", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 990)]
        public static void MoveNextRaidToOneHour()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Report(Component?.DebugScheduleRaidSoon() ?? false, "DevMode: Next Marked Men raid will start in one in-game hour.", "DevMode: Could not move raid timer. Load a player home map with free colonists.");
        }

        [DebugAction(DebugCategory, "Start scouting pack event now", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 980)]
        public static void StartScoutingPackNow()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Report(Component?.DebugFireProbeNow() ?? false, "DevMode: Started a Marked Men scouting pack event now.", "DevMode: Could not start scouting pack. Load a player home map with free colonists.");
        }

        [DebugAction(DebugCategory, "Start horde event now", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 970)]
        public static void StartHordeNow()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Report(Component?.DebugFireHordeNow() ?? false, "DevMode: Started a Marked Men horde event now.", "DevMode: Could not start horde. Load a player home map with free colonists.");
        }

        [DebugAction(DebugCategory, "Run full feature diagnostics", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 800)]
        public static void RunFullFeatureDiagnostics()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Messages.Message("DevMode: The Marked Men debug actions are disabled in mod settings.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Log.Message("===== [The Marked Men] Starting full feature diagnostics =====");
            int passed = 0;
            int failed = 0;
            int warnings = 0;

            Diagnostics("Def references", delegate
            {
                Diagnostics("Faction: " + DefOk(CADefOf.CrossedFaction), ref passed, ref failed);
                Diagnostics("PawnKind Berserker: " + DefOk(CADefOf.Berserker), ref passed, ref failed);
                Diagnostics("PawnKind Hunter: " + DefOk(CADefOf.Hunter), ref passed, ref failed);
                Diagnostics("PawnKind Brute: " + DefOk(CADefOf.Brute), ref passed, ref failed);
                Diagnostics("PawnKind Stalker: " + DefOk(CADefOf.Stalker), ref passed, ref failed);
                Diagnostics("PawnKind Screamer: " + DefOk(CADefOf.Screamer), ref passed, ref failed);
                Diagnostics("PawnKind Alpha: " + DefOk(CADefOf.Alpha), ref passed, ref failed);
                Diagnostics("PawnKind AlphaPsychic: " + DefOk(CADefOf.AlphaPsychic), ref passed, ref failed);
                Diagnostics("PawnKind Charger: " + DefOk(CADefOf.Charger), ref passed, ref failed);
                Diagnostics("PawnKind Spitter: " + DefOk(CADefOf.Spitter), ref passed, ref failed);
                Diagnostics("PawnKind Bomber: " + DefOk(CADefOf.Bomber), ref passed, ref failed);
                Diagnostics("PawnKind Child: " + DefOk(CADefOf.Child), ref passed, ref failed);
                Diagnostics("Hediff CrossVirus: " + DefOk(CADefOf.CrossVirus), ref passed, ref failed);
                Diagnostics("Hediff CrossVirusImmunity: " + DefOk(CADefOf.CrossVirusImmunity), ref passed, ref failed);
                Diagnostics("Hediff CrossedRash: " + DefOk(CADefOf.CrossedRash), ref passed, ref failed);
                Diagnostics("Hediff BloodRush: " + DefOk(CADefOf.BloodRush), ref passed, ref failed);
                Diagnostics("Hediff CommandAura: " + DefOk(CADefOf.CommandAura), ref passed, ref failed);
                Diagnostics("Hediff PsychicAura: " + DefOk(CADefOf.PsychicAura), ref passed, ref failed);
                Diagnostics("Hediff Panic: " + DefOk(CADefOf.Panic), ref passed, ref failed);
                Diagnostics("Hediff BomberCharge: " + DefOk(CADefOf.BomberCharge), ref passed, ref failed);
                Diagnostics("Hediff SpitterGlands: " + DefOk(CADefOf.SpitterGlands), ref passed, ref failed);
                Diagnostics("Incident Raid: " + DefOk(CADefOf.CrossedRaid), ref passed, ref failed);
                Diagnostics("Incident Horde: " + DefOk(CADefOf.CrossedHorde), ref passed, ref failed);
                Diagnostics("Incident Probe: " + DefOk(CADefOf.CrossedProbe), ref passed, ref failed);
                Diagnostics("Incident DownedSurvivor: " + DefOk(CADefOf.CrossedDownedSurvivor), ref passed, ref failed);
                Diagnostics("Stat MarkedVirusResistance: " + DefOk(CADefOf.MarkedVirusResistance), ref passed, ref failed);
                Diagnostics("Thought SocialTerror: " + DefOk(CADefOf.CrossedSocialTerror), ref passed, ref failed);
                Diagnostics("Tattoo face: " + DefOk(CADefOf.CrossedFaceTattoo), ref passed, ref failed);
            });

            Diagnostics("Harmony patches", delegate
            {
                MethodInfo baseKill = AccessTools.Method(typeof(Pawn), "Kill");
                MethodInfo basePostApply = AccessTools.Method(typeof(Pawn), "PostApplyDamage");
                MethodInfo baseGenerateTraits = AccessTools.Method(typeof(PawnGenerator), "GenerateTraits");
                MethodInfo baseGenerateNewPawn = AccessTools.Method(typeof(PawnGenerator), "GenerateNewPawnInternal");
                CheckPatch(baseKill, "Patch_InfectedDeathReanimation (Prefix)", ref passed, ref failed);
                CheckPatch(basePostApply, "Patch_BloodExposure (Postfix)", ref passed, ref failed);
                CheckPatch(baseGenerateTraits, "Patch_ForcedTraits (Postfix)", ref passed, ref failed);
                CheckPatch(baseGenerateNewPawn, "Patch_MarkKindTuning (Postfix)", ref passed, ref failed);
            });

            Diagnostics("Game state", delegate
            {
                TheMarkedMenGameComponent comp = Component;
                Diagnostics("GameComponent exists: " + (comp != null ? "OK" : "MISSING"), ref passed, ref failed);
                if (comp == null) return;
                Faction faction = Find.FactionManager?.FirstFactionOfDef(CADefOf.CrossedFaction);
                Diagnostics("Crossed faction exists: " + (faction != null ? "OK" : "MISSING"), ref passed, ref failed);
                Diagnostics("Raid timer active: " + (Find.TickManager != null ? "OK" : "NO TICK MANAGER"), ref passed, ref failed);
            });

            Diagnostics("Map diagnostics", delegate
            {
                if (Find.Maps == null || Find.Maps.Count == 0)
                {
                    Diagnostics("No maps loaded", ref warnings, ref failed);
                    return;
                }
                for (int i = 0; i < Find.Maps.Count; i++)
                {
                    Map map = Find.Maps[i];
                    if (map == null || map.mapPawns == null) continue;
                    int crossedCount = 0;
                    int virusCount = 0;
                    List<Pawn> allPawns = map.mapPawns.AllPawnsSpawned.ToList();
                    for (int j = 0; j < allPawns.Count; j++)
                    {
                        Pawn p = allPawns[j];
                        if (CrossedUtility.IsInfectedPawn(p)) crossedCount++;
                        if (CrossedUtility.HasMarkedVirusHediff(p)) virusCount++;
                    }
                    Diagnostics("Map " + map.Index + " \"" + map + "\": " + allPawns.Count + " pawns, " + crossedCount + " Marked, " + virusCount + " infected", ref passed, ref warnings);
                }
            });

            Diagnostics("Class hediff verification", delegate
            {
                if (Find.Maps == null) return;
                for (int i = 0; i < Find.Maps.Count; i++)
                {
                    Map map = Find.Maps[i];
                    if (map?.mapPawns == null) continue;
                    List<Pawn> pawns = map.mapPawns.AllPawnsSpawned.ToList();
                    for (int j = 0; j < pawns.Count; j++)
                    {
                        Pawn p = pawns[j];
                        if (p == null || !CrossedUtility.IsInfectedPawn(p)) continue;
                        string kind = p.kindDef?.defName ?? "null";
                        bool hasBerserker = p.kindDef == CADefOf.Berserker && p.health.hediffSet.HasHediff(CADefOf.BloodRush);
                        bool hasAlpha = p.kindDef == CADefOf.Alpha && p.health.hediffSet.HasHediff(CADefOf.CommandAura);
                        bool hasPsychic = p.kindDef == CADefOf.AlphaPsychic && p.health.hediffSet.HasHediff(CADefOf.PsychicAura);
                        bool hasSpitter = p.kindDef == CADefOf.Spitter && p.health.hediffSet.HasHediff(CADefOf.SpitterGlands);
                        bool hasBomber = p.kindDef == CADefOf.Bomber && p.health.hediffSet.HasHediff(CADefOf.BomberCharge);
                        if ((p.kindDef == CADefOf.Berserker && !hasBerserker) ||
                            (p.kindDef == CADefOf.Alpha && !hasAlpha) ||
                            (p.kindDef == CADefOf.AlphaPsychic && !hasPsychic) ||
                            (p.kindDef == CADefOf.Spitter && !hasSpitter) ||
                            (p.kindDef == CADefOf.Bomber && !hasBomber))
                            Diagnostics("MISSING class hediff on " + kind + " pawn " + p.Name, ref warnings, ref failed);
                    }
                }
            });

            Diagnostics("Settings", delegate
            {
                TheMarkedMenSettings s = TheMarkedMenMod.Settings;
                Diagnostics("Settings loaded: " + (s != null ? "OK" : "NULL"), ref passed, ref failed);
                if (s != null)
                {
                    Diagnostics("Infection enabled: " + s.infectionEnabled, ref passed, ref warnings);
                    Diagnostics("Warbands: " + TheMarkedMenSettings.WarbandsEnabled + ", Hordes: " + TheMarkedMenSettings.HordesEnabled + ", Probes: " + TheMarkedMenSettings.ProbesEnabled, ref passed, ref warnings);
                }
            });

            Diagnostics("Compatibility", delegate
            {
                Diagnostics("RJW loaded: " + TheMarkedMenRjwCompatibility.IsRjwLoaded(), ref passed, ref warnings);
                Diagnostics("VPE active: " + VPECompat.IsActive, ref passed, ref warnings);
            });

            string summary = string.Format("===== [The Marked Men] Diagnostics complete. {0} passed, {1} warnings, {2} failed =====", passed, warnings, failed);
            Log.Message(summary);
            if (failed > 0)
                Log.Error("[The Marked Men] Diagnostics detected " + failed + " failures — review log above.");
            Messages.Message(summary, failed > 0 ? MessageTypeDefOf.ThreatBig : MessageTypeDefOf.NeutralEvent, false);
        }

        private static void Diagnostics(string text, ref int passCounter, ref int failCounter)
        {
            Log.Message("[The Marked Men] " + text);
            if (text.Contains("FAIL") || text.Contains("MISSING") || text.Contains("ERROR"))
                failCounter++;
            else if (text.Contains("WARN"))
                passCounter++;
            else
                passCounter++;
        }

        private static void Diagnostics(string label, System.Action action)
        {
            Log.Message("[The Marked Men] --- " + label + " ---");
            try
            {
                action();
            }
            catch (System.Exception ex)
            {
                Log.Error("[The Marked Men] " + label + " threw: " + ex);
            }
        }

        private static string DefOk(object def)
        {
            return def != null ? "OK" : "MISSING";
        }

        private static void CheckPatch(MethodInfo original, string patchName, ref int passed, ref int failed)
        {
            bool patched = Harmony.GetPatchInfo(original) != null;
            Log.Message("[The Marked Men]   Patch " + patchName + ": " + (patched ? "applied" : "NOT FOUND"));
            if (patched) passed++; else failed++;
        }

        private static TheMarkedMenGameComponent Component => Current.Game?.GetComponent<TheMarkedMenGameComponent>();

        private static void Report(bool success, string successText, string failureText)
        {
            Messages.Message(success ? successText : failureText, success ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput, false);
        }
    }
}
