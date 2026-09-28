using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float speed = 2f;
    public float changeDirectionTime = 2f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private float timer;
    private Transform chaseTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        ChooseNewDirection();
    }

    void Update()
    {
        if (chaseTarget != null)
        {
            Vector2 toTarget = (Vector2)chaseTarget.position - rb.position;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                movement = toTarget.normalized;
            }
        }
        else
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                ChooseNewDirection();
            }
        }

        if (movement != Vector2.zero)
        {
            float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * speed;
    }

    void ChooseNewDirection()
    {
        movement = Random.insideUnitCircle.normalized;

        timer = changeDirectionTime;
    }

    public void SetChaseTarget(Transform target)
    {
        chaseTarget = target;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("wall"))
        {
            Vector2 normal = collision.GetContact(0).normal;
            movement = Vector2.Reflect(movement, normal).normalized;
            timer = changeDirectionTime;
        }
    }
}