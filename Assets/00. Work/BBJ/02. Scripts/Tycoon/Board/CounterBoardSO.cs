using BBJ.Schedule;
using BBJ.Tycoon.Data;
using BBJ.Tycoon.Workplaces;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Tycoon.Board
{
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

        public CounterWorkplace Counter => _counter;

        public void AddReadyOrder(OrderTicket ticket)
            => _counter?.AddReadyOrder(ticket);

        private void TriggerSchedule()
            => _scheduleTriggerChannel?.RaiseEvent(new ScheduleTriggerEvent());

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
