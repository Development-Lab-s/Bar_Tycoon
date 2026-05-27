using System;
using System.Collections.Generic;
using BBJ.Order;
using BBJ.Staff;
using UnityEngine;

namespace BBJ.Save
{
    [Serializable]
    public class GameSaveData
    {
        public StageSaveData  Stage  = new();
        public OrdersSaveData Orders = new();
        public StaffSaveData  Staff  = new();
        public int            SelectedLpIndex = 0;
    }

    [Serializable]
    public class StageSaveData
    {
        public List<PlacedObstacleEntrySave> PlacedObjects = new();
    }

    [Serializable]
    public class PlacedObstacleEntrySave
    {
        public string     ObjectDataId;
        public Vector2Int CellIndex;
        public bool       FlipX;
    }

    [Serializable]
    public class OrdersSaveData
    {
        public List<OrderTicketSaveData> Tickets = new();
    }

    [Serializable]
    public class OrderTicketSaveData
    {
        public string         RecipeId;
        public OrderWorkPhase WorkPhase;
    }

    [Serializable]
    public class StaffSaveData
    {
        public List<StaffMemberSaveData> Members = new();
    }

    [Serializable]
    public class StaffMemberSaveData
    {
        public AgentRole Role;
        public Vector3   LastPosition;
    }
}
