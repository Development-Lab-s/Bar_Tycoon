using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "characterLikeitemSo", menuName = "Scriptable Objects/characterLikeitemSo")]
public class LikeitemListSo : ScriptableObject
{
    public List<CharacterSO> character = new List<CharacterSO>();

    public string MostCharacter(CharacterRegisterSO registerSO)
    {
        if (registerSO == null || character.Count == 0) return string.Empty;

        CharacterSO best = null;
        int bestLevel = -1;

        foreach (var charSO in character)
        {
            if (charSO == null) continue;
            var ctrl = registerSO.GetById(charSO.id);
            if (ctrl == null) continue;
            if (ctrl.CurrentLevel > bestLevel)
            {
                bestLevel = ctrl.CurrentLevel;
                best = charSO;
            }
        }

        return best != null ? best.characterName : character[0].characterName;
    }
}
