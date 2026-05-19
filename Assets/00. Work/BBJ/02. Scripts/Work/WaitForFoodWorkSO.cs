using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.Order;
using Cysharp.Threading.Tasks;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [UnityEngine.CreateAssetMenu(fileName = "WaitForFoodWork", menuName = "Tycoon/Work/WaitForFood")]
    public class WaitForFoodWorkSO : WorkSO
    {
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var actions  = executor.GetModule<AgentActionModule>();
            if (customer == null || actions == null) return WorkResult.Cancelled;
            await actions.Execute<WaitAction>(a => a.ExecuteAsync(() => customer.FoodServed, ctx.Token));
            return WorkResult.Completed;
        }
    }
}
