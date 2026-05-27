using System;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Core.Sorting
{
    public class CharSortTrigger : MonoBehaviour
    {
        private const int changeSortOffset = 498;
        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log(other.name);

            if (other.CompareTag("SortingTarget"))
            {
                var sortings = other.transform.parent.GetComponentsInChildren<DepthSortingByPoint>(true);
                foreach (var sorting in sortings)
                {
                    if (sorting.SortingOffset >= changeSortOffset)
                        sorting.AddSortOffset(-changeSortOffset);
                    else
                        sorting.AddSortOffset(changeSortOffset);
                }
            }
        }
    }
}