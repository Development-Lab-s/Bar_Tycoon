using System.Collections.Generic;
using _00._Work.Lusaload._02._Scripts.SO;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    // 칵테일 한 잔을 만들기 위한 재료 투입 순서와 성공/실패 상태를 관리하는 클래스
    public class CocktailOrderSequence
    {
        private readonly List<BaseAlcoholDataSO> _expectedOrder;        // 투입해야 할 재료와 순서 (셔플 적용 후)
        private readonly List<BaseAlcoholDataSO> _addedAlcohols = new(); // 실제로 투입된 재료 목록
        private SequenceState _state = SequenceState.InProgress;         // 현재 제조 상태

        public IReadOnlyList<BaseAlcoholDataSO> ExpectedOrder => _expectedOrder;  // 요구 순서 읽기 전용 뷰
        public IReadOnlyList<BaseAlcoholDataSO> AddedAlcohols => _addedAlcohols;  // 투입 재료 읽기 전용 뷰
        public SequenceState State => _state;                                      // 현재 InProgress/Completed/Failed
        public bool IsFull => _addedAlcohols.Count >= _expectedOrder.Count;        // 투입 슬롯이 모두 채워졌는지 여부

        // recipeIngredients를 복사해 시퀀스 생성. shuffle=true면 투입 순서를 랜덤화
        public CocktailOrderSequence(IReadOnlyList<BaseAlcoholDataSO> recipeIngredients, bool shuffle = true)
        {
            _expectedOrder = new List<BaseAlcoholDataSO>(recipeIngredients);

            if (shuffle)
                ShuffleExpectedOrder();
        }

        // Fisher-Yates 알고리즘으로 _expectedOrder를 제자리 셔플
        private void ShuffleExpectedOrder()
        {
            for (int i = _expectedOrder.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (_expectedOrder[i], _expectedOrder[randomIndex]) = (_expectedOrder[randomIndex], _expectedOrder[i]);
            }
        }
 
        // 재료를 셰이커에 추가 시도. 이미 완성/가득 찬 경우, 중복·잘못된 재료/순서를 판별해 결과 반환
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