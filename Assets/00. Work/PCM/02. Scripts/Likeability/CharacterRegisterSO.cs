using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterRegisterSO", menuName = "CharLike/CharacterRegisterSO")]
public class CharacterRegisterSO : ScriptableObject
{
    private List<PlayerCharController> Registry = new List<PlayerCharController>();

    public void Register(PlayerCharController character)
    {
        if (!Registry.Contains(character)) Registry.Add(character);
    }

    public void Unregister(PlayerCharController character)
    {
        if (Registry.Contains(character)) Registry.Remove(character);
    }

    public PlayerCharController GetCharacterByName(string name)
    {
        return Registry.FirstOrDefault(c => c.characterData != null && c.characterData.characterName == name);
    }

    // 2. 특정 기준 위치에서 가장 가까운 캐릭터 컨트롤러 찾아오기
    public PlayerCharController GetClosestCharacter(Vector3 from)
    {
        return Registry
            .OrderBy(c => Vector3.Distance(from, c.transform.position))
            .FirstOrDefault();
    }
}
