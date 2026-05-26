using _00._Work._Resources._02._Scripts.Agents.Players;
using _00._Work._Resources._02._Scripts.Modules;
using Assets._00._Work.PCM._02._Scripts;
using BBJ.Modules; // IHoverRenderer ���ӽ����̽�
using UnityEngine;
using UnityEngine.Events;

namespace _00._Work.PCM._02._Scripts
{
    public abstract class AbstructContractObject : TestContractObject, ILikeabillty, IChatProvider
    {
        [field: SerializeField] public CharacterEnum characterEnum { get; set; }
        [field: SerializeField] public UnityEvent<int> OnLike { get; set; }
        [field: SerializeField] public string ChatMessage { get; set; }
    }
}