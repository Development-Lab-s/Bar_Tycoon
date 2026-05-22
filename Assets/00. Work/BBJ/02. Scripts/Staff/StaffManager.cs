using BBJ.Save;
using Gamelib.ObjectPool.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using BBJ.Schedule;
using BBJ.Register;
using BBJ.WorkplaceSystem;

namespace BBJ.Staff
{
    public class StaffManager : MonoBehaviour
    {
        [Serializable]
        public struct StaffEntry
        {
            public StaffConfigSO Config;
        }

        [SerializeField] private List<StaffEntry>      _entries = new();
        [SerializeField] private WorkplaceRegisterSO   _workplaceRegister;
        [SerializeField] private WorkplaceTypeSO       _entranceType;

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

            var entrances = _workplaceRegister?.GetAll(_entranceType);
            var spawnPos  = entrances != null && entrances.Count > 0
                ? entrances[0].transform.position
                : Vector3.zero;

            var go    = Instantiate(entry.Config.Prefab, spawnPos, Quaternion.identity);
            var agent = go.GetComponent<StaffAgent>();
            if (agent == null) return;

            _spawnedAgents.Add(agent);
        }

        private void SpawnAtPosition(StaffEntry entry, Vector3 position)
        {
            if (entry.Config?.Prefab == null) return;

            var go    = Instantiate(entry.Config.Prefab, position, Quaternion.identity);
            var agent = go.GetComponent<StaffAgent>();
            if (agent == null) return;

            _spawnedAgents.Add(agent);
        }
    }
}
