using _00._Work._Resources._02._Scripts.Modules;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts._TileChange
{
    public class LP : MonoBehaviour, ILP
    {
        [SerializeField] private EventChannelSO EventChannel;
        [SerializeField] private Ease easeType;
        private RectTransform rect;
        public BgmSounds sound { get; set; }

        private MotionHandle moveMotion;
        private MotionHandle rotateMotion;
        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
        }
        public void Active()
        {
            // 기존에 실행 중인 동작이 있다면 안전하게 취소
            StopExistingMotions();

            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;

            // 1. 이동 애니메이션
            moveMotion = LMotion.Create(0f, rect.rect.size.x * 0.75f, 0.5f)
                .WithEase(easeType).WithOnComplete
                (() =>
                {
                    EventChannel.RaiseEvent(new PlaySoundEvent(sound, Vector3.zero, SoundChannelId.Bgm));
                })
                .Bind(x =>
                {
                    rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
                });
            rotateMotion = LMotion.Create(0f, -360f, 2f)
                .WithLoops(-1, LoopType.Restart)
                .Bind(angle =>
                {
                    rect.localRotation = Quaternion.Euler(0, 0, angle);
                });
        }

        public void Stop()
        {
            StopExistingMotions();
            EventChannel.RaiseEvent(new StopSoundEvent(SoundChannelId.Bgm));

            float currentZ = rect.localRotation.eulerAngles.z;
            if (currentZ > 180f) currentZ -= 360f;

            rotateMotion = LMotion.Create(rect.localEulerAngles.z, 0f, 2f)
            .WithEase(easeType)
            .Bind(angle =>
            {
                rect.localRotation = Quaternion.Euler(0, 0, angle);
            });
            moveMotion = LMotion.Create(rect.anchoredPosition.x, 0f, 0.5f)
            .WithEase(easeType)
            .Bind(x =>
            {
                rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            });
        }

        private void StopExistingMotions()
        {
            if (moveMotion.IsActive()) moveMotion.Cancel();
            if (rotateMotion.IsActive()) rotateMotion.Cancel();
        }


    }
}