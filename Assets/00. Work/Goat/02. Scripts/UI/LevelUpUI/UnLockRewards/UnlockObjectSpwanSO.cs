using BBJ.GridSystem.Objects;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI.UnLockRewards
{
    [CreateAssetMenu(fileName = "unlockObjectSpawnSO", menuName = "SO/unlock/ObjectSpawn", order = 0)]
    public class UnlockObjectSpwanSO : AbstractUnlockSO
    {
        [SerializeField] private  ObjectsBatchSO objectDataSO;
        public override void LevelUpReward()
        {
            foreach (PlacedObstacleEntry obstacle in objectDataSO.ObjectsLayout)
            {
                eventChannelSo?.RaiseEvent(new ObjectSpawnEvent().Init(obstacle.obstacleData, obstacle.cellIndex, obstacle.flipX ));   
            }
        }
    }
}