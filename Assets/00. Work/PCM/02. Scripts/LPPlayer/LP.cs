using _00._Work._Resources._02._Scripts.Modules;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets._00._Work.PCM._02._Scripts._TileChange
{
    public class LP : MonoBehaviour, ILP
    {
        [SerializeField] protected EventChannelSO SoundEventChannel;
        [SerializeField] protected Ease easeType;
        [SerializeField] protected SoundListSo SoundList;
        public RectTransform rect;
        protected MotionHandle moveMotion;
        protected MotionHandle rotateMotion;
        public virtual void Awake()
        {
            rect = GetComponent<RectTransform>();
            ApplyPosition();
        }
        public virtual void ApplyPosition()
        {
            rect.anchoredPosition = new Vector2(200, 0);
        }
        public virtual void Active()
        {
            Debug.Log("선 실행");
            // 기존에 실행 중인 동작이 있다면 안전하게 취소
            StopExistingMotions();
            //rect.localRotation = Quaternion.identity;

            // 1. 이동 애니메이션
            
            
            rotateMotion = LMotion.Create(0f, -360f, 2f)
                .WithLoops(-1, LoopType.Restart)
                .Bind(angle =>
                {
                    rect.localRotation = Quaternion.Euler(0, 0, angle);
                });
        }
        public virtual void PlaySound(BgmSounds id) 
        {
            StartCoroutine(RestTime(id));

        }
        public IEnumerator RestTime(BgmSounds id)
        {
            SoundEventChannel.RaiseEvent(new StopSoundEvent(SoundChannelId.Bgm));
            yield return new WaitForSeconds(1f);
            SoundEventChannel.RaiseEvent(new PlaySoundEvent(id, Vector3.zero, SoundChannelId.Bgm));
        }
        public virtual string NameChosing(int id)
        {
           return SoundList.sounds[id].name;
        }
        public virtual void Stop()
        {
            StopExistingMotions();

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

        public virtual void StopExistingMotions()
        {
            if (moveMotion.IsActive()) moveMotion.Cancel();
            if (rotateMotion.IsActive()) rotateMotion.Cancel();
        }


    }
}