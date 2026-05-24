using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangess : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainSceneName;
    public void HandleSceneChanged()
    {
        ReturnToMainScene();
    }

    private void ReturnToMainScene()
    {
        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            Debug.LogWarning("[SceneChangess] mainSceneName이 비어 있습니다. scene load를 중단합니다.");
            return;
        }

        SceneManager.LoadScene(mainSceneName);
    }
}
