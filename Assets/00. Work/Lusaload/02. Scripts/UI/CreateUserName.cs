using System.Text;
using _00._Work.Lusaload._02._Scripts.SO;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _00._Work.Lusaload._02._Scripts.UI
{
    public class CreateUserName : MonoBehaviour
    {
        private TMP_InputField _inputField;
        [SerializeField] private NameSO nameSO;
        [SerializeField] private GameObject warningLabel;

        private const int MaxLength = 8;
        private bool _isChangingText;

        private void Awake()
        {
            _inputField = GetComponent<TMP_InputField>();
            _inputField.lineType = TMP_InputField.LineType.SingleLine;
            _inputField.onValueChanged.AddListener(HandleInputValueChanged);
            _inputField.onSubmit.AddListener(SaveName);

            if (warningLabel != null)
                warningLabel.SetActive(false);
        }

        private void Update()
        {
            if (_inputField == null || !_inputField.isFocused)
                return;

            // 한글 조합 중에는 강제로 처리하지 않음
            if (!string.IsNullOrEmpty(Input.compositionString))
                return;

            ApplyFilter(_inputField.text);
        }

        private void OnDestroy()
        {
            if (_inputField == null)
                return;

            _inputField.onValueChanged.RemoveListener(HandleInputValueChanged);
            _inputField.onSubmit.RemoveListener(SaveName);
        }

        private void HandleInputValueChanged(string input)
        {
            if (_isChangingText)
                return;

            // 한글 조합 중에는 입력을 건드리지 않음
            if (!string.IsNullOrEmpty(Input.compositionString))
                return;

            ApplyFilter(input);
        }

        private void ApplyFilter(string input)
        {
            string filtered = FilterKoreanEnglish(input);

            if (filtered != input)
            {
                _isChangingText = true;
                _inputField.SetTextWithoutNotify(filtered);
                _inputField.caretPosition = filtered.Length;
                _isChangingText = false;
                input = filtered;
            }

            // 8글자 초과 시 경고 실시간 표시
            if (warningLabel != null)
                warningLabel.SetActive(input.Length > MaxLength);
        }

        // 한국어/영어만 허용, 글자 수 제한 없음 (경고로 안내)
        private string FilterKoreanEnglish(string input)
        {
            var builder = new StringBuilder();
            foreach (char c in input)
            {
                if (IsKoreanOrEnglish(c))
                    builder.Append(c);
            }
            return builder.ToString();
        }

        private bool IsKoreanOrEnglish(char c)
        {
            bool isEnglish = c is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
            bool isKorean =
                c is >= '가' and <= '힣' ||
                c is >= 'ㄱ' and <= 'ㅎ' ||
                c is >= 'ㅏ' and <= 'ㅣ';
            return isEnglish || isKorean;
        }

        private void SaveName(string input)
        {
            string finalName = FilterKoreanEnglish(input);

            if (string.IsNullOrEmpty(finalName) || finalName.Length > MaxLength)
            {
                if (warningLabel != null)
                    warningLabel.SetActive(finalName.Length > MaxLength);

                _inputField.ActivateInputField();
                _inputField.caretPosition = _inputField.text.Length;
                return;
            }

            if (nameSO == null)
            {
                Debug.LogWarning("NameSO가 연결되지 않았습니다.");
                return;
            }

            nameSO.SetName(finalName);

            if (warningLabel != null)
                warningLabel.SetActive(false);

#if UNITY_EDITOR
            EditorUtility.SetDirty(nameSO);
            AssetDatabase.SaveAssets();
#endif
        }
    }
}
