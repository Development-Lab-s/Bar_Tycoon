using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.Util
{
    /// <summary>
    /// Small helper for runtime component lookup patterns used by story stage components.
    ///
    /// It keeps GetComponent / GetComponentInParent / AddComponent fallback logic out of
    /// feature classes so directors can focus on stage, actor, background, and camera logic.
    /// </summary>
    public static class StoryRuntimeComponentResolver
    {
        public static T GetOnSelfOrAdd<T>(MonoBehaviour owner, ref T cached)
            where T : Component
        {
            if (cached != null)
                return cached;

            if (owner == null)
                return null;

            cached = owner.GetComponent<T>();
            if (cached != null)
                return cached;

            Debug.LogWarning(
                $"[{owner.GetType().Name}] {typeof(T).Name} is missing. Adding a runtime component.",
                owner);

            cached = owner.gameObject.AddComponent<T>();
            return cached;
        }

        public static T GetInSelfOrParent<T>(MonoBehaviour owner, ref T cached)
            where T : Component
        {
            if (cached != null)
                return cached;

            if (owner == null)
                return null;

            cached = owner.GetComponent<T>();
            if (cached != null)
                return cached;

            cached = owner.GetComponentInParent<T>();
            return cached;
        }

        public static T GetInSelfOrParentOrAdd<T>(MonoBehaviour owner, ref T cached)
            where T : Component
        {
            T resolved = GetInSelfOrParent(owner, ref cached);
            if (resolved != null)
                return resolved;

            if (owner == null)
                return null;

            Debug.LogWarning(
                $"[{owner.GetType().Name}] {typeof(T).Name} is missing. Adding a runtime component.",
                owner);

            cached = owner.gameObject.AddComponent<T>();
            return cached;
        }

        public static T GetInSelfOrParentWithWarning<T>(
            MonoBehaviour owner,
            ref T cached,
            ref bool loggedMissing,
            string message)
            where T : Component
        {
            T resolved = GetInSelfOrParent(owner, ref cached);
            if (resolved != null)
                return resolved;

            if (!loggedMissing && owner != null)
            {
                Debug.LogWarning(
                    string.IsNullOrWhiteSpace(message)
                        ? $"[{owner.GetType().Name}] {typeof(T).Name} is missing."
                        : message,
                    owner);

                loggedMissing = true;
            }

            return null;
        }
    }
}
