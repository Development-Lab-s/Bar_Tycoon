using BBJ.EventSystem;
using BBJ.Save;
using Gamelib.EventSystem;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BBJ.Schedule;

namespace BBJ.Staff
{
    public class StaffManager : MonoBehaviour
    {
        [Serializable]
        public struct StaffEntry
        {
            public StaffConfigSO Config;
        }

        [Serializable]
        public struct SpawnPoint
        {
            public AgentRole Role;
            public Vector3   Position;
        }

        [SerializeField] private List<StaffEntry>    _entries     = new();
        [SerializeField] private List<StaffConfigSO> _allConfigs  = new();
        [SerializeField] private List<SpawnPoint>  _spawnPoints = new();
        [SerializeField] private EventChannelSO    _staffChannel;

        private readonly List<StaffAgent> _spawnedAgents = new();

        private void Awake()     => _staffChannel?.AddListener<StaffSpawnEvent>(OnSpawnEvent);
        private void OnDestroy() => _staffChannel?.RemoveListener<StaffSpawnEvent>(OnSpawnEvent);

        private void OnSpawnEvent(StaffSpawnEvent e)
        {
            var agent = SpawnByConfig(e.Config);
            if (agent != null)
                e.OnSpawnEnded(agent.transform.position);
        }

        public void SpawnAll()
        {
            foreach (var entry in _entries)
                SpawnByConfig(entry.Config);
        }

        public void RestoreStaff(StaffSaveData data)
        {
            foreach (var save in data.Members)
            {
                var config = _allConfigs.Find(c => c != null && c.Role == save.Role);
                if (config == null) continue;
                SpawnAtPosition(config, save.LastPosition);
            }
        }

        public StaffSaveData GetSaveData()
        {
            var data = new StaffSaveData();
            foreach (var agent in _spawnedAgents)
            {
                if (agent == null) continue;
                var scheduling = agent.GetModule<SchedulingModule>();
                if (scheduling == null) continue;
                data.Members.Add(new StaffMemberSaveData
                {
                    Role         = scheduling.Role,
                    LastPosition = agent.transform.position,
                });
            }
            return data;
        }

        public void CancelAllWork()
        {
            foreach (var agent in _spawnedAgents)
                agent?.GetModule<SchedulingModule>()?.CancelWork();
        }

        private StaffAgent SpawnByConfig(StaffConfigSO config)
        {
            if (config?.Prefab == null) return null;

            var point = _spawnPoints.Find(p => p.Role == config.Role);
            return SpawnAtPosition(config, point.Position);
        }

        private StaffAgent SpawnAtPosition(StaffConfigSO config, Vector3 position)
        {
            if (config?.Prefab == null) return null;

            var go    = Instantiate(config.Prefab, position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            var agent = go.GetComponent<StaffAgent>();
            if (agent == null) return null;

            _spawnedAgents.Add(agent);
            return agent;
        }
    }
}
