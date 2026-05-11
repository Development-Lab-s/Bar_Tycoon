using _00._Work._Resources._02._Scripts.Modules;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkDurationUI : MonoBehaviour, IModule
{
    [SerializeField] private Slider _duationSlider;
    [SerializeField] private TextMeshProUGUI _percentText;
    float targetValue;
    float currentValue;
    public void Initialize(ModuleOwner owner)
    {
        _duationSlider.minValue = 0;
        _duationSlider.maxValue = 1;

        Disable();
    }

    public void Active()
    {
        this.gameObject.SetActive(true);
        Refresh();
    }
    public void Disable() => this.gameObject.SetActive(false);
    public void SetPercent(float newTargetValue)
    {
        float p = Mathf.Clamp01(newTargetValue);
        _percentText.text = Mathf.RoundToInt(p * 100) + "%";
        _duationSlider.SetValueWithoutNotify(p);

        Refresh();
    }
    public void Refresh()
    {
        //Debug.Log($"{_curValue} / {_maxValue} = {p}");
    }

}
