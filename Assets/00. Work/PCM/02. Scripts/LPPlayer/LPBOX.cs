using Assets._00._Work.PCM._02._Scripts.LPPlayer;
using Gamelib.SoundSystem;
using System;
using UnityEngine;
using UnityEngine.UI;

public class LPBOX : MonoBehaviour
{
    [SerializeField]private SideLp _lpScript;
    public event Action<int> OnLPClicked;
    private int _myId;
    private Button _button;
    private bool _isOpen = false;
    public void SetUp(int id)
    {
        Debug.Log("adad");
        _myId = id;
        _button = GetComponent<Button>();
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => OnLPClicked?.Invoke(_myId));

        // LP 스크립트에 사운드 ID 미리 전달
        //if (_lpScript != null) _lpScript.sound = (BgmSounds)id;
    }
    public void Select()
    {
        if (_isOpen) return;
        if (_lpScript != null)
        {
            _isOpen = true;
            _lpScript.Active();
            _lpScript.PlaySound((BgmSounds)_myId);
        }
    }
    public string ChangeName()
    {
        return _lpScript.NameChosing(_myId);
    }

    public void StopLP()
    {
        _lpScript.Stop();
        _isOpen = false;
    }

    public void Show()
    {
        _lpScript.Active();
    }
}