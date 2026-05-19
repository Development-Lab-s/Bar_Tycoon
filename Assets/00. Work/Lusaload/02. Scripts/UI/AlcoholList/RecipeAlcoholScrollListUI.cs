using _00._Work.Lusaload._02._Scripts.SO;
using _00._Work.Lusaload._02._Scripts.UI.CocktailShaker;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.UI.AlcoholList
{
    // 현재 시퀀스의 요구 재료 목록을 실시간으로 반영하는 스크롤 리스트 UI
    public class RecipeAlcoholScrollListUI : MonoBehaviour, ISequenceReaderReceiver
    {
        [SerializeField] private Transform contentParent;          // 버튼들이 생성될 부모 Transform
        [SerializeField] private BaseAlcoholButtonUI buttonPrefab; // 재료 버튼 프리팹

        private ISequenceReader _sequenceReader; // 시퀀스 변경 알림을 받기 위한 의존성
 
        // 시퀀스 리더를 교체하고 현재 시퀀스로 목록을 즉시 갱신
        public void SetSequenceReader(ISequenceReader reader)
        {
            if (_sequenceReader != null)
                _sequenceReader.OnSequenceChanged -= OnSequenceChanged;
 
            _sequenceReader = reader;
 
            if (_sequenceReader != null)
            {
                _sequenceReader.OnSequenceChanged += OnSequenceChanged;
                BuildList(_sequenceReader.CurrentSequence);
            }
        }
 
        private void OnDestroy()
        {
            if (_sequenceReader != null)
                _sequenceReader.OnSequenceChanged -= OnSequenceChanged;
        }
 
        private void OnSequenceChanged(CocktailOrderSequence sequence)
        {
            BuildList(sequence);
        }
 
        // sequence의 ExpectedOrder 순서대로 버튼을 재생성. null이면 목록을 비움
        private void BuildList(CocktailOrderSequence sequence)
        {
            ClearChild();
 
            if (sequence == null)
                return;
 
            foreach (BaseAlcoholDataSO alcoholData in sequence.ExpectedOrder)
            {
                if (alcoholData == null) continue;
 
                BaseAlcoholButtonUI buttonUI = Instantiate(buttonPrefab, contentParent);
                buttonUI.SetData(alcoholData);
            }
        }
 
        // contentParent의 모든 자식 오브젝트를 역순으로 제거
        private void ClearChild()
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }
    }
}