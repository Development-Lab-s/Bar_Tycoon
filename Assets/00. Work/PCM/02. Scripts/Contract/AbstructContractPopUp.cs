using _00._Work._Resources._02._Scripts.Modules;
using LitMotion;
using LitMotion.Extensions;
using System.Collections;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts.Contract
{
    public abstract class AbstructContractPopUp : MonoBehaviour, IAbstructContractPopUp
    {
        [SerializeField] protected float animDuration = 0.1f;
        [SerializeField] protected float waitDuration = 2.0f; // 자동 종료 시 대기 시간
        [SerializeField] protected Ease easeType = Ease.OutBack;

        private ModuleOwner _owner;
        private Vector3 _originScale;
        private Coroutine _timerCoroutine;
        private MotionHandle _motionHandle;
        public virtual void Initialize(ModuleOwner owner)
        {
            _owner = owner; 
        }
        public virtual void AfterInit()
        {
            _originScale = transform.localScale;
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }
        public virtual void Open(bool isAutoClose = false)
        {
            if (_motionHandle.IsActive()) _motionHandle.Cancel();
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);

            gameObject.SetActive(true);
            AnimateScale(true, isAutoClose); //true면 몇 초 뒤 닫힘
        }

        // 외부에서 버튼 등으로 직접 닫고 싶을 때 호출
        public virtual void Close()
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            AnimateScale(false);
        }

        private void AnimateScale(bool isAppearing, bool isAutoClose = false)
        {
            if (_motionHandle.IsActive()) _motionHandle.Cancel();

            Vector3 start = isAppearing ? Vector3.zero : transform.localScale;
            Vector3 end = isAppearing ? _originScale : Vector3.zero;

            _motionHandle = LMotion.Create(start, end, animDuration)
                .WithEase(easeType)
                .WithOnComplete(() =>
                {
                    if (isAppearing)
                    {
                        if (isAutoClose)
                        {
                            _timerCoroutine = StartCoroutine(WaitAndClose());
                        }
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                })
                .BindToLocalScale(transform);
        }

        private IEnumerator WaitAndClose()
        {
            yield return new WaitForSeconds(waitDuration);
            Close();
        }

        public virtual void OnDisable()
        {
            if (_motionHandle.IsActive())
            {
                _motionHandle.Cancel();
            }
        }


    }
}