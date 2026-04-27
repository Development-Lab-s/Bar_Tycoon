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
        
        private void OnEnable()
        {
            _inputSo.IsClick += IsCheckInteract;
        }
        private void OnDisable()
        {
            _inputSo.IsClick -= IsCheckInteract;
        }
        public void IsCheckInteract()
        {
            if (_inputSo.MainCam == null) return;

            Ray ray = _inputSo.MainCam.ScreenPointToRay(_inputSo.MousePosition);

            RaycastHit2D hit = Physics2D.Raycast(ray.origin, Vector2.zero, 0f, whatisPlayer);

            if (hit.collider == null) return;

            if (hit.collider.TryGetComponent<AbstructContractObject>(out var target))
            {
                target.ExcuteClick();
            }

        }
    }
}