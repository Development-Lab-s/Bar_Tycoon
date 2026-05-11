using System.Collections.Generic;
using _00._Work.Lusaload._02._Scripts.SO;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    public class CocktailOrderSequence
    {
        private readonly List<BaseAlcoholDataSO> _expectedOrder;
        private readonly List<BaseAlcoholDataSO> _addedAlcohols = new();
        private SequenceState _state = SequenceState.InProgress;
 
        public IReadOnlyList<BaseAlcoholDataSO> ExpectedOrder => _expectedOrder;
        public IReadOnlyList<BaseAlcoholDataSO> AddedAlcohols => _addedAlcohols;
        public SequenceState State => _state;
        public bool IsFull => _addedAlcohols.Count >= _expectedOrder.Count;
 
        public CocktailOrderSequence(IReadOnlyList<BaseAlcoholDataSO> recipeIngredients, bool shuffle = true)
        {
            _expectedOrder = new List<BaseAlcoholDataSO>(recipeIngredients);
 
            if (shuffle)
                ShuffleExpectedOrder();
        }
 
        private void ShuffleExpectedOrder()
        {
            for (int i = _expectedOrder.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (_expectedOrder[i], _expectedOrder[randomIndex]) = (_expectedOrder[randomIndex], _expectedOrder[i]);
            }
        }
 
        public AddAlcoholResult TryAdd(BaseAlcoholDataSO alcohol)
        {
            if (_state == SequenceState.Completed)
                return AddAlcoholResult.Completed;
 
            if (IsFull)
                return AddAlcoholResult.Full;
 
            if (_addedAlcohols.Contains(alcohol))
                return AddAlcoholResult.AlreadyContained;
 
            _addedAlcohols.Add(alcohol);
 
            bool isIngredientValid = _expectedOrder.Contains(alcohol);
            bool isOrderValid = isIngredientValid && _expectedOrder[_addedAlcohols.Count - 1] == alcohol;
 
            if (!isIngredientValid)
            {
                _state = SequenceState.Failed;
                return IsFull ? AddAlcoholResult.FullAfterFail : AddAlcoholResult.WrongIngredient;
            }
 
            if (!isOrderValid)
            {
                _state = SequenceState.Failed;
                return IsFull ? AddAlcoholResult.FullAfterFail : AddAlcoholResult.WrongOrder;
            }
 
            if (_state == SequenceState.Failed)
            {
                return IsFull ? AddAlcoholResult.FullAfterFail : AddAlcoholResult.AddedAfterFail;
            }
 
            if (IsFull)
            {
                _state = SequenceState.Completed;
                return AddAlcoholResult.Completed;
            }
 
            return AddAlcoholResult.Added;
        }
    }
}