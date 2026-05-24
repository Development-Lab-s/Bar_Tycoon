using BBJ.GridSystem.Objects;
using BBJ.Staff;
using Coffee.UIEffects;
using Gamelib.EventSystem;
using System;
using UnityEngine;

namespace BBJ.EventSystem
{
    public class StaffSpawnEvent : GameEvent {
        public event Action<Vector3> CallBack; 
        public StaffConfigSO Config;

        public void OnSpawnEnded(Vector3 pos) => CallBack?.Invoke(pos);

        public StaffSpawnEvent Init(StaffConfigSO data, Action<Vector3> callback = default)
        {
            Config = data;
            CallBack = callback;
            return this;
        }
    }
}
