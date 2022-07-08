using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

// 참고: 애니메이터 null 검사를 사용하여 캐릭터와 캡슐 모두에 대해 컨트롤러를 통해 애니메이션을 호출합니다.
namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("캐릭터의 이동속도(m/s)")]
        public float MoveSpeed = 2.0f;  // 이동속도

        [Tooltip("캐릭터의 달리기 속도(m/s)")]
        public float SprintSpeed = 5.335f; // 달리기 속도

        [Tooltip("바라보는 방향으로 회전하는 속도")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("가속/감속")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("플레이어가 점프할수 있는 높이")]
        public float JumpHeight = 1.2f;

        [Tooltip("캐릭터는 자체 중력값 사용. 엔진 기본값= -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("점프 쿨타임. 즉시 점프 하려면 0.0f으로 설정")]
        public float JumpTimeout = 0.50f;

        [Tooltip("떨어지고 있는 상태에 들어가는데 필요한 시간. 계단을 내려갈때 유용")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("캐릭터의 땅 착지 여부. 접지 점검에 내장된 캐릭터 컨트롤러의 일부가 아님")]
        public bool Grounded = true;

        [Tooltip("거친 땅에 유용하다")]
        public float GroundedOffset = -0.14f;

        [Tooltip("접지 확인의 반경. 캐릭터 컨트롤러의 반지름과 일치해야 함.")]
        public float GroundedRadius = 0.28f;

        [Tooltip("캐릭터가 그라운드로 사용하는 레이어")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("카메라가 따라갈 Cinemachine 가상 카메라에 설정된 추적 대상")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("카메라 최대 상승 높이")]
        public float TopClamp = 70.0f;

        [Tooltip("카메라 최대 하상 높이")]
        public float BottomClamp = -30.0f;

        [Tooltip("카메라를 오버라이드 할 수 있는 추가 단계. 잠금시 카메라 위치를 미세 조정하는데 유용")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("모든 축의 카메라 위치 잠금")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // 플레이어
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // 타임아웃, 시간
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // 애니메이션 아이디들
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDNormalAttack;
        private int _animIDPowerAttack;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        private GameObject attackRange;
        private void Awake()
        {
            attackRange = transform.Find("AttackRange").gameObject;
            // 메인 카메라 오브젝트 정보 가져옴
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator); // _animator가 있으면 _hasAnimator에 true반환
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // 시작시 타임아웃 재설정
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);
                       
            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        // 애니메이션 이름 할당
        private void AssignAnimationIDs() 
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

            _animIDNormalAttack = Animator.StringToHash("NormalAttack");
            _animIDPowerAttack = Animator.StringToHash("PowerAttack");
        }
        //---------------------------------------------------------

        //접지 확인
        private void GroundedCheck()
        {
            // set sphere 위치, 간격 띄우기
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // 문자를 사용하는 경우 애니메이터 업데이트 update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }
        //---------------------------------------------------------

        //카메라 회전
        private void CameraRotation()
        {
            // 입력이 있고, 카메라 위치가 고정되지 않았을 때
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                // 마우스 입력에 Time.deltaTime을 곱하지 마라
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // 회전값이 360도 제한되도록 회전을 고정 시킴. 
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine이 타겟을 따라가게 함
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler( _cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }
        //---------------------------------------------------------

        // 캐릭터 이동
        private void Move()
        {
            // 이동 속도, 스프린트 속도 및 스프린트가 눌린 경우에 따라 목표 속도를 설정합니다. 
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // 제거, 교체 또는 반복하기 쉽도록 설계된 단순한 가속 및 감속. 
            // 참고: Vector2의 == 연산자는 근사치를 사용하므로 부동소수점 오류가 발생하기 쉽고 크기보다 저렴합니다.
            // 입력이 없으면 목표 속도를 0으로 설정합니다.
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // 플레이어의 현재 수평 속도에 대한 참조
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // 목표 속도에 따라 가속 또는 감속
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // 보다 유기적인 속도 변화를 주는 선형적인 결과보다는 곡선적인 결과를 생성한다.
                // note T in Lerp는 클램핑되어 있으므로 속도를 클램핑할 필요가 없음.
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // 소수점 이하 세 자리까지의 왕복 속도
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // 입력 방향 정규화
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // 참고: Vector2의 != 연산자는 근사치를 사용하므로 부동소수점 오류가 발생하기 쉽고 크기보다 저렴합니다.
            // 플레이어가 이동할 때 이동 입력이 있으면 플레이어를 회전시킵니다.
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // 카메라 위치를 기준으로 얼굴 입력 방향으로 회전합니다.
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // 플레이어 이동
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // 캐릭터를 사용하는 경우 애니메이터 업데이트
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }
        //---------------------------------------------------------

        // 점프 및 중력
        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // 추락 타이머 리셋
                _fallTimeoutDelta = FallTimeout;

                // 캐릭터를 사용하는 경우 애니메이터 업데이트
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // 정지했을 때 속도가 무한히 떨어지는 것을 막는다.
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // 점프
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // H * -2 * G의 제곱근 = 원하는 높이에 도달하는 데 필요한 속도
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // 캐릭터를 사용하는 경우 애니메이터 업데이트
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // 점프 타임아웃
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // 점프 시간 초과 타이머 재설정
                _jumpTimeoutDelta = JumpTimeout;

                // 추락 타임아웃
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // 캐릭터를 사용하는 경우 애니메이터 업데이트
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // 착륙 전에 점프 불가능
                _input.jump = false;
            }

            // 터미널 아래에 있는 경우 시간 경과에 따른 중력 적용(시간 경과에 따른 선형 속도 향상을 위해 델타 시간 2회 증가)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }
        //---------------------------------------------------------

        // 카메라 각도
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
        //---------------------------------------------------------

        // 접지 확인 기즈모, 점프 가능하면 녹색, 불가능이면 적색으로 표시
        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            //선택한 경우, 접지된 충돌기의 위치와 일치하는 반경에 Gizmo를 그립니다.
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }
        //---------------------------------------------------------

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}