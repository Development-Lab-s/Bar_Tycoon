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
        public readonly List<Vector2> spawnPositions = new List<Vector2>();
        public readonly List<ObjectDataSO> objectDataSOs = new List<ObjectDataSO>();
        public override void LevelUpReward()
        {
            spawnPositions.Clear();
            objectDataSOs.Clear();
            ObjectSpawnEvent objSpawnEvt = new ObjectSpawnEvent();
            
            foreach (PlacedObstacleEntry obstacle in objectDataSO.ObjectsLayout)
            {
                objectDataSOs.Add(obstacle.obstacleData);
                eventChannelSo?.RaiseEvent(objSpawnEvt.Init(obstacle.obstacleData, obstacle.cellIndex, obstacle.flipX, (pos) => spawnPositions.Add(pos)));   
            }
        }

        public override List<Vector2> GetSpawnPositions()
        {
            return new List<Vector2>(spawnPositions);
        }

        public override List<Sprite> GetSprite()
        {
            List<Sprite> sprites = new();

            foreach (ObjectDataSO obsData in objectDataSOs)
            {
                if (obsData == null)
                {
                    Debug.Log("obsDataSO is null");
                    continue;
                }

                if (obsData.Icon == null)
                {
                    Debug.Log("obsData.Icon is null");
                    continue;   
                }

                sprites.Add(obsData.Icon);
            }
            
            return sprites;
        }
    }
}