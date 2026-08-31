using System;

namespace Help.Item
{
    [Serializable]
    public struct MaterialRequirement
    {
        public AlphabetMaterial Material;
        public int Count;

        public MaterialRequirement(AlphabetMaterial material, int count)
        {
            Material = material;
            Count = count;
        }
    }
}
