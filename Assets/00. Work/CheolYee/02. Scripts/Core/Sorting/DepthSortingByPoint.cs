using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Core.Sorting
{
    [AddComponentMenu("Bar Tycoon/Depth Sorting By Point")]
    public class DepthSortingByPoint : MonoBehaviour
    {
        public enum UpdateMode { Once, LateUpdate }

        [SerializeField] Transform sortPoint;
        [SerializeField] Renderer[] targetRenderers;
        [SerializeField] Vector3 sortAxis = new(0f, 0.1f, 0f);
        [SerializeField] int precision = 100;
        [SerializeField] int sortingOffset = 0;
        [SerializeField] UpdateMode updateMode = UpdateMode.Once;

        public Transform SortPoint => sortPoint;
        public int SortingOffset => sortingOffset;
        
        void Reset() => AutoSetup();

        void OnValidate()
        {
            AutoSetup();
            if (updateMode == UpdateMode.Once) Apply();
        }

        void Start()
        {
            if (updateMode == UpdateMode.Once) Apply();
        }

        void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate) Apply();
        }

        // Public so the Editor "Apply Now" button and external callers can trigger it
        public void Apply()
        {
            if (sortPoint == null || targetRenderers == null) return;
            int order = ComputeOrder();
            foreach (var r in targetRenderers)
                if (r != null) r.sortingOrder = order;
        }
        
        public void AddSortOffset(int offset)
        {
            sortingOffset += offset;
            Apply();
        }

        public int ComputeOrder()
        {
            if (sortPoint == null) return sortingOffset;
            return Mathf.RoundToInt(-Vector3.Dot(sortPoint.position, sortAxis.normalized) * precision) + sortingOffset;
        }

        void AutoSetup()
        {
            if (sortPoint == null)
                sortPoint = transform.Find("SortPoint");
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        /*void OnDrawGizmosSelected()
        {
            if (sortPoint == null) return;
            Gizmos.color = new Color(1f, 0.85f, 0f);
            Gizmos.DrawWireSphere(sortPoint.position, 0.06f);
            Gizmos.DrawLine(transform.position, sortPoint.position);
        }*/
    }
}
