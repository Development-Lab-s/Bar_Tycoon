using BBJ.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using System.Linq;

namespace BBJ.Modules
{
    public class AgentUIModule : MonoBehaviour, IModule, IAgentUIModule
    {
        private Dictionary<Type, IAgentUI> _uis;

        public void Initialize(ModuleOwner owner)
        {
            _uis = GetComponentsInChildren<IAgentUI>()
                .ToDictionary(ui => ui.GetType());
        }

        public T Get<T>() where T : class, IAgentUI
        {
            _uis.TryGetValue(typeof(T), out var ui);
            return ui as T;
        }
        public void SetActiveUI<T>(bool isActive) where T : class, IAgentUI
        {
            T ui = Get<T>();
            if (isActive)
            {
                ui.Open();
                return;
            }
            ui.Close();
        }

        public void CloseAll()
        {
            foreach (var ui in _uis.Values)
                ui.Close();
        }
    }
}
