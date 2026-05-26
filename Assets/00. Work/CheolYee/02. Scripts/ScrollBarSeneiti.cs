using System;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.CheolYee._02._Scripts
{
    public class ScrollBarSeneiti : MonoBehaviour
    {
        [SerializeField] ScrollRect scrollbar;

        private void Start()
        {
            if (scrollbar != null)
            {
                scrollbar.scrollSensitivity = 12;
            }
        }
    }
}