using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private TuturialDatas[] tutorialDatas;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private Image tutorialImage;
    private void Start()
    {
        gameObject.SetActive(false);
    }
    private int currentIndex;
    private ToturialInfoSO[] data = null;
    public void OpenTutorial(int tutorialIndex)
    {
        gameObject.SetActive(true);
        data = tutorialDatas[tutorialIndex].data;
        currentIndex = 0;
        ShowTutorial(currentIndex);
    }
    public void Next()
    {
        currentIndex++;

        if (currentIndex >= data.Length)
        {
            gameObject.SetActive(false);
            currentIndex = data.Length - 1;
        }
        Debug.Log(currentIndex);
        ShowTutorial(currentIndex);
    }

    public void Prev()
    {
        currentIndex--;

        if (currentIndex < 0)
            currentIndex = 0;

        ShowTutorial(currentIndex);
    }

    private void ShowTutorial(int index)
    {
        ToturialInfoSO data = this.data[index];

        titleText.text = data.title;
        descText.text = data.description;
        tutorialImage.sprite = data.image;

        pageText.text =
            $"{index + 1}/{this.data.Length}";
    }
}