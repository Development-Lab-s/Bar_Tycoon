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

    public PlayerCharController GetById(string id)
    {
        return Registry.FirstOrDefault(c => c.characterData != null && c.characterData.id == id);
    }

    public IReadOnlyList<PlayerCharController> GetAll() => Registry;

    public PlayerCharController GetClosestCharacter(Vector3 from)
    {
        return Registry
            .OrderBy(c => Vector3.Distance(from, c.transform.position))
            .FirstOrDefault();
    }
}
