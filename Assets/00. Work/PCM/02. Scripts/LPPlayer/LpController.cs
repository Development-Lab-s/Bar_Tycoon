using _00._Work._Resources._02._Scripts.Agents.Players;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LpController : MonoBehaviour
{
    private Dictionary<int, LPBOX> Lpcontroll = new();
    private int currentActiveId = -1;
    private void Awake()
    {
        LPBOX[] Lpbox = GetComponentsInChildren<LPBOX>(); 
        Debug.Log(Lpbox.Length);
        for(int i = 0; i < Lpbox.Length; i++)
        {
            Lpcontroll.Add(i,Lpbox[i]);

            Lpbox[i].SetUp(i);
            Lpbox[i].OnLPClicked += PlayLp;
        }            
    }

    private void PlayLp(int id)
    {
        if (currentActiveId == id) return;
        if (currentActiveId != -1) Lpcontroll[currentActiveId].Stop();
        Lpcontroll[id].Select();
        currentActiveId = id;
    }
    private void OnDestroy()
    {
        foreach (var box in Lpcontroll.Values)
            box.OnLPClicked -= PlayLp;
    }
}
