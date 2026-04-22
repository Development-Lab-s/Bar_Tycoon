using BBJ.Tycoon;
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
            return Agents
                .Where(w => !w.IsOccupied)
                .OrderBy(w => Vector3.Distance(from, w.transform.position))
                .Take(maxCount)
                .ToList();
        }

        public List<Workplace> GetCandidates(Vector3 from, WorkplaceType type, int maxCount = 5)
        {
            return Agents
                .Where(w => !w.IsOccupied && w.WorkplaceType == type)
                .OrderBy(w => Vector3.Distance(from, w.transform.position))
                .Take(maxCount)
                .ToList();
        }

        public List<T> GetAll<T>(WorkplaceType type) where T : Workplace
        {
            return Agents
                .Where(w => w.WorkplaceType == type)
                .OfType<T>()
                .ToList();
        }
    }
}
