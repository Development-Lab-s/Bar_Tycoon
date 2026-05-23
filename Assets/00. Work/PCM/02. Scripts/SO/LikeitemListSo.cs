using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "characterLikeitemSo", menuName = "Scriptable Objects/characterLikeitemSo")]
public class LikeitemListSo : ScriptableObject
{
    public List<CharacterSO> character = new List<CharacterSO>();

    public string MostCharacter()
    {
        character = character.OrderBy(x => x.currentExp).ToList();
        return character[0].characterName;
    }
}
