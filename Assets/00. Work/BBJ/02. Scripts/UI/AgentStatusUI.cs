using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI
{
    public class AgentStatusUI : MonoBehaviour, IAgentUI
    {
        [SerializeField] private Image    _icon;
        [SerializeField] private TMP_Text _label;

        private void Start() { gameObject.SetActive(false); }

        public void OnOpen()  { gameObject.SetActive(true); }
        public void OnClose() { gameObject.SetActive(false); }

        public void SetIcon(Sprite sprite)
        {
            if (_icon == null) return;
            _icon.sprite = sprite;
            _icon.gameObject.SetActive(true);
            _label.gameObject.SetActive(false);
        }

        public void SetText(string text)
        {
            if (_label == null) return;
            _label.text = text;
            _icon.gameObject.SetActive(false);
            _label.gameObject.SetActive(true);
        }
    }
}
