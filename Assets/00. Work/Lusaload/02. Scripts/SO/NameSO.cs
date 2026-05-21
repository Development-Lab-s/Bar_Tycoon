using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    [CreateAssetMenu(fileName = "NameSO", menuName = "SO/NameSO", order = 0)]
    public class NameSO : ScriptableObject
    {
        public string playerName;

        public void SetName(string name)
        {
            playerName = name;
        }
    }
}