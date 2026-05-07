using BBJ.WorkplaceSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BBJ.Register
{
    [CreateAssetMenu(fileName = "WorkplaceRegister", menuName = "Register/WorkplaceRegister")]
    public class WorkplaceRegisterSO : RegisterSO<Workplace>
    {
        public List<Workplace> GetCandidates(Vector3 from, int maxCount = 3)
        {
            return Registry
                .Where(w => w.IsAvailable)
                .OrderBy(w => Vector3.Distance(from, w.transform.position))
                .Take(maxCount)
                .ToList();
        }

        public List<Workplace> GetCandidates(Vector3 from, WorkplaceTypeSO type, int maxCount = 5)
        {
            return GetCandidates(from, maxCount)
                .Where(w => w.WorkplaceType == type)
                .ToList();
        }

        public List<Workplace> GetAll(WorkplaceTypeSO type)
        {
            return Registry
                .Where(w => w.WorkplaceType == type)
                .ToList();
        }
        public Workplace GetFirst(WorkplaceTypeSO type)
        {
            return Registry
                .First(w => w.WorkplaceType == type);
        }

    }
}
