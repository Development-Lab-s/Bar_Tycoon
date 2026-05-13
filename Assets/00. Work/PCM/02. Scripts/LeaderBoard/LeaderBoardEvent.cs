using Gamelib.EventSystem;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

public class LeaderBoardEvent : GameEvent
{
    public List<LeaderboardEntry> Entris { get;}
    public string Id { get;}
    public LeaderBoardEvent(string id,List<LeaderboardEntry> entris)
    {
        Id = id;
        Entris = entris;
    }
}
