using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts.Contract
{
    [CreateAssetMenu(fileName = "ChatSO", menuName = "Chat/ChatSO")]
    public class ChatSO : ScriptableObject
    {
        public List<string> Line = new();
    }
}