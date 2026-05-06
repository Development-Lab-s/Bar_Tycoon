using _00._Work._Resources._02._Scripts.Modules;
using Assets._00._Work.PCM._02._Scripts._TileChange;
using Gamelib.SoundSystem;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LPBOX : MonoBehaviour, ILPBOX
{
    public event Action<int> OnLPClicked;
    private int _myId;
    private LP _lpScript;
    private Button button;

    public void SetUp(int id)
    {
        _lpScript = GetComponentInChildren<LP>();
        button = GetComponent<Button>();
        button.onClick.AddListener(() => OnLPClicked?.Invoke(_myId));
        _myId = id;
        if (_lpScript != null) _lpScript.sound = (BgmSounds)id;
    }
    public void Select() => _lpScript.Active();
    public void Stop() => _lpScript.Stop();
}
