using _00._Work._Resources._02._Scripts.Modules;
using Gamelib.SoundSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts._TileChange
{
    public class LpController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        private int _id = 0;
        public int Id
        { 
            get
            {
                return _id;
            }
            set
            {
                value = Mathf.Clamp(value, 0, Enum.GetValues(typeof(BgmSounds)).Length-1);
                _id = value;
                SoundChange(_id);
                NameChange(_id);
            }
        }
        private void OnDisable()
        {
            Debug.Log($"[LpController] Disable 발생! 호출 스택: {Environment.StackTrace}");
        }
        private LP lp;
        private void Awake()
        {
            lp = GetComponentInChildren<LP>();
            lp.Active();
            SoundChange(0);
            NameChange(0);
        }
        private void SoundChange(int id)
        {
            lp.PlaySound((BgmSounds)id);
        }
        private void NameChange(int id)
        {
           text.text = lp.NameChosing(id);
        }
        public void idPlus() => Id++;
        public void idMinus() => Id--;
   
    }
}