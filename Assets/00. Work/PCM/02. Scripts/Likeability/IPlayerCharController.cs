using System;
using UnityEngine.UI;

public interface IPlayerCharController
{
    Action<int> levelTrigger { get; }
    CharacterSO characterData { get; }
    Image characterImage { get; }
    CharacterRegisterSO registerSO { get; }
    float GetExpRatio();
    void GiveItem(int expAmount);
}