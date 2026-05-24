using BBJ.Save;
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
            public Vector3       DefaultSpawnPosition;
        }

        [SerializeField] private List<StaffEntry> _entries = new();

        private readonly List<StaffAgent> _spawnedAgents = new();

        public void SpawnAll()
        {
            foreach (var entry in _entries)
                SpawnNormal(entry);
        }

        public void RestoreStaff(StaffSaveData data)
        {
            foreach (var save in data.Members)
            {
                var entry = _entries.Find(e => e.Config != null && e.Config.Role == save.Role);
                if (entry.Config == null) continue;
                SpawnAtPosition(entry, save.LastPosition);
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

        private void SpawnNormal(StaffEntry entry)
        {
            if (entry.Config?.Prefab == null) return;

            var go    = Instantiate(entry.Config.Prefab, entry.DefaultSpawnPosition, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            var agent = go.GetComponent<StaffAgent>();
            if (agent == null) return;

            _spawnedAgents.Add(agent);
        }

        private void SpawnAtPosition(StaffEntry entry, Vector3 position)
        {
            if (entry.Config?.Prefab == null) return;

            var go    = Instantiate(entry.Config.Prefab, position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            var agent = go.GetComponent<StaffAgent>();
            if (agent == null) return;

            _spawnedAgents.Add(agent);
        }
    }
}
