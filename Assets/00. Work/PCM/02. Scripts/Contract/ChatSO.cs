using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatSO", menuName = "Chat/ChatSO")]
public class ChatSO : ScriptableObject
{
    public List<string> Line = new(); 
}
