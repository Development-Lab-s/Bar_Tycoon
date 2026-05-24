using _00._Work._Resources._02._Scripts.Systems.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial Data")]
    [SerializeField]
    private TuturialDatas[] tutorialDatas;

    [Header("UI")]
    [SerializeField] private GameObject parents;
    [SerializeField]private TMP_Text titleText;
    [SerializeField]private TMP_Text descText;
    [SerializeField]private TMP_Text pageText;
    [SerializeField]private Image tutorialImage;
    private int pageIndex;
    private int currentTutorialIndex = -1;
    private ToturialInfoSO[] currentPages;

    private void Awake()
    {
        LoadTutorial();
        if (parents.activeSelf)
        {
            parents.SetActive(false);
        }       
    }

    public void OpenTutorial()
    {
        for (int i = 0; i < tutorialDatas.Length; i++)
        {
            if (!tutorialDatas[i].isEnd)
            {
                Debug.Log(i+ " " + tutorialDatas[i].isEnd);
                currentTutorialIndex = i;
                currentPages = tutorialDatas[i].data;
                pageIndex = 0;
                parents.SetActive(true);
                ShowTutorial(pageIndex);
                //SaveTutorial();
                return;
            }
        }

        Debug.Log("실행할 튜토리얼 없음");
    }

    public void Next()
    {
        if (currentPages == null)
            return;

        pageIndex++;

        // 마지막 페이지 도달
        if (pageIndex >= currentPages.Length)
        {
            tutorialDatas[currentTutorialIndex].isEnd = true;

            parents.SetActive(false);

            Debug.Log(
                $"{tutorialDatas[currentTutorialIndex].TutorialName} 완료");

            return;
        }

        ShowTutorial(pageIndex);
    }

    public void Prev()
    {
        if (currentPages == null)
            return;

        pageIndex--;

        if (pageIndex < 0)
            pageIndex = 0;

        ShowTutorial(pageIndex);
    }

    private void ShowTutorial(int index)
    {
        ToturialInfoSO tutorialData =
            currentPages[index];

        titleText.text = tutorialData.title;

        descText.text = tutorialData.description;

        tutorialImage.sprite = tutorialData.image;

        pageText.text =
            $"{index + 1}/{currentPages.Length}";
    }
    private void SaveTutorial()
    {
        TutorialSaveData saveData =
            new TutorialSaveData();

        for (int i = 0; i < tutorialDatas.Length; i++)
        {
            saveData.tutorialEnds.Add(
                tutorialDatas[i].isEnd);
        }
        SaveManager.Save(
            saveData,
            "Tutorial.save",
            "Tutorial");
    }
    private void LoadTutorial()
    {
        if (!SaveManager.IsSaveFile(
            "Tutorial.save",
            "Tutorial"))
        {
            return;
        }

        TutorialSaveData saveData =
            (TutorialSaveData)SaveManager.Load(
                typeof(TutorialSaveData),
                "Tutorial.save",
                "Tutorial");

        for (int i = 0;
             i < tutorialDatas.Length &&
             i < saveData.tutorialEnds.Count;
             i++)
        {
            tutorialDatas[i].isEnd =
                saveData.tutorialEnds[i];
        }
    }
}