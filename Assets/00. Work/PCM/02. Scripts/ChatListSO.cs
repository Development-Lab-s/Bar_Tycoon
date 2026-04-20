using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatListSO", menuName = "Scriptable Objects/ChatListSO")]
public class ChatListSO : ScriptableObject
{
    public List<ChatSO> LineList = new(); 
}
