using BBJ.EventSystem;
using BBJ.Order;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work.Lusaload._02._Scripts.Recipe;
using _00._Work.Lusaload._02._Scripts.UI.CocktailShaker;
using BBJ.Work;

namespace BBJ.Scene
{
    public class CocktailSceneHost : MonoBehaviour, ISceneHost
    {
        [SerializeField] private EventChannelSO        _orderChannel;
        [SerializeField] private CocktailRecipeManager _recipeManager;
        [SerializeField] private CocktailShaker        _shaker;
        [SerializeField] private EventChannelSO        _sceneChannel;

        private enum Result { None, Success, Fail }
        private Result _result;

        public SceneType SceneType => SceneType.Cocktail;

        private void Awake()
        {
            UtilDebugger.AssertAllAssigned(this);
            GameSceneManager.Instance.RegisterHost(this);
            _shaker.OnCocktailSuccess += OnShakerSuccess;
            _shaker.OnCocktailFail    += OnShakerFail;
        }

        private void OnDestroy()
        {
            _shaker.OnCocktailSuccess -= OnShakerSuccess;
            _shaker.OnCocktailFail    -= OnShakerFail;
        }

        public void OnForeground()
        {
            _result = Result.None;
            var ticket = CocktailTransitionTrigger.Instance.PendingTicket;
            if (ticket != null)
                _recipeManager.SetRecipe(ticket.Ordered);
        }

        public void OnBackground()
        {
            var ticket = CocktailTransitionTrigger.Instance.PendingTicket;
            CocktailTransitionTrigger.Instance.Clear();

            if (ticket == null) return;

            if (_result == Result.Success)
                _orderChannel.RaiseEvent(new OrderNotifyCompleteEvent(ticket, ticket.ReservedBy));
            else if (_result == Result.Fail)
                _orderChannel.RaiseEvent(new OrderNotifyReleasedEvent(ticket, ticket.ReservedBy));
        }

        private void OnShakerSuccess()
        {
            _result = Result.Success;
            _sceneChannel.RaiseEvent(new SceneTransitionRequestEvent(SceneType.Main));
        }

        private void OnShakerFail()
        {
            _result = Result.Fail;
            _sceneChannel.RaiseEvent(new SceneTransitionRequestEvent(SceneType.Main));
        }
    }
}
