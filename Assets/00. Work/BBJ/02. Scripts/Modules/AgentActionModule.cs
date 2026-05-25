using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Actions;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



namespace BBJ.Modules
{
    public class AgentActionModule : MonoBehaviour, IModule, IAgentActionModule
    {
        private Dictionary<Type, IAgentAction> _actions = new Dictionary<Type, IAgentAction>();
        private Agent _agent;

        public void Initialize(ModuleOwner owner)
        {
            _agent = owner as Agent;
            _actions = GetComponentsInChildren<IAgentAction>()
                .ToDictionary((action) => action.GetType());
            foreach (IAgentAction action in _actions.Values)
                action.InitOwner(_agent);
        }

        public T GetAction<T>() where T : class, IAgentAction
        {
            _actions.TryGetValue(typeof(T), out var action);
            return action as T;
        }

        public UniTask Execute<T>(Func<T, UniTask> execute) where T : class, IAgentAction
        {
            var action = GetAction<T>();
            if (action == null) return UniTask.CompletedTask;
            return execute(action);
        }
    }
}
