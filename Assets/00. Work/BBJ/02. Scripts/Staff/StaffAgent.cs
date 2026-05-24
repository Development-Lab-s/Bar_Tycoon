using _00._Work._Resources._02._Scripts.Agents.FSM;
using Agents.StatSystem;
using BBJ.Movement;
using BBJ.States;
using System;
using UnityEngine;

namespace BBJ
{
    public class StaffAgent : AbstractTycoonAgent
    {
        [SerializeField] private StatSO _speedStat;

        public IStatModule Stat { get; private set; }
        public IPathMovement Movement { get; private set; }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();

            Stat     = GetModule<IStatModule>();
            Movement = GetModule<IPathMovement>();

            UtilDebugger.LogNullMembers(this);
        }

        protected override void AfterInitComponents()
        {
            base.AfterInitComponents();

            float speed = Stat.SubscribeStat(_speedStat.AssetIndex, HandleSpeedStatChanged, 1f);
            Movement.OnSpeedChange(speed);
            ChangeState(StaffState.Idle);
        }

        private void OnDestroy()
        {
            Stat.UnSubscribeStat(_speedStat.AssetIndex, HandleSpeedStatChanged);
        }
        private void HandleSpeedStatChanged(StatSO stat, float current, float _) => Movement?.OnSpeedChange(current);
    }

}
