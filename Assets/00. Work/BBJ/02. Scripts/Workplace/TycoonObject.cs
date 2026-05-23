using _00._Work._Resources._02._Scripts.Modules;
using BBJ.GridSystem.Objects;
using System;
using UnityEngine;

namespace BBJ.WorkplaceSystem
{
    public class 
        TycoonObject : ModuleOwner
    {
        [SerializeField] protected TileSetData _tileSetData;
        [SerializeField] protected bool        _flipX;
        public TileSetData TileSetData => _tileSetData;
        public bool        FlipX       => _flipX;


        public virtual void Setup(Func<Vector2Int, Vector3> offsetToWorld, bool flipX)
        {
            _flipX = flipX;
            if (flipX)
                transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }
}
