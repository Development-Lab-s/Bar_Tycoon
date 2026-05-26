using BBJ.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Modules
{
    public class AgentUIModule : MonoBehaviour, IModule, IAgentUIModule
    {
        private Dictionary<Type, IAgentUI> _uis;
        private CancellationTokenSource    _sequenceCts;

        public void Initialize(ModuleOwner owner)
        {
            _uis = GetComponentsInChildren<IAgentUI>(true)
                .ToDictionary(ui => ui.GetType());
        }

        public T Get<T>() where T : class, IAgentUI
        {
            _uis.TryGetValue(typeof(T), out var ui);
            return ui as T;
        }

        public async UniTask PlaySequenceAsync(CancellationToken ct, params IAgentUI[] sequence)
        {
            CancelSequence();
            foreach (var ui in _uis.Values)
                _ = ui.CloseAsync();
            _sequenceCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = _sequenceCts.Token;

            for (int i = 0; i < sequence.Length; i++)
            {
                if (token.IsCancellationRequested) return;

                bool cancelled = await sequence[i].OpenAsync()
                    .AttachExternalCancellation(token)
                    .SuppressCancellationThrow();
                if (cancelled) return;

                bool isLast = i == sequence.Length - 1;
                if (!isLast)
                {
                    cancelled = await sequence[i].CloseAsync()
                        .AttachExternalCancellation(token)
                        .SuppressCancellationThrow();
                    if (cancelled) return;
                }
            }
        }

        public void CancelSequence()
        {
            _sequenceCts?.Cancel();
            _sequenceCts?.Dispose();
            _sequenceCts = null;
        }

        public void CloseAll()
        {
            CancelSequence();
            foreach (var ui in _uis.Values)
                _ = ui.CloseAsync();
        }
    }
}
