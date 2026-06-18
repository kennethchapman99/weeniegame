using System;

namespace CheddarAndCocoa.Game
{
    public static class MissionSeedGenerator
    {
        public static int StableSeed(string missionId, int sessionIndex, int variantIndex = 0)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (missionId == null ? 0 : StringComparer.Ordinal.GetHashCode(missionId));
                hash = hash * 31 + sessionIndex;
                hash = hash * 31 + variantIndex;
                return hash == int.MinValue ? 0 : Math.Abs(hash);
            }
        }

        public static int VariantIndex(int seed, int variantCount)
        {
            if (variantCount <= 1) return 0;
            return Math.Abs(seed) % variantCount;
        }
    }
}
