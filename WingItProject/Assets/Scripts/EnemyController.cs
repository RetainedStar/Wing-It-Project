using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    #region Variables

    private float currentHealth;
    public float maximumHealth;

    public float attackDistance;
    bool canAttack;
    public int damage;

    private CapsuleCollider collider;

    NavMeshAgent agent;
    GameObject target;

    #endregion

    #region Methods
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        collider = GetComponent<CapsuleCollider>();
    }

    // Use this for initialization
    void Start()
    {
        currentHealth = maximumHealth;

        canAttack = true;
    }

    // Update is called once per frame
    void Update()
    {
        PlayerCollision();
        FindClosestTarget();
        MoveToTarget();
        DamageTarget();
    }

    void PlayerCollision()
    {
        if (PlayerController.instance.dashed)
        {
            collider.isTrigger = true;
        }
        else
        {
            collider.isTrigger = false;
        }
    }

    void FindClosestTarget()
    {
        float playerDistance = Vector3.Distance(PlayerController.instance.transform.position, transform.position);
        float baseDistance = Vector3.Distance(BaseManager.instance.transform.position, transform.position);

        if (playerDistance < baseDistance)
        {
            target = PlayerController.instance.gameObject;
        }
        else
        {
            target = BaseManager.instance.gameObject;
        }
    }

    void MoveToTarget()
    {
        if (target != null)
        {
            agent.SetDestination(target.transform.position);
        }
    }

    public void Damage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            agent.Stop();
            StartCoroutine(Die());
        }
    }

    void DamageTarget()
    {
        if (Vector3.Distance(target.transform.position, transform.position) < attackDistance)
        {
            if (canAttack)
            {

                if (target.GetComponent<BaseManager>())
                {
                    BaseManager.instance.Damage(damage);
                }
                else if (target.GetComponent<PlayerController>())
                {
                    PlayerController.instance.Damaged(damage);
                }

                canAttack = false;
                Invoke("ReactiveAttack", 1.5f);
            }
        }
    }

    void ReactiveAttack()
    {
        canAttack = true;
    }

    IEnumerator Die()
    {
        Destroy(this.gameObject);
        yield return new WaitForSeconds(0f);
    }

    #endregion

    #region Editor
    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }

    #endregion
}
