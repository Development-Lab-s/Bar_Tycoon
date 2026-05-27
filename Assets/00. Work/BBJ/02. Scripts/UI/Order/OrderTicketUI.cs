using BBJ.Order;
using BBJ.Work;
using LitMotion;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BBJ.UI.Order
{
    public class OrderTicketUI : MonoBehaviour
    {
        [SerializeField] private Image    _foodIcon;
        [SerializeField] private TMP_Text _foodName;
        [SerializeField] private TMP_Text _workPhase;
        [SerializeField] private TMP_Text _priceLabel;

        [SerializeField] private Color _incompleteColor = Color.white;
        [SerializeField] private Color _completeColor   = Color.green;
        [SerializeField] private float _waveAmplitude   = 3f;
        [SerializeField] private float _waveFrequency   = 2f;

        private OrderTicket         _ticket;
        private Action<OrderTicket> _onCancel;
        private MotionHandle        _waveHandle;

        private void OnEnable()
        {
            if (_ticket == null) return;
            Refresh();
            StartWave();
        }

        private void OnDisable()
        {
            if (_waveHandle.IsActive()) _waveHandle.Cancel();
        }

        public void Setup(OrderTicket ticket, Action<OrderTicket> onCancel)
        {
            _ticket   = ticket;
            _onCancel = onCancel;
            var ordered = ticket.Ordered;

            if (ordered != null)
            {
                _foodIcon.sprite = ordered.cocktailIcon;
                _foodName.text   = ordered.cocktailName ?? "-";
                _priceLabel.text = "가격 : " + ordered.price + " <sprite=\"cost\" index=0>";
            }
        }

        public void Refresh()
        {
            if (_ticket == null) return;

            if (_workPhase != null)
                _workPhase.text = "(" + WorkPhaseLabel(_ticket.WorkPhase) + "...)";

            if (_foodName != null)
                _foodName.color = _ticket.WorkPhase >= OrderWorkPhase.ReadyForServe
                    ? _completeColor
                    : _incompleteColor;
        }

        private void StartWave()
        {
            if (_workPhase == null) return;
            if (_waveHandle.IsActive()) _waveHandle.Cancel();

            float period = _waveFrequency > 0f ? 1f / _waveFrequency : 1f;
            _waveHandle = LMotion.Create(0f, Mathf.PI * 2f, period)
                .WithLoops(-1)
                .Bind(UpdateWave);
        }

        private void UpdateWave(float t)
        {
            if (_workPhase == null) return;
            _workPhase.ForceMeshUpdate();
            var textInfo = _workPhase.textInfo;
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;
                int   matIdx  = charInfo.materialReferenceIndex;
                int   vertIdx = charInfo.vertexIndex;
                var   verts   = textInfo.meshInfo[matIdx].vertices;
                float offset  = Mathf.Sin(t + i * Mathf.PI * 0.5f) * _waveAmplitude;
                for (int v = 0; v < 4; v++)
                    verts[vertIdx + v].y += offset;
            }
            _workPhase.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        private static string WorkPhaseLabel(OrderWorkPhase phase) => phase switch
        {
            OrderWorkPhase.PendingCook     => "",
            OrderWorkPhase.ReadyForServe   => "서빙 중",
            OrderWorkPhase.Eating          => "식사 중",
            OrderWorkPhase.ReadyForCashier => "계산 중",
            OrderWorkPhase.Done            => "완료",
            _                              => ""
        };
    }
}
