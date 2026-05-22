using System;
using TMPro;
using UnityEngine;

namespace BBJ.Particle
{
    [Serializable]
    public class CostTypeConfig
    {
        public TMP_SpriteAsset spriteAsset;
        public int spriteIndex;
        public Color gainColor = Color.green;
        public Color spendColor = Color.red;
    }
}
