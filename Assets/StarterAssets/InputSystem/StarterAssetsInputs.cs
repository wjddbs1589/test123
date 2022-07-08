using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

        public bool normalAttack;
        public bool powerAttack;

        [Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		Animator anim;
		GameObject attackRange;
		private void Awake()
		{
			attackRange = transform.Find("AttackRange").gameObject; // AttackRange라는 게임 오브젝트를 찾아서 가져옴
			anim = GetComponent<Animator>();    
        }

		//----------------------------------------------------
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}
		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
		public void OnNormalAttack(InputValue value)
        {
			NormalAttackInput(value.isPressed);
        }
		public void OnPowerAttack(InputValue value)
		{
			PowerAttackInput(value.isPressed);
		}

		//----------------------------------------------------
#endif
		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}
        public void NormalAttackInput(bool newNormalAttack)
        {			
			anim.SetTrigger("NormalAttack");
            normalAttack = newNormalAttack;
        }
        public void PowerAttackInput(bool newPowerAttack)
        {
			anim.SetTrigger("PowerAttack");
			powerAttack = newPowerAttack;
        }
		//----------------------------------------------------

		// 피격 효과를 위한 이벤트 효과 추가
		// 애니메이션에 이벤트 추가후 일정 프레임에서 공격 범위 활성화
		public void OnAttackRange()
        {
			attackRange.SetActive(true);
        }

		//----------------------------------------------------

        private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}	
}