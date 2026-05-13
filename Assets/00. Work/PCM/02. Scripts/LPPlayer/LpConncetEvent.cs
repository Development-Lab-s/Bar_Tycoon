using Gamelib.EventSystem;
using UnityEngine;

public class LpConncetEvent : GameEvent
{
    public LpConncetEvent(int id)
    {
        Id = id;
    }

    public int Id { get; private set; }


}
