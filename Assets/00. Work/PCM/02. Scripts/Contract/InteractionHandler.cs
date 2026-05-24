using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.PCM._02._Scripts;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using Systems;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets._00._Work.PCM._02._Scripts
{
    public class InteractionHandler :
        MonoBehaviour,
        IModule
    {
        [Header("Input")]
        [SerializeField] private PlayerInputSO _inputSo;
        [Header("Interact Layer")]
        [SerializeField]
        private LayerMask whatisPlayer;

        private ModuleOwner _owner;

        private AbstructContractObject _currentHover;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;

            _inputSo.IsClick += IsCheckInteract;
        }

        private void Update()
        {
            CheckHover();
        }

        private void OnDisable()
        {
            if (_inputSo != null)
            {
                _inputSo.IsClick -= IsCheckInteract;
            }
        }

        #region Hover

        private void CheckHover()
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;
            if (_inputSo.MainCam == null)
                return;
            Vector2 worldPos = _inputSo.MainCam.ScreenToWorldPoint(_inputSo.MousePosition);
            RaycastHit2D[] hits =Physics2D.RaycastAll(worldPos,Vector2.zero,0f,whatisPlayer);
            float closestDistance =float.MaxValue;
            AbstructContractObject closestTarget =null;

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null)
                    continue;
                AbstructContractObject target =hit.collider.GetComponentInParent<AbstructContractObject>();
                if (target == null)
                    continue;
                float distance =Vector2.Distance(_inputSo.MainCam.transform.position,target.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }
            if (_currentHover != closestTarget)
            {
                _currentHover?.UnHover();
                _currentHover = closestTarget;
                _currentHover?.Hover();
            }
        }
        #endregion

        #region Click
        private void IsCheckInteract()
        {
            _currentHover?.ExcuteClick();
        }

        #endregion
    }
}