using _00._Work.PCM._02._Scripts;
using System.Collections;
using Systems;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts
{
    public class InteractionHandler : MonoBehaviour
    {
        [SerializeField] private PlayerInputSO _inputSo;
        [SerializeField] private LayerMask whatisPlayer;
        private Camera mainCam;
        public Camera MainCam
        {
            get
            {
                if (mainCam == null)
                    mainCam = Camera.main;
                return mainCam;
            }
        }
        private void OnEnable()
        {
            _inputSo.isClick += IsCheckInteract;
        }
        private void OnDisable()
        {
            _inputSo.isClick -= IsCheckInteract;
        }
        public void IsCheckInteract()
        {
            if (MainCam == null) return;
            Vector2 mousePos = MainCam.ScreenToWorldPoint(_inputSo.MousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 20f, whatisPlayer);
            if (hit.collider == null) return;
            if (hit.collider.TryGetComponent<AbstructContractObject>(out var target))
            {
                target.ExcuteClick();
            }

        }
    }
}