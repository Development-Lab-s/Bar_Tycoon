using _00._Work._Resources._02._Scripts.Systems.SaveSystem;
using Gamelib.EventSystem;
using UnityEngine;

public class TutorialOpen : MonoBehaviour
{
    [SerializeField] private TutorialManager _manager;
    public void Open()
    {
        _manager.OpenTutorial();
    }
}
