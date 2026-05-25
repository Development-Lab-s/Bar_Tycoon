using _00._Work._Resources._02._Scripts.Modules;
using Cysharp.Threading.Tasks.Triggers;
using System;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Modules
{
    public class SeatModule : MonoBehaviour, IModule
    {
        private ModuleOwner _owner;
        private OccupationSlot? _slot;

        [SerializeField] private Transform seatPos;
        [SerializeField] private float _facingDirection = 1f;
        public float FacingDirection => _facingDirection;

        private Vector3 prevPos;
        private Transform customer;
        public ModuleOwner AssignedAgent { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            var tycoonObject = owner as TycoonObject;
            _facingDirection *= (tycoonObject.FlipX ? 1f : -1f);
            _owner = owner;
        }

        public void AssignCustomer(ModuleOwner customer)
        {
            AssignedAgent = customer;
            _slot = null;
        }

        public void Seat(ModuleOwner customer)
        {
            prevPos = customer.transform.position;
            customer.transform.position = seatPos.transform.position;
            this.customer = customer.transform;
        }
        public void AssignWithSlot(OccupationSlot slot, ModuleOwner customer)
        {
            _slot = slot;
            AssignedAgent = customer;
        }

        public void ClearCustomer()
        {
            AssignedAgent = null;
            _slot = null;
        }

        public void UnSeat()
        {
            customer.position = prevPos;
        }
    }
}
