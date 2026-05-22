using System.Threading;
using BBJ.WorkplaceSystem;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.Lusaload._02._Scripts.SO;

namespace BBJ.Order
{
    public class OrderTicket
    {
        public CocktailRecipeSO Ordered  { get; }
        public ModuleOwner      Customer { get; internal set; }
        public Workplace        Seat     { get; internal set; }

        public OrderState     State              { get; private set; } = OrderState.Waiting;
        public OrderWorkPhase WorkPhase          { get; internal set; } = OrderWorkPhase.ReadyForServer;
        public ModuleOwner    ReservedBy         { get; private set; }
        public CancelReason?  CancellationReason { get; private set; }

        // CancellationTokenSource — Cancel() 시 연결된 워커에 전파
        private readonly CancellationTokenSource _cts = new();
        public CancellationToken Token => IsTerminal ? CancellationToken.None : _cts.Token;
        public bool IsTerminal        => State is OrderState.Done or OrderState.Cancelled;
        public bool IsPlayerActionable => WorkPhase is OrderWorkPhase.ReadyForServer
                                                    or OrderWorkPhase.ReadyForServe
                                                    or OrderWorkPhase.ReadyForCashier;

        public OrderTicket(CocktailRecipeSO food, ModuleOwner customer, Workplace seat)
        {
            Ordered     = food;
            Customer = customer;
            Seat     = seat;
        }

        internal bool Advance()
        {
            if (State >= OrderState.InProgress) return false;
            State++;
            return true;
        }

        public bool TryReserve(ModuleOwner actor)
        {
            if (actor == null || State != OrderState.Waiting) return false;
            ReservedBy = actor;
            return Advance();
        }

        public bool TryStartProgress(ModuleOwner actor)
        {
            if (State != OrderState.Reserved || ReservedBy != actor) return false;
            return Advance();
        }

        // 플레이어 뺏기: 진행 중인 작업이라도 소유권을 강제 이전하고 상태를 Reserved로 되돌린다.
        public bool TrySteal(ModuleOwner newOwner)
        {
            if (State == OrderState.Done || State == OrderState.Cancelled) return false;
            ReservedBy = newOwner;
            State      = OrderState.Reserved;
            return true;
        }

        internal void Release()
        {
            ReservedBy = null;
            State = OrderState.Waiting;
        }

        internal void Finish()
        {
            ReservedBy = null;
            State = OrderState.Done;
            _cts.Dispose();
        }

        internal void Cancel(CancelReason reason)
        {
            CancellationReason = reason;
            ReservedBy = null;
            State = OrderState.Cancelled;
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
