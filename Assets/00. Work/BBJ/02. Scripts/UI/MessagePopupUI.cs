using System.Collections;
using BBJ.EventSystem;
using Gamelib.EventSystem;
using TMPro;
using UnityEngine;

namespace BBJ.UI
{
    public class MessagePopupUI : MonoBehaviour
    {
        [SerializeField] private EventChannelSO _uiChannel;
        [SerializeField] private GameObject     _panel;
        [SerializeField] private TMP_Text       _label;
        [SerializeField] private float          _displayDuration = 2f;

        private Coroutine _hideRoutine;

        private void OnEnable()  => _uiChannel?.AddListener<MessageEvent>(OnMessage);
        private void OnDisable() => _uiChannel?.RemoveListener<MessageEvent>(OnMessage);

        private void OnMessage(MessageEvent e)
        {
            _label.text = e.Message;
            _panel.SetActive(true);

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(_displayDuration);
            _panel.SetActive(false);
            _hideRoutine = null;
        }
    }
}
