using UnityEngine;
using UnityEngine.InputSystem;

public class playerMovementPc : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        movement = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
                movement.x = -1;

            if (Keyboard.current.dKey.isPressed)
                movement.x = 1;

            if (Keyboard.current.sKey.isPressed)
                movement.y = -1;

            if (Keyboard.current.wKey.isPressed)
                movement.y = 1;
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(
            rb.position + movement.normalized * speed * Time.fixedDeltaTime
        );
    }
}
