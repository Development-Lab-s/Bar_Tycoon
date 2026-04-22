using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Register
{
    /// <summary>
    /// 런타임 등록소 베이스.
    ///
    /// 변경: 중복 체크를 List.Contains() O(n) → HashSet O(1)로 교체.
    ///   외부 순회용 IReadOnlyList는 유지 (List를 내부에서 병행 관리).
    /// </summary>
    public abstract class RegisterSO<T> : ScriptableObject
    {
        private readonly List<T>    _agentList = new();
        private readonly HashSet<T> _agentSet  = new();

        public IReadOnlyList<T> Agents => _agentList;

        public virtual void Register(T agent)
        {
            if (_agentSet.Add(agent))  // HashSet.Add: 이미 있으면 false 반환, O(1)
                _agentList.Add(agent);
        }

        public void Unregister(T agent)
        {
            if (_agentSet.Remove(agent))
                _agentList.Remove(agent);
        }

        public void Clear()
        {
            _agentList.Clear();
            _agentSet.Clear();
        }

        private void OnDisable() => Clear();
    }
}
