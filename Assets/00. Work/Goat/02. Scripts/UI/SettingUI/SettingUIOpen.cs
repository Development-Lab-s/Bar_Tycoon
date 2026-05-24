using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.SettingUI
{
    public class SettingUIOpen : MonoBehaviour
    {
        [SerializeField] private GameObject targetObject;

        public void ToggleUI()
        {
            if (targetObject == null)
                return;

            targetObject.SetActive(!targetObject.activeSelf);
        }
    }
}