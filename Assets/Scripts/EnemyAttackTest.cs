using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackTest : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Color color;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        color = meshRenderer.material.color;
    }

    public void takeDamage(int damage) // damage는 받은 피해량 저장
    {
        StartCoroutine(OnHitColor());   
    }

    // 공격을 받았을 때 생상을 적색으로 변경후 0.1f초 후에 기본 색상으로 변경
    IEnumerator OnHitColor()
    {
        meshRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        meshRenderer.material.color = color;
    }
}
