using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Rigidbody player;
    private Vector3 position;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        position = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = position;
    }

    public void OnMove(InputValue value)
    {
        var movement = value.Get<Vector2>();
        position = Vector3.MoveTowards(position, position + new Vector3(movement.x, 0, movement.y), 1);
    }
}
