using System;
using UnityEngine;

namespace BBJ.Particle
{
    [Serializable]
    public class CostTypeConfig
    {
        public Sprite icon;
        public int spriteIndex;
        public Color gainColor = Color.green;
        public Color spendColor = Color.red;
    }
}
