using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Tycoon
{
    /// <summary>
    /// 카운터 상태를 관리하는 Runtime SO.
    ///
    /// CounterWorkplace(씬 오브젝트)를 씬 초기화 시 주입받아,
    /// WorkSO가 씬 오브젝트를 직접 참조하지 않아도 되게 한다.
    /// </summary>
    [CreateAssetMenu(fileName = "CounterBoard", menuName = "Tycoon/SO/CounterBoard")]
    public class CounterBoardSO : ScriptableObject
    {
        [SerializeField] private EventChannelSO _scheduleTriggerChannel;

        private CounterWorkplace _counter;

        public void Initialize(CounterWorkplace counter)
        {
            _counter = counter;
            _counter.OnReadyOrderAdded     += TriggerSchedule;
            _counter.OnPayingCustomerAdded += TriggerSchedule;
        }

        public void AddReadyOrder(OrderTicket ticket)
        {
            _counter?.AddReadyOrder(ticket);
        }

        public CounterWorkplace Counter => _counter;

        private void TriggerSchedule()
            => _scheduleTriggerChannel?.RaiseEvent(new BBJ.Schedule.ScheduleTriggerEvent());

        private void OnDisable()
        {
            if (_counter != null)
            {
                _counter.OnReadyOrderAdded     -= TriggerSchedule;
                _counter.OnPayingCustomerAdded -= TriggerSchedule;
            }
            _counter = null;
        }
    }
}
