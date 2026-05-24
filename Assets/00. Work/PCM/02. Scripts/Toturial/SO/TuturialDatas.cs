using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "TuturialDatas", menuName = "Scriptable Objects/TuturialDatas")]
public class TuturialDatas : ScriptableObject
{
    public bool isEnd;
    public string TutorialName;
    public ToturialInfoSO[] data;   
}
