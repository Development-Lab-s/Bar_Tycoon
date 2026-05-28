using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI
{
    public class ResetSaveButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _settingsPanel;

        private void OnEnable()  => _button.onClick.AddListener(OnClick);
        private void OnDisable() => _button.onClick.RemoveListener(OnClick);

        private void OnClick()
        {
            _settingsPanel?.SetActive(false);
            GameLoader.DeleteSaveOnQuit = true;
            Application.Quit();
        }
    }
}
