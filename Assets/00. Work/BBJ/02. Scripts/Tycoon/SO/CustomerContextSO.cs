using BBJ.Register;
using UnityEngine;

namespace BBJ.Tycoon
{
    /// <summary>
    /// CustomerAgent가 필요로 하는 런타임 SO 참조를 하나로 묶은 컨텍스트.
    ///
    /// 기존: CustomerAgent가 SO 5개를 SerializeField로 직접 보유
    ///   → Prefab 직렬화 필드가 많아지고, 하나 변경 시 Prefab 재설정 필요
    ///
    /// 변경: SO 참조를 이 에셋 하나로 집중.
    ///   CustomerAgent는 CustomerContextSO 하나만 SerializeField로 보유한다.
    /// </summary>
    [CreateAssetMenu(fileName = "CustomerContext", menuName = "Tycoon/SO/CustomerContext")]
    public class CustomerContextSO : ScriptableObject
    {
        public CustomerBoardSO       CustomerBoard;
        public OrderBoardSO          OrderBoard;
        public WorkplaceRegisterSO   WorkplaceRegister;
        public PathRequestSO         PathRequest;
        public CounterBoardSO        CounterBoard;
    }
}
