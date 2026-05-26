using _00._Work._Resources._02._Scripts.Systems.SaveSystem;
using Gamelib.EventSystem;
using UnityEngine;

public class TutorialOpen : MonoBehaviour
{
    [SerializeField] private TutorialManager _manager;
    [SerializeField] private TutoBoolSO Tutobol;
    private void Start()
    {
        Open();
    }
    public void Open()
    {
        if (Tutobol.value)
        {
            _manager.OpenTutorial();
            Tutobol.value = false;
        }
    }
}
