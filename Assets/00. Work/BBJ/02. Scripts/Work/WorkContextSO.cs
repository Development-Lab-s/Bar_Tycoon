using BBJ.Register;
using BBJ.WorkplaceSystem;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "WorkContext", menuName = "Tycoon/Work/WorkContext")]
    public class WorkContextSO : ScriptableObject
    {
        [Header("Registers")]
        public WorkplaceRegisterSO WorkplaceRegister;
        public OrderRegisterSO     OrderRegister;

        [Header("Workplace Types")]
        public WorkplaceTypeSO SeatType;
        public WorkplaceTypeSO KitchenType;
        public WorkplaceTypeSO CounterType;
        public WorkplaceTypeSO ExitType;
        public WorkplaceTypeSO ServeStationType;

        [Header("Channels")]
        public EventChannelSO OrderChannel;
        public EventChannelSO CustomerChannel;
    }
}
