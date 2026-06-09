using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public struct MarkedVirusApparelProtection
    {
        public float resistance;
        public bool sealedAgainstMarkedVirus;
        public bool blocksMarkedVirusExposure;

        public MarkedVirusApparelProtection(float resistance, bool sealedAgainstMarkedVirus, bool blocksMarkedVirusExposure = false)
        {
            this.blocksMarkedVirusExposure = blocksMarkedVirusExposure;
            this.resistance = blocksMarkedVirusExposure ? 1f : Mathf.Clamp01(resistance);
            this.sealedAgainstMarkedVirus = (sealedAgainstMarkedVirus || blocksMarkedVirusExposure) && this.resistance > 0f;
        }
    }
}
