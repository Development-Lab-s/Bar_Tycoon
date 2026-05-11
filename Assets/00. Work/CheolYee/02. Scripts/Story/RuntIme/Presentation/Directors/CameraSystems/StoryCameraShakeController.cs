using System.Reflection;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Unity.Cinemachine;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.CameraSystems
{
    public sealed class StoryCameraShakeController : MonoBehaviour
    {
        [Header("Impulse")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("Default Direction")]
        [SerializeField] private bool randomizeDirection = true;
        [SerializeField] private Vector2 fallbackDirection = Vector2.right;

        private bool _loggedMissingImpulseSource;
        private bool _loggedUnsupportedTargetMode;

        public void Trigger(StoryActorKeyframeData key)
        {
            if (key == null)
                return;

            if (key.cameraShakeTargetMode != StoryCameraShakeTargetMode.All)
            {
                if (!_loggedUnsupportedTargetMode)
                {
                    Debug.LogWarning(
                        $"[{nameof(StoryCameraShakeController)}] Only CameraShake target mode 'All' is implemented right now.",
                        this);

                    _loggedUnsupportedTargetMode = true;
                }

                return;
            }

            CinemachineImpulseSource source = ResolveImpulseSource();
            if (source == null)
            {
                if (!_loggedMissingImpulseSource)
                {
                    Debug.LogWarning(
                        $"[{nameof(StoryCameraShakeController)}] Camera shake requested but no CinemachineImpulseSource is assigned.",
                        this);

                    _loggedMissingImpulseSource = true;
                }

                return;
            }

            float strength = Mathf.Max(0f, key.cameraShakeStrength);
            if (strength <= 0f)
                return;

            float duration = Mathf.Max(0.01f, key.cameraShakeDuration);
            float frequency = Mathf.Max(0f, key.cameraShakeFrequency);

            Vector2 dir2 = ResolveDirection();
            Vector3 velocity = new Vector3(dir2.x, dir2.y, 0f).normalized * strength;

            source.DefaultVelocity = velocity;

            CinemachineImpulseDefinition definition = source.ImpulseDefinition;
            definition.ImpulseDuration = duration;
            TryApplyImpulseFrequency(ref definition, frequency);
            source.ImpulseDefinition = definition;

            source.GenerateImpulseWithForce(strength);
        }

        private CinemachineImpulseSource ResolveImpulseSource()
        {
            if (impulseSource != null)
                return impulseSource;

            impulseSource = GetComponent<CinemachineImpulseSource>();
            return impulseSource;
        }

        private Vector2 ResolveDirection()
        {
            if (randomizeDirection)
            {
                Vector2 random = Random.insideUnitCircle;
                if (random.sqrMagnitude > 0.0001f)
                    return random.normalized;
            }

            if (fallbackDirection.sqrMagnitude > 0.0001f)
                return fallbackDirection.normalized;

            return Vector2.right;
        }

        private static void TryApplyImpulseFrequency(ref CinemachineImpulseDefinition definition, float frequency)
        {
            if (frequency <= 0f)
                return;

            PropertyInfo prop = typeof(CinemachineImpulseDefinition).GetProperty("FrequencyGain");
            if (prop == null || prop.PropertyType != typeof(float) || !prop.CanWrite)
                return;

            object boxed = definition;
            prop.SetValue(boxed, frequency);
            definition = (CinemachineImpulseDefinition)boxed;
        }
    }
}