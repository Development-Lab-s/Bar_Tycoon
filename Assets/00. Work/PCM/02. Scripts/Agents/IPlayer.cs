using Assets._00._Work.PCM._02._Scripts.Contract;
using System;
using UnityEngine;
using UnityEngine.Events;

public interface IPlayer
{
    public CharcterLikeSO charLike {get;}
    public UnityEvent ChatOpen { get; }
}
