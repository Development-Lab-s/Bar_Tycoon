using LitMotion;
using LitMotion.Extensions; // 확장 메서드 활용을 위해 추가
using System.Collections;
using UnityEngine;

public class ContractPopUI : MonoBehaviour
{
    [SerializeField] private float animDuration = 0.2f;
    [SerializeField] private float waitDuration = 2.0f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private Vector3 _originScale;
    private Coroutine _timerCoroutine;
    private MotionHandle _motionHandle; // 현재 실행 중인 모션 관리용

    private void Awake()
    {
        _originScale = transform.localScale;
        gameObject.SetActive(false);
    }

    public void EventRegister()
    {
        if (_motionHandle.IsActive()) _motionHandle.Cancel();

        gameObject.SetActive(true);
        FadeOut(true);
    }

    private IEnumerator WaitAndClose()
    {
        yield return new WaitForSeconds(waitDuration);
        FadeOut(false);
    }

    private void FadeOut(bool isAppearing)
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
                   if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
                   _timerCoroutine = StartCoroutine(WaitAndClose());
               }
               else
               {
                   gameObject.SetActive(false);
               }
           })
           .BindToLocalScale(transform); // Bind 직접 구현 대신 확장 메서드 사용
    }

    private void OnDestroy()
    {
        if (_motionHandle.IsActive()) _motionHandle.Cancel();
    }
}