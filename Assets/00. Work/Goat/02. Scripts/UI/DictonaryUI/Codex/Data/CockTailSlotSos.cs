using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data
{
    [CreateAssetMenu(fileName = "CockTailSlotSos", menuName = "SO/CockTailSlotSos", order = 0)]
    public class CockTailSlotSos : ScriptableObject
    {
        [field: SerializeField] public List<CockTailSlotSo> cockTailSlotList;
    }
}