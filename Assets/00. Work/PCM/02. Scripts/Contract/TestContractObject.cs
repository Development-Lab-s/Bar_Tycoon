using _00._Work.PCM._02._Scripts;
using Assets._00._Work.PCM._02._Scripts.Contract;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Assets._00._Work.PCM._02._Scripts
{
    public class TestContractObject : AbstructContractObject, IPlayer
    {
        [field:SerializeField]public CharcterLikeSO charLike { get; private set; }

        [field:SerializeField]public UnityEvent ChatOpen {  get; private set; }
    }
}