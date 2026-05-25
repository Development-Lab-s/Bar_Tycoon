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
        [SerializeField] private EventChannelSO cameraManagerEvent;
        
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
            cameraManagerEvent.RaiseEvent(new CameraManagerEvent().Init(spawnPositions, false));
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

        public override List<string> GetDescription()
        {
            List<string> strings = new();
            foreach (ObjectDataSO obsData in objectDataSOs)
            {
                if (obsData == null)
                {
                    Debug.Log("obsDataSO is null");
                    continue;
                }

                if (obsData.Description == null)
                {
                    Debug.Log("obsData.description is null");
                    continue;   
                }
                
                strings.Add(obsData.DisplayName);
            }
            
            return strings;
        }
    }
}