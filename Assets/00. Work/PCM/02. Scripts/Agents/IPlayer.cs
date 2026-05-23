using Assets._00._Work.PCM._02._Scripts.Contract;
using System;
using UnityEngine;
using UnityEngine.Events;

public interface IPlayer
{
    public CharcterLikeSO CharlikeSo { get;}
    public UnityEvent<string> ChatOpen { get; }
}
