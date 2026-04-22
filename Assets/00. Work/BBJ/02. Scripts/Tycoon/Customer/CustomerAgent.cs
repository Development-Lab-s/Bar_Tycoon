using BBJ.Modules;
using Gamelib.ObjectPool.Runtime;
using System.Collections;
using UnityEngine;

namespace BBJ.Tycoon
{
    public class CustomerAgent : PoolableMono
    {
        [SerializeField] private CustomerContextSO _context;

        [Header("Settings")]
        [SerializeField] private float _eatDuration   = 8f;
        [SerializeField] private float _patienceLimit = 60f;

        public CustomerPhase CurrentPhase { get; private set; } = CustomerPhase.Entering;

        private SeatWorkplace      _mySeat;
        private OrderTicket        _myTicket;
        private FoodDataSO         _selectedFood;
        private PathMovementModule _movement;

        private void Awake() => _movement = GetComponent<PathMovementModule>();

        public override void ResetItem()
        {
            _mySeat       = null;
            _myTicket     = null;
            _selectedFood = null;
            CurrentPhase  = CustomerPhase.Entering;
        }

        public void StartCycle(FoodDataSO food)
        {
            _selectedFood = food;
            StartCoroutine(RunCycle());
        }

        private IEnumerator RunCycle()
        {
            // 1. 자리 탐색
            ChangePhase(CustomerPhase.Entering);

            var candidates = _context.WorkplaceRegister.GetCandidates(
                transform.position, WorkplaceType.Seat, 1);
            var seat = candidates.Count > 0 ? candidates[0] as SeatWorkplace : null;

            if (seat == null) { yield return Leave(); yield break; }

            seat.AssignCustomer(this);
            _mySeat = seat;
            yield return MoveToWorld(seat.GetNearestPoint(transform.position));

            // 2. 주문 대기
            ChangePhase(CustomerPhase.WaitingOrder);
            yield return new WaitUntil(() =>
                CurrentPhase == CustomerPhase.WaitingFood ||
                CurrentPhase == CustomerPhase.Leaving);

            if (CurrentPhase == CustomerPhase.Leaving) yield break;

            // 3. 음식 대기 (인내심)
            float waited = 0f;
            yield return new WaitUntil(() =>
            {
                waited += Time.deltaTime;
                return CurrentPhase == CustomerPhase.Eating || waited >= _patienceLimit;
            });

            if (CurrentPhase != CustomerPhase.Eating)
            {
                _mySeat.ClearCustomer();
                if (_myTicket != null) _context.OrderBoard.Unregister(_myTicket);
                yield return Leave();
                yield break;
            }

            // 4. 식사
            yield return new WaitForSeconds(_eatDuration);

            // 5. 계산대 이동
            ChangePhase(CustomerPhase.MovingToCounter);
            _mySeat.ClearCustomer();

            var counter = _context.CounterBoard.Counter;
            yield return MoveToWorld(counter.GetNearestPoint(transform.position));
            counter.EnqueuePaying(this);

            // 6. 계산 대기
            ChangePhase(CustomerPhase.WaitingPayment);
            yield return new WaitUntil(() => CurrentPhase == CustomerPhase.Leaving);

            yield return Leave();
        }

        public OrderTicket PlaceOrder(SeatWorkplace seat)
        {
            if (CurrentPhase != CustomerPhase.WaitingOrder) return null;
            _myTicket = new OrderTicket(_selectedFood, this, seat);
            ChangePhase(CustomerPhase.WaitingFood);
            return _myTicket;
        }

        public void OnFoodServed(OrderTicket ticket)
        {
            if (CurrentPhase != CustomerPhase.WaitingFood) return;
            ChangePhase(CustomerPhase.Eating);
        }

        public void OnPaymentDone()
        {
            if (_myTicket != null)
            {
                _myTicket.ChangeState(OrderState.Done);
                _context.OrderBoard.Unregister(_myTicket);
            }
            ChangePhase(CustomerPhase.Leaving);
        }

        private void ChangePhase(CustomerPhase phase) => CurrentPhase = phase;

        private IEnumerator MoveToWorld(Vector3 destination)
        {
            bool arrived = false;
            void OnArrived() => arrived = true;
            _movement.MoveComplectedEvent += OnArrived;

            _context.PathRequest.RequestPath(
                transform.position, destination,
                (path, success) =>
                {
                    if (success && path.Length > 0) _movement.OnPathMove(path);
                    else arrived = true;
                });

            yield return new WaitUntil(() => arrived);
            _movement.MoveComplectedEvent -= OnArrived;
        }

        private IEnumerator Leave()
        {
            ChangePhase(CustomerPhase.Leaving);
            yield return MoveToWorld(_context.CustomerBoard.ExitPoint);
            _context.CustomerBoard.ReturnToPool(this);
        }
    }
}
