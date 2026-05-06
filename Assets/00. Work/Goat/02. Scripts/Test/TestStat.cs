using Agents.StatSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Test
{
    public class TestStat : MonoBehaviour
    {
        [SerializeField] private StatSO _statSO;

        [ContextMenu(("statUp"))]
        public void StatUp()
        {
            _statSO.BaseValue += 10;
        }
    }
}