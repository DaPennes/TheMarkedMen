using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    public static class MarkedIdeologyUtility
    {
        private const string LogPrefix = "[The Marked Men] ";
        private const string FixedIconDefName = "Skull";
        private const string FixedColorDefName = "Red";
        private const string FallbackStyleCategoryDefName = "Morbid";

        private static readonly string[] FallbackMemeDefNames =
        {
            "Structure_Ideological",
            "Cannibal",
            "Supremacist",
            "Raider"
        };

        public static void NormalizeMarkedOneIdeology()
        {
            if (!ModsConfig.IdeologyActive)
            {
                return;
            }

            try
            {
                FactionDef factionDef = CADefOf.CrossedFaction;
                Faction faction = factionDef == null ? null : Find.FactionManager?.FirstFactionOfDef(factionDef);
                Ideo ideo = faction?.ideos?.PrimaryIdeo;
                if (factionDef == null || ideo == null)
                {
                    return;
                }

                bool changed = ApplyFixedText(ideo, factionDef);
                changed |= ApplyFixedVisibility(ideo, factionDef);
                changed |= ApplyFixedMemes(ideo, factionDef);
                changed |= ApplyFixedIconAndColor(ideo);
                changed |= ApplyFixedStyles(ideo, factionDef);

                if (changed)
                {
                    RecacheIdeo(ideo);
                    Log.Message(LogPrefix + "Normalized The Marked One ideology.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(LogPrefix + "Skipped The Marked One ideology normalization: " + ex.Message);
            }
        }

        private static bool ApplyFixedText(Ideo ideo, FactionDef factionDef)
        {
            bool changed = false;

            if (!factionDef.ideoName.NullOrEmpty() && ideo.name != factionDef.ideoName)
            {
                ideo.name = factionDef.ideoName;
                changed = true;
            }

            if (!factionDef.ideoDescription.NullOrEmpty() && ideo.description != factionDef.ideoDescription)
            {
                ideo.description = factionDef.ideoDescription;
                changed = true;
            }

            return changed;
        }

        private static bool ApplyFixedVisibility(Ideo ideo, FactionDef factionDef)
        {
            if (ideo.hidden == factionDef.hiddenIdeo)
            {
                return false;
            }

            ideo.hidden = factionDef.hiddenIdeo;
            return true;
        }

        private static bool ApplyFixedMemes(Ideo ideo, FactionDef factionDef)
        {
            if (ideo.memes == null)
            {
                return false;
            }

            List<MemeDef> targetMemes = BuildTargetMemes(factionDef);
            if (targetMemes.Count == 0)
            {
                return false;
            }

            bool alreadyExact = ideo.memes.Count == targetMemes.Count;
            if (alreadyExact)
            {
                for (int i = 0; i < targetMemes.Count; i++)
                {
                    if (!ideo.memes.Contains(targetMemes[i]))
                    {
                        alreadyExact = false;
                        break;
                    }
                }
            }

            if (alreadyExact)
            {
                return false;
            }

            ideo.memes.Clear();
            ideo.memes.AddRange(targetMemes);
            ideo.SortMemesInDisplayOrder();
            return true;
        }

        private static bool ApplyFixedIconAndColor(Ideo ideo)
        {
            IdeoIconDef iconDef = DefDatabase<IdeoIconDef>.GetNamedSilentFail(FixedIconDefName);
            ColorDef colorDef = DefDatabase<ColorDef>.GetNamedSilentFail(FixedColorDefName);
            if (iconDef == null || colorDef == null || ideo.iconDef == iconDef && ideo.colorDef == colorDef && ideo.primaryFactionColor == null)
            {
                return false;
            }

            ideo.SetIcon(iconDef, colorDef, true);
            return true;
        }

        private static bool ApplyFixedStyles(Ideo ideo, FactionDef factionDef)
        {
            List<StyleCategoryDef> targetStyles = BuildTargetStyles(factionDef);
            if (targetStyles.Count == 0)
            {
                return false;
            }

            if (ideo.thingStyleCategories == null)
            {
                ideo.thingStyleCategories = new List<ThingStyleCategoryWithPriority>();
            }

            bool alreadyExact = ideo.thingStyleCategories.Count == targetStyles.Count;
            if (alreadyExact)
            {
                for (int i = 0; i < targetStyles.Count; i++)
                {
                    ThingStyleCategoryWithPriority current = ideo.thingStyleCategories[i];
                    if (current == null || current.category != targetStyles[i])
                    {
                        alreadyExact = false;
                        break;
                    }
                }
            }

            if (alreadyExact)
            {
                return false;
            }

            ideo.thingStyleCategories.Clear();
            for (int i = 0; i < targetStyles.Count; i++)
            {
                ideo.thingStyleCategories.Add(new ThingStyleCategoryWithPriority(targetStyles[i], 1f));
            }

            ideo.SortStyleCategories();
            ideo.style?.RecalculateAvailableStyleItems();
            ideo.style?.EnsureAtLeastOneStyleItemAvailable();
            return true;
        }

        private static List<MemeDef> BuildTargetMemes(FactionDef factionDef)
        {
            List<MemeDef> targetMemes = new List<MemeDef>();
            AddUniqueMemes(targetMemes, factionDef.forcedMemes);
            if (targetMemes.Count == 0)
            {
                for (int i = 0; i < FallbackMemeDefNames.Length; i++)
                {
                    MemeDef meme = DefDatabase<MemeDef>.GetNamedSilentFail(FallbackMemeDefNames[i]);
                    if (meme != null && !targetMemes.Contains(meme))
                    {
                        targetMemes.Add(meme);
                    }
                }
            }

            return targetMemes;
        }

        private static List<StyleCategoryDef> BuildTargetStyles(FactionDef factionDef)
        {
            List<StyleCategoryDef> targetStyles = new List<StyleCategoryDef>();
            AddUniqueStyles(targetStyles, factionDef.styles);
            if (targetStyles.Count == 0)
            {
                StyleCategoryDef style = DefDatabase<StyleCategoryDef>.GetNamedSilentFail(FallbackStyleCategoryDefName);
                if (style != null)
                {
                    targetStyles.Add(style);
                }
            }

            return targetStyles;
        }

        private static void AddUniqueMemes(List<MemeDef> targetMemes, List<MemeDef> sourceMemes)
        {
            if (sourceMemes == null)
            {
                return;
            }

            for (int i = 0; i < sourceMemes.Count; i++)
            {
                MemeDef meme = sourceMemes[i];
                if (meme != null && !targetMemes.Contains(meme))
                {
                    targetMemes.Add(meme);
                }
            }
        }

        private static void AddUniqueStyles(List<StyleCategoryDef> targetStyles, List<StyleCategoryDef> sourceStyles)
        {
            if (sourceStyles == null)
            {
                return;
            }

            for (int i = 0; i < sourceStyles.Count; i++)
            {
                StyleCategoryDef style = sourceStyles[i];
                if (style != null && !targetStyles.Contains(style))
                {
                    targetStyles.Add(style);
                }
            }
        }

        private static void RecacheIdeo(Ideo ideo)
        {
            ideo.RecachePrecepts();
            ideo.RecachePossibleRoles();
            ideo.RecachePossibleBuildings();
            ideo.RecachePossibleBuildables();
            ideo.RecachePossibleMentalBreaks();
            ideo.RecacheNeeds();
        }
    }
}
