using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public sealed class StatWorker_MarkedVirusResistance : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            return PawnFor(req) != null || ApparelDefFor(req) != null;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            Pawn pawn = PawnFor(req);
            if (pawn != null)
            {
                return CrossedUtility.GetMarkedVirusApparelResistance(pawn);
            }

            ThingDef apparelDef = ApparelDefFor(req);
            return apparelDef == null ? 0f : CrossedUtility.GetMarkedVirusApparelProtection(apparelDef).resistance;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            Pawn pawn = PawnFor(req);
            if (pawn != null)
            {
                MarkedVirusApparelProtection pawnProtection = CrossedUtility.GetMarkedVirusExposureProtection(pawn);
                string pawnPercent = Mathf.RoundToInt(Mathf.Clamp01(pawnProtection.resistance) * 100f).ToString() + "%";
                if (pawnProtection.blocksMarkedVirusExposure)
                {
                    return "Marked Virus resistance: " + pawnPercent + "\n\nFully sealed protective gear blocks direct Marked Virus exposure. This is separate from toxic environment resistance.";
                }

                return pawnProtection.sealedAgainstMarkedVirus
                    ? "Marked Virus resistance: " + pawnPercent + "\n\nBest worn Marked Virus protection is sealed protective gear. The Marked Virus uses this value separately from toxic environment resistance."
                    : "Marked Virus resistance: " + pawnPercent + "\n\nBest worn Marked Virus protection reduces exposure risk. The Marked Virus uses this value separately from toxic environment resistance.";
            }

            ThingDef apparelDef = ApparelDefFor(req);
            if (apparelDef == null)
            {
                return string.Empty;
            }

            MarkedVirusApparelProtection protection = CrossedUtility.GetMarkedVirusApparelProtection(apparelDef);
            string percent = Mathf.RoundToInt(Mathf.Clamp01(protection.resistance) * 100f).ToString() + "%";
            if (protection.blocksMarkedVirusExposure)
            {
                return "Marked Virus resistance: " + percent + "\n\nThis apparel is fully sealed against direct Marked Virus exposure.";
            }

            return protection.sealedAgainstMarkedVirus
                ? "Marked Virus resistance: " + percent + "\n\nThis apparel resists Marked Virus exposure and treats breakthrough infections as sealed-protection breakthroughs."
                : "Marked Virus resistance: " + percent + "\n\nThis apparel resists Marked Virus exposure but is not sealed protective gear.";
        }

        private static ThingDef ApparelDefFor(StatRequest req)
        {
            Thing thing = req.Thing;
            if (thing?.def?.apparel != null)
            {
                return thing.def;
            }

            if (req.Def is ThingDef thingDef && thingDef.apparel != null)
            {
                return thingDef;
            }

            return null;
        }

        private static Pawn PawnFor(StatRequest req)
        {
            return req.Thing as Pawn;
        }
    }
}
