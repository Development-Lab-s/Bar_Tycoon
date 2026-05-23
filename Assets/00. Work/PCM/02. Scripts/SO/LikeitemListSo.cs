using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "characterLikeitemSo", menuName = "Scriptable Objects/characterLikeitemSo")]
public class LikeitemListSo : ScriptableObject
{
    public List<CharItemSO> itemSo = new List<CharItemSO>();

    public string MostCharacter()
    {
        itemSo = itemSo.OrderBy(x => x.CurrentCount).ToList();
        return itemSo[0].Ownercharacter;
    }
}
