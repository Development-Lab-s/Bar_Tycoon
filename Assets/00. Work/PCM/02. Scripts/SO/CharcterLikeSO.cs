using Assets._00._Work.PCM._02._Scripts.Contract;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharcterLikeSO", menuName = "Scriptable Objects/CharcterLikeSO")]
public class CharcterLikeSO : ScriptableObject
{
    [System.Serializable]
    public struct DialogueData
    {
        [TextArea(2, 5)] public string context;
        //public Sprite characterFace;
    }
    public List<DialogueData> DialogueDataList = new();
}
