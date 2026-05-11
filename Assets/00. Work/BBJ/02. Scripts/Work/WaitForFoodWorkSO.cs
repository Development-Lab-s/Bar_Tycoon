using BBJ.Actions;
using BBJ.Customer;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "WaitForFoodWork", menuName = "Tycoon/Work/WaitForFood")]
    public class WaitForFoodWorkSO : WorkSO
    {
        //[SerializeField] private float _timeout = 120f;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            if (customer == null || agent == null) return;
            await agent.WaitUntilAsync(() => customer.FoodServed, ct);
        }
    }
}
