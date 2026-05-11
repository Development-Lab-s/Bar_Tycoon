using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data
{
    [CreateAssetMenu(fileName = "cockTailSlotSo", menuName = "SO/cockTailSlotSo", order = 0)]
    public class CockTailSlotSo : ScriptableObject
    {
        [field: SerializeField] public int CockTailId { get; private set; }
        [field: SerializeField] public Sprite CockTailImage { get; private set; }
        [field: SerializeField] public string CockTailName { get; private set; }
        [field: SerializeField] public string CokcTailDescription { get; private set; }
        [field: Range(0,100)][field: SerializeField] public int SourNum { get; private set; }
        [field: Range(0,100)][field: SerializeField] public int SugarNum { get; private set; }
        [field: Range(0,100)][field: SerializeField] public int BitterNum { get; private set; }

        public void ChangeId(int id)
        {
            CockTailId = id;
        }
    }
}