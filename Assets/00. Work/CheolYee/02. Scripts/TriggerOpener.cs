using System;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts
{
    public class TriggerOpener : MonoBehaviour
    {
        [SerializeField] private GameObject triggerTarget;

        private void Awake()
        {
            triggerTarget.SetActive(false);
        }

        private void Start()
        {
            triggerTarget.SetActive(true);
        }
    }
}