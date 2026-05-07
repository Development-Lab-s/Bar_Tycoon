using _00._Work._Resources._02._Scripts.Modules;
using BBJ.WorkplaceSystem;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Modules
{
    public class SeatModule : MonoBehaviour, IModule
    {
        private ModuleOwner _owner;
        private OccupationSlot? _slot;
        [SerializeField] private Transform seatPos;

        public ModuleOwner AssignedAgent { get; private set; }

        public void Initialize(ModuleOwner owner) => _owner = owner;

        public void AssignCustomer(ModuleOwner customer)
        {
            AssignedAgent = customer;
            _slot = null;
        }

        public void Seat(ModuleOwner customer)
        {
            customer.transform.position = seatPos.transform.position;
        }
        public void AssignWithSlot(OccupationSlot slot, ModuleOwner customer)
        {
            _slot         = slot;
            AssignedAgent = customer;
        }

        public void ClearCustomer()
        {
            AssignedAgent = null;
            _slot = null;
        }
    }
}
