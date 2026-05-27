using System;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.CheolYee._02._Scripts
{
    public class ScrollRectSensi : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private float sensitivity = 12f;

        private void Awake()
        {
            if (scrollRect != null)
                scrollRect.scrollSensitivity = sensitivity;
        }

        private void Start()
        {
            if (scrollRect != null)
                scrollRect.scrollSensitivity = sensitivity;
        }

        private void Update()
        
        {
            if (!Mathf.Approximately(scrollRect.scrollSensitivity, sensitivity))
                scrollRect.scrollSensitivity = sensitivity;
        }
    }
}