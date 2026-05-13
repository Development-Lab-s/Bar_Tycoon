using _00._Work._Resources._02._Scripts.Modules;
using LitMotion;
using LitMotion.Extensions;
using Spine.Unity;
using System.Collections;
using System.Net.NetworkInformation;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts.Contract
{
    public enum Uistate
    {
        None,Open, Close 
    }
    public abstract class AbstructContractPopUp : MonoBehaviour, IAbstructContractPopUp , IModule, IAfterInitModule
    {
        [SerializeField] protected float animDuration = 0.1f;
        [SerializeField] protected float waitDuration = 2.0f; // 자동 종료 시 대기 시간
        [SerializeField] protected Ease easeType = Ease.OutBack;
        public bool IsAnimating => _motionHandle.IsActive();

        private ModuleOwner _owner;
        private Vector3 _originScale;
        private Coroutine _timerCoroutine;
        private MotionHandle _motionHandle;
        private UiOwner _uiOwner;
        public bool isOpen { get; set; }

        public virtual void Initialize(ModuleOwner owner)
        {
            _owner = owner; 
        }
        public virtual void AfterInit()
        {
            _uiOwner = _owner.GetModule<UiOwner>();
            _originScale = transform.localScale;
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }
        public void Open(bool isAutoClose = false)
        {

            if (isOpen || IsAnimating) return;

            isOpen = true;
            _uiOwner.StackAdd(this);

            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            if (_motionHandle.IsActive()) _motionHandle.Cancel();

            if (!gameObject.activeSelf)
            {
                OnOpen();
                gameObject.SetActive(true);
            }

            AnimateScale(true, isAutoClose);
        }
        public abstract void OnOpen();

        public virtual void Close()
        {
            if (!isOpen ||IsAnimating) return;

            isOpen = false;

            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            if (_motionHandle.IsActive()) _motionHandle.Cancel();

            AnimateScale(false);
        }

        private void AnimateScale(bool isAppearing, bool isAutoClose = false)
        {
            if (_motionHandle.IsActive()) _motionHandle.Cancel();

            Vector3 start = transform.localScale; 
            Vector3 end = isAppearing ? _originScale : Vector3.zero;

            Ease currentEase = isAppearing ? easeType : Ease.InBack;

            _motionHandle = LMotion.Create(start, end, animDuration)
                .WithEase(currentEase)
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
                        transform.localScale = Vector3.zero;
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