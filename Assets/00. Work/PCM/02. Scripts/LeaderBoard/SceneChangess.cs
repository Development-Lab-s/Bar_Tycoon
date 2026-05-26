using _00._Work._Resources._02._Scripts.Systems.SaveSystem;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using _00._Work.Goat._02._Scripts.SaveCode;
using BBJ.EventSystem;
using BBJ.Scene;
using Gamelib.EventSystem;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class SceneChangess : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private StoryEpisodeSO targetEpisode;
    [SerializeField] private EventChannelSO storyChannelSo;
    [SerializeField] private EventChannelSO mainChannelSo;
    [SerializeField] private SceneType target;
    private bool firstStory;
    public bool register;
    private const string SaveSubFileName = "TitleSave.save";
    private void Awake()
    {
        LoadGameData();
        mainChannelSo.RaiseEvent(new SceneReadyEvent());
    }
    public void HandleSceneChanged()
    {
        ReturnToMainScene();
    }

    private void ReturnToMainScene()
    {

        if (!firstStory)
        {
            Debug.Log("실행");
            firstStory = true;
            register = true;
            SaveGameData();
            storyChannelSo?.RaiseEvent(new StoryEpisodeUnlockRequested(targetEpisode));
        }
        else
        {
            Debug.Log("실행2");
            mainChannelSo?.RaiseEvent(new SceneTransitionRequestEvent(target));
        }
    }
    public void SaveGameData()
    {
        TitleData dataToSave = GetSaveData();

        // SaveManager를 사용해 지정된 파일 이름으로 저장 (Default 폴더에 저장됨)
        SaveManager.Save(dataToSave, SaveSubFileName,"Title");
        Debug.Log("[SceneChangess] 세이브 성공! firstStory: " + firstStory + " " + register);
    }

    public void LoadGameData()
    {
        // 먼저 저장된 파일이 존재하는지 검사
        if (SaveManager.IsSaveFile(SaveSubFileName, "Title"))
        {
            // 파일을 로드하고 TitleData 타입으로 캐스팅합니다.
            TitleData loadedData = SaveManager.Load(typeof(TitleData),SaveSubFileName,"Title") as TitleData;

            if (loadedData != null)
            {
                firstStory = loadedData.firstStory;
                register = loadedData.registerId;
                Debug.Log("[SceneChangess] 세이브 로드 완료! firstStory: " + firstStory + " " + register);
                return;
            }
        }
        firstStory = false;
        Debug.Log("[SceneChangess] 저장된 세이브 파일이 없어 기본값(false)으로 시작합니다.");
    }
    public TitleData GetSaveData()
    {
        return new TitleData
        {
            registerId = register,
            firstStory = firstStory
        };
    } 
}
