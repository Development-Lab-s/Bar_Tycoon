
using BBJ.Register;
using Gamelib.EventSystem;
using UnityEngine;
using BBJ.GridSystem.Pathfind;

namespace BBJ.Customer
{
    [CreateAssetMenu(fileName = "CustomerContext", menuName = "Tycoon/SO/CustomerContext")]
    public class CustomerContextSO : ScriptableObject
    {
        public WorkplaceRegisterSO WorkplaceRegister;
        public PathRequestSO       PathRequest;
        public EventChannelSO      OrderChannel;
        public EventChannelSO      CustomerChannel;
    }
}
