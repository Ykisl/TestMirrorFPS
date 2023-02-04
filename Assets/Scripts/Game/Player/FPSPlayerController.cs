using Game.Weapon;
using Mirror;
using System;
using UnityEngine;

namespace Game.Player
{
    public class FPSPlayerController : NetworkBehaviour, IHitable
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform playerView;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private WeaponController weapon;
        [Space]
        [Header("Camera config values")]
        [SerializeField] private float mouseSpeed = 450f;
        [SerializeField] private float maxVerticalAngle = 90f;
        [SerializeField] private float minVerticalAngle = -90f;
        [Header("Moving config values")]
        [SerializeField][SyncVar] private float moveSpeed = 10f;
        [Header("Invisible config values")]
        [SerializeField] [SyncVar] private float invisibleTime = 3f;

        protected Transform _transform;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private float _verticalCameraAngle = 0;

        [SyncVar] private bool _isPlayerInputEnabled = true;

        [SyncVar] private bool _isPlayerInvisible;
        [SyncVar] private bool _isInvisibleTimerActive;
        [SyncVar] private float _invisibleTimer;

        public event Action<FPSPlayerController> OnPlayerHit; 

        private void Start()
        {
            _transform = transform;

            _startPosition = _transform.position;
            _startRotation = _transform.rotation;

            characterController.enabled = isLocalPlayer;
            playerCamera.gameObject.SetActive(isLocalPlayer);
        }

        [ClientRpc]
        public void RpcRespawnPlayer()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            characterController.enabled = false;

            _transform.position = _startPosition;
            _transform.rotation = _startRotation;

            characterController.enabled = true;
        }

        [ClientRpc]
        public void RpcSetIsPlayerInputEnabled(bool isInputEnabled)
        {
            _isPlayerInputEnabled = isInputEnabled;
        }

        public override void OnStartLocalPlayer()
        {
            SetIsCursorLocked(true);
        }

        public override void OnStopLocalPlayer()
        {
            SetIsCursorLocked(false);
        }

        private void Update()
        {
            var delta = Time.deltaTime;

            if (isLocalPlayer && _isPlayerInputEnabled)
            {
                UpdateLocalPlayer(delta);
            }

            if (isServer)
            {
                UpdateInvisibleTimer(delta);
            }
        }

        private void UpdateLocalPlayer(float delta)
        {
            UpdatePlayerCamera(delta);
            UpdatePlayerMovement(delta);
            UpdateWeaponInput(delta);
        }

        #region PlayerCamera

        private void UpdatePlayerCamera(float delta)
        {
            var mouseY = Input.GetAxis("Mouse Y");
            var mouseX = Input.GetAxis("Mouse X");

            var cameraRotateDirection = mouseX * Vector3.up * delta * mouseSpeed;
            _transform.Rotate(cameraRotateDirection);

            UpdateVerticalCameraAngle(delta, mouseY);
        }

        private void UpdateVerticalCameraAngle(float delta, float verticalMouseAxis)
        {
            var verticalMouseSpeed = verticalMouseAxis * mouseSpeed * delta;
            _verticalCameraAngle -= verticalMouseSpeed;

            _verticalCameraAngle = Mathf.Clamp(_verticalCameraAngle, minVerticalAngle, maxVerticalAngle);

            var verticalCameraVector = new Vector3(_verticalCameraAngle, 0, 0);
            playerView.localRotation = Quaternion.Euler(verticalCameraVector);
        }

        #endregion

        #region PlayerMovement
        private void UpdatePlayerMovement(float delta)
        {
            var verticalAxis = Input.GetAxis("Vertical");
            var horizontalAxis = Input.GetAxis("Horizontal");

            var directionVector = transform.right * horizontalAxis + transform.forward * verticalAxis;
            var moveVector = directionVector * moveSpeed * delta;

            characterController.Move(moveVector);
        }

        #endregion

        #region Weapon

        private void UpdateWeaponInput(float delta)
        {
            if(!Input.GetMouseButtonDown(0))
            {
                return;
            }

            CmdShootWeapon();
        }

        [Command]
        private void CmdShootWeapon()
        {
            if(weapon == null)
            {
                return;
            }

            if (!weapon.TryShoot())
            {
                Debug.Log("Неудачное поподание");
                return;
            }

            OnPlayerHit?.Invoke(this);
        }

        public bool TryHit(GameObject sender)
        {
            if (_isPlayerInvisible)
            {
                return false;
            }

            StartInvisibleTimer();
            Debug.Log($"Поподание по объекту {gameObject.name} от {sender.name}");
            return true;
        }

        #endregion

        #region Invisible

        public void SetIsInvisible(bool isInvisible)
        {
            _isPlayerInvisible = isInvisible;
            Debug.Log($"{name} состояние неуязвимости {_isPlayerInvisible}");
        }

        private void StartInvisibleTimer()
        {
            _invisibleTimer = 0;

            SetIsInvisible(true);
            _isInvisibleTimerActive = true;
        }

        private void StopInvisibleTimer()
        {
            _isInvisibleTimerActive = false;
        }

        private void UpdateInvisibleTimer(float delta)
        {
            if (!_isInvisibleTimerActive)
            {
                return;
            }

            _invisibleTimer += delta;
            if(_invisibleTimer >= invisibleTime)
            {
                FinishInvisibleTimer();
            }
        }

        private void FinishInvisibleTimer()
        {
            StopInvisibleTimer();
            SetIsInvisible(false);
        }

        #endregion

        private void SetIsCursorLocked(bool isCursorLocked)
        {
            var cursorState = isCursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
            //Cursor.lockState = cursorState;
        }
    }
}
