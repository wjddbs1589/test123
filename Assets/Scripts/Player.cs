using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    private bool isJumping = false;
    public float jumpPower = 8.0f;

    bool canRun = false;

    PlayerInput actions = null;
    Rigidbody rigid = null;
    Animator anim = null;

    Vector3 dir = Vector3.zero;
    Vector3 moveDirection = Vector3.zero;
    //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void FixedUpdate()
    {
        Move();
        if (Mathf.Abs(rigid.velocity.y) > 0.01f)
        {
            isJumping = true;
        }
    }
    private void Awake()
    {
        actions = new();
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }
    //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    // 플레이어 인풋으로 적용한 기능
    //private void OnEnable()
    //{
    //    actions.Player.Enable();
    //    actions.Player.Move.performed += OnMove;
    //    actions.Player.Run.performed += OnRun;
    //    actions.Player.NormalAttack.performed += OnNormalAttack;
    //    actions.Player.StrongAttack.performed += OnStrongAttack;
    //    actions.Player.Jump.performed += OnJump;

    //}
    //private void OnDisable()
    //{
    //    actions.Player.Jump.performed -= OnJump;
    //    actions.Player.NormalAttack.performed -= OnStrongAttack;
    //    actions.Player.StrongAttack.performed -= OnNormalAttack;
    //    actions.Player.Run.performed -= OnRun;
    //    actions.Player.Move.performed -= OnMove;
    //    actions.Player.Disable();
    //}
    //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    // 이동 
    private void OnMove(InputAction.CallbackContext context)
    {
        dir = context.ReadValue<Vector2>();
        moveDirection = new Vector3(dir.x, 0.0f, dir.y).normalized; // 입력 받은 값을 정규화 해서 진행 방향 선택

        anim.SetBool("IsMove", !context.canceled);
    }
    private void Move()  // 누르는 방향으로 이동 + 방향을 바라봄 
    {
        Rotate();
        rigid.MovePosition(rigid.position + (moveDirection * moveSpeed * Time.fixedDeltaTime)); // 진행방향으로 이동        
    }
    private void Rotate() // 캐릭터 회전, 진행 방향으로 회전
    {
        transform.LookAt(transform.position + moveDirection);
    }
    private void OnRun(InputAction.CallbackContext context) // 달리기
    {
        moveSpeed = 15.0f;
        anim.SetBool("IsRun",canRun);
        //달리기 안끝나는 문제 해결 해야 함
    }
    //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    // 점프
    private void OnJump(InputAction.CallbackContext context)
    {
        if (!isJumping)
        {
            rigid.AddForce(transform.up * jumpPower, ForceMode.Impulse);
        }
    }
    //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    // 공격
    private void OnNormalAttack(InputAction.CallbackContext context)
    {
        Debug.Log("약한공격 실행");
    }
    private void OnStrongAttack(InputAction.CallbackContext context)
    {
        Debug.Log("강한공격 실행");
    }
    //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    // 충돌 확인
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isJumping = false;
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isJumping = true;
        }
    }
    //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
}
