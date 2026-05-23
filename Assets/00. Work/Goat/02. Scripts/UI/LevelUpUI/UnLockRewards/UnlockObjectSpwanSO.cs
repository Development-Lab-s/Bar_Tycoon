using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using BBJ.GridSystem.Objects;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI.UnLockRewards
{
    [CreateAssetMenu(fileName = "unlockObjectSpawnSO", menuName = "SO/unlock/ObjectSpawn", order = 0)]
    public class UnlockObjectSpwanSO : AbstractUnlockSO
    {
        [SerializeField] private  ObjectsBatchSO objectDataSO;
        [SerializeField] private EventChannelSO cameraMoveEvent;
        public override void LevelUpReward()
        {
            ObjectSpawnEvent objSpawnEvt = new ObjectSpawnEvent();
            List<Vector2> spawnPosition = new List<Vector2>();
            foreach (PlacedObstacleEntry obstacle in objectDataSO.ObjectsLayout)
            {
                eventChannelSo?.RaiseEvent(objSpawnEvt.Init(obstacle.obstacleData, obstacle.cellIndex, obstacle.flipX, (pos) => spawnPosition.Add(pos)));   
            }
            cameraMoveEvent.RaiseEvent(new CameraManagerEvent().Init(spawnPosition));
        }
    }
}