using _00._Work.PCM._02._Scripts;
using System.Collections;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts
{
    public class TestContractObject : AbstructContractObject
    {
        public GameObject TestPanel;

        public override void EventRegister()
        {
            if (!TestPanel.activeSelf)
                TestPanel.SetActive(true);
            else
                TestPanel.SetActive(false);
        }
    }
}