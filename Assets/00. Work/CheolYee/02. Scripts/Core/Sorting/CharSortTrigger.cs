using System;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Core.Sorting
{
    public class CharSortTrigger : MonoBehaviour
    {
        [SerializeField] private int changeSortOffset = 500;
        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log(other.name);
            if (other.transform.parent.TryGetComponent(out DepthSortingByPoint sorting))
            {
                if (sorting.SortingOffset >= changeSortOffset)
                    sorting.AddSortOffset(-changeSortOffset);
                else
                    sorting.AddSortOffset(changeSortOffset);
            }
        }
    }
}