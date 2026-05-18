
using BBJ.Register;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Customer
{
    [CreateAssetMenu(fileName = "CustomerContext", menuName = "Tycoon/SO/CustomerContext")]
    public class CustomerContextSO : ScriptableObject
    {
        public WorkplaceRegisterSO WorkplaceRegister;
        public EventChannelSO      OrderChannel;
        public EventChannelSO      CustomerChannel;
    }
}
