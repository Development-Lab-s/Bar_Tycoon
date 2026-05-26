using Gamelib.SoundSystem;
using System;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class LPContents : MonoBehaviour
{
    private GridLayoutGroup _gridLayout;
    private RectTransform _rT;
    private void Awake()
    {
        _gridLayout = GetComponentInChildren<GridLayoutGroup>();
        _rT = GetComponent<RectTransform>();
    }
    private void OnEnable()
    {
        float newHeight = (_gridLayout.cellSize.y + _gridLayout.spacing.y) * (Enum.GetValues(typeof(BgmSounds)).Length) + 75;
        Vector2 size = _rT.sizeDelta;
        size.y = newHeight;
        _rT.sizeDelta = size;
        
    }
}
