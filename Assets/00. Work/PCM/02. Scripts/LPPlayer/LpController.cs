using _00._Work._Resources._02._Scripts.Modules;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._00._Work.PCM._02._Scripts._TileChange
{
    public class LpController : MonoBehaviour , IModule
    {
        [SerializeField] private EventChannelSO _LPchannel;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField]private LpBoxImage _LpImage;
        [SerializeField] private Image Lpbox;
        private ModuleOwner _owner;
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
                imageChange(_id);
            }
        }

        private void imageChange(int id)
        {
            Lpbox.sprite = _LpImage.image[id];
        }

        private ILP lp;
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            lp = GetComponentInChildren<ILP>();
        }
        private void SoundChange(int id)
        {
            _LPchannel.RaiseEvent(new LpConncetEvent(id));
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