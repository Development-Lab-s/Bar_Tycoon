using _00._Work.Lusaload._02._Scripts.SO;
using Agents.StatSystem;
using UnityEngine;
using BBJ.Order;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CookWorkDuration", menuName = "Tycoon/WorkDuration/Cook")]
    public class CookWorkDurationSO : WorkDurationSO
    {
        //[SerializeField] private StatSO _cookStat;
        [SerializeField] private WorkDurationSO cookStatDuration;
        [SerializeField] private float _minDuration = 0.5f;
        [SerializeField] private float _timePerStage = 3f;       // 음식 stage따른 시간 배율

        public override float GetDuration(IStatModule workerStat, OrderTicket conductData)
        {
            float baseDuration = GetBaseCookTime(conductData.Ordered);
            float statValue    = cookStatDuration.GetDuration(workerStat, conductData);

            return Mathf.Max(_minDuration, baseDuration * statValue);
        }

        private float GetBaseCookTime(CocktailRecipeSO ordered)
        {
            if (ordered == null) return _minDuration;

            int stage = Mathf.Max(1, ordered.unlockStage);
            return _timePerStage * stage;
        }
    }
}
