using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackRange : MonoBehaviour
{
    // 게임 오브젝트 활성화시 실행
    private void OnEnable()  
    {
        StartCoroutine(disable());
    }

    // 트리거가 접촉했을때 접촉 대상의 태그가 Enemy이면 공격받은 대상의 컴포넌트에서 takeDamage(10);실행
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyAttackTest>().takeDamage(10);
        }
    }

    IEnumerator disable()
    {
        yield return new WaitForSeconds(0.1f);
        gameObject.SetActive(false);
    }
}
