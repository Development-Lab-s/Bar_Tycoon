using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CharInfoUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CharListSO charList;

    [Header("UI References - List")]
    [SerializeField] private Transform content;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private GameObject charListIconPrefab;
    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;

    [Header("UI References - Detail")]
    [SerializeField] private Image characterImage;
    [SerializeField] private Image characterTinyImage;
    [SerializeField] private Image characterSDImage;
    [SerializeField] private Image[] Unlock;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI jobText;
    [SerializeField] private TextMeshProUGUI specialtyText;
    [SerializeField] private TextMeshProUGUI hobbyText;
    [SerializeField] private TextMeshProUGUI favoriteCocktailText;

    [Header("Settings")]
    [SerializeField] private int moveStep = 1;

    private List<CharListIcon> spawnedIcons = new();
    private int currentIndex = 0;

    [SerializeField]private SliderFill sliderFill;
    private void OnEnable()
    {
        SpawnIcons();
        currentIndex = 0;
        StartCoroutine(InitAfterLayout());
        sliderFill.ChoseCharacter = currentIndex;
    }

    private IEnumerator InitAfterLayout()
    {
        // 레이아웃 계산 완료 대기
        yield return new WaitForEndOfFrame();
        UpdateView();
    }

    private void Start()
    {
        leftArrow.onClick.AddListener(OnClickLeft);
        rightArrow.onClick.AddListener(OnClickRight);
    }

    private void SpawnIcons()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
        spawnedIcons.Clear();

        foreach (var charData in charList.charList)
        {
            var obj = Instantiate(charListIconPrefab, content);
            var icon = obj.GetComponent<CharListIcon>();
            icon.Setup(charData);
            spawnedIcons.Add(icon);
        }
    }

    private void UpdateView()
    {
        for (int i = 0; i < spawnedIcons.Count; i++)
        {
            float scale = (i == currentIndex) ? 1.1f : 0.9f;
            spawnedIcons[i].transform.localScale = Vector3.one * scale;
        }

        CenterToCurrentIndex();
        UpdateDetailUI(charList.charList[currentIndex]);
        Unlocker();

        leftArrow.interactable = currentIndex > 0;
        rightArrow.interactable = currentIndex < spawnedIcons.Count - 1;
    }

    private void CenterToCurrentIndex()
    {
        float iconX = spawnedIcons[currentIndex].GetComponent<RectTransform>().localPosition.x;
        float viewportCenterX = viewport.rect.width * 0.5f;
        Vector3 pos = content.localPosition;
        pos.x = -iconX + viewportCenterX;
        content.localPosition = pos;
    }

    private void UpdateDetailUI(CharacterDataSO data)
    {
        if (characterImage != null) characterImage.sprite = data.characterImage;
        if (characterTinyImage != null) characterTinyImage.sprite = data.characterImage;
        if (characterSDImage != null) characterSDImage.sprite = data.icon;
        if (nameText != null) nameText.text = data.characterName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (jobText != null) jobText.text = $"직업: {data.job}";
        if (specialtyText != null) specialtyText.text = $"특기: {data.specialty}";
        if (hobbyText != null) hobbyText.text = $"취미: {data.hobby}";
        if (favoriteCocktailText != null) favoriteCocktailText.text = $"좋아하는 칵테일: {data.favoriteCocktail}";
    }

    private void OnClickLeft()
    {
        currentIndex = Mathf.Max(0, currentIndex - moveStep);
        sliderFill.ChoseCharacter = currentIndex;
        UpdateView();
    }
    private void Unlocker()
    {
        for (int i = 0; i < Unlock.Length; i++)
        {
            Debug.Log(charList.charList[currentIndex]
                .charLevel.currentLevel);
            bool isUnlocked =
                charList.charList[currentIndex]
                .charLevel.currentLevel >= 2*(i+1);
            Unlock[i].gameObject.SetActive(!isUnlocked);
        }
    }
    private void OnClickRight()
    {
        currentIndex = Mathf.Min(spawnedIcons.Count - 1, currentIndex + moveStep);
        sliderFill.ChoseCharacter = currentIndex;
        UpdateView();
    }
}