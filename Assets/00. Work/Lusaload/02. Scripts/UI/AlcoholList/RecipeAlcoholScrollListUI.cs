using _00._Work.Lusaload._02._Scripts.SO;
using _00._Work.Lusaload._02._Scripts.UI.CocktailShaker;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.UI.AlcoholList
{
    public class RecipeAlcoholScrollListUI : MonoBehaviour, ISequenceReaderReceiver
    {
        [SerializeField] private Transform contentParent;
        [SerializeField] private BaseAlcoholButtonUI buttonPrefab;
 
        private ISequenceReader _sequenceReader;
 
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
 
        private void ClearChild()
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }
    }
}