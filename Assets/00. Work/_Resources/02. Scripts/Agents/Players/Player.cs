using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using _00._Work._Resources._02._Scripts.Systems.GameEvents;
using Agents;
using Gamelib.EventSystem;
using System;
using Systems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00._Work._Resources._02._Scripts.Agents.Players
{
    public class Player : Agent
    {
        [Header("Player values")] 
        [field: SerializeField] public EventChannelSO PlayerEventChannel { get; private set; }

        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }
    }
}