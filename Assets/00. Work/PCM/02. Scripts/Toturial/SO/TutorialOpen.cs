using _00._Work._Resources._02._Scripts.Systems.SaveSystem;
using UnityEngine;

public class TutorialOpen : MonoBehaviour
{
    [SerializeField] private TutorialManager _manager;

    private void Start()
    {
        _manager.OpenTutorial();
    }
}
