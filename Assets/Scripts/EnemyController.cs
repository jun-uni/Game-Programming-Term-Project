using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public Transform player;
    Animator animator;
    NavMeshAgent agent;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    void Update()
    {
        // 플레이어 따라가기
        agent.SetDestination(player.position);

        // 속도에 따라 애니메이션 전환
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);

        // 예시: 공격 거리 도달 시
        if (Vector3.Distance(transform.position, player.position) < 2f)
        {
            animator.SetBool("IsAttacking", true);
        }
        else
        {
            animator.SetBool("IsAttacking", false);
        }
    }

    public void Die()
    {
        animator.SetBool("IsDead", true);
        agent.isStopped = true;
    }
}
