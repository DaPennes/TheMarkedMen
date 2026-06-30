using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    public static class CrossedCompatibility
    {
        private static readonly string[] KnownExactPackages =
        {
            "ceteam.combatextended",
            "kentington.saveourship2",
            "dubwise.dubsbadhygiene",
            "dubwise.dubsperformanceanalyzer.steam",
            "orion.hospitality",
            "roolo.runandgun",
            "krkr.rocketman",
            "taranchuk.performancefish",
            "taranchuk.performanceoptimizer",
            "taranchuk.fastergameloading",
            "edria.performancesuperboosterultimate",
            "fluxxfield.defloadcache",
            "dev.soeur.imageopt",
            "rwmt.multiplayer",
            "rim.job.world",
            "zetrith.prepatcher",
            "daniledman.combatupdate",
            "oskarpotocki.vanillafactionsexpanded.core",
            "ferny.worldbuilder",
            "c0ffee.rimworld.animations",
            "smashphil.xmlpatchhelper",
            "v1024.visibleerrorlogs",
            "imranfish.patchoperationstacktraces",
            "mlie.logafterdeferror",
            "ludeon.rimworld.royalty",
            "ludeon.rimworld.ideology",
            "ludeon.rimworld.biotech",
            "ludeon.rimworld.anomaly",
            "ludeon.rimworld.odyssey"
        };

        private static readonly string[] KnownPackagePrefixes =
        {
            "vanillaexpanded."
        };

        public static void LogDetectedMods()
        {
            if (TheMarkedMenMod.Settings != null && !TheMarkedMenMod.Settings.verboseCompatibilityLogging)
            {
                return;
            }

            try
            {
                HashSet<string> activePackages = new HashSet<string>();
                foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
                {
                    string packageId = mod?.PackageIdPlayerFacing;
                    if (!string.IsNullOrEmpty(packageId))
                    {
                        activePackages.Add(packageId.ToLowerInvariant());
                    }
                }

                List<string> detected = new List<string>();
                for (int i = 0; i < KnownExactPackages.Length; i++)
                {
                    string packageId = KnownExactPackages[i];
                    if (activePackages.Contains(packageId))
                    {
                        detected.Add(packageId);
                    }
                }

                foreach (string activePackage in activePackages)
                {
                    for (int i = 0; i < KnownPackagePrefixes.Length; i++)
                    {
                        string prefix = KnownPackagePrefixes[i];
                        if (activePackage.StartsWith(prefix, StringComparison.Ordinal) && !detected.Contains(prefix + "*"))
                        {
                            detected.Add(prefix + "*");
                        }
                    }
                }

                Log.Message("[The Marked Men] Compatibility scan detected: " + (detected.Count == 0 ? "no tracked packages" : string.Join(", ", detected.ToArray())));
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Compatibility scan skipped: " + ex.Message);
            }
        }
    }
}
