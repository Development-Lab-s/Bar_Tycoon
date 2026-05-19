using BBJ.Schedule;
using BBJ.Staff;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BBJ.Register
{
    [CreateAssetMenu(fileName = "ScheduleRegister", menuName = "Register/ScheduleRegister")]
    public class ScheduleRegisterSO : RegisterSO<ISchedulable>
    {
        public ISchedulable FindAvailable(AgentRole role)
        {
            var avaliable = Registry.FirstOrDefault(s => s.IsAvailableForWork && (s.Role & role) != 0);
            return avaliable;
        }

        public IEnumerable<ISchedulable> GetAllAvailable(AgentRole role)
            => Registry.Where(s => s.IsAvailableForWork && (s.Role & role) != 0);
    }
}
