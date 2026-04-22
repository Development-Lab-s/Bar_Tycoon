using BBJ.Register;
using UnityEngine;

namespace BBJ.Tycoon.Board
{
    [CreateAssetMenu(fileName = "CustomerContext", menuName = "Tycoon/SO/CustomerContext")]
    public class CustomerContextSO : ScriptableObject
    {
        public CustomerBoardSO     CustomerBoard;
        public OrderBoardSO        OrderBoard;
        public WorkplaceRegisterSO WorkplaceRegister;
        public PathRequestSO       PathRequest;
        public CounterBoardSO      CounterBoard;
    }
}
