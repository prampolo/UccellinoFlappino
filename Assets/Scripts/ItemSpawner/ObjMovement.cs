using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjMovement : MonoBehaviour
{

    public Rigidbody2D rb;

    private float speed = 4f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Vector3 temp = transform.localScale;
        temp.x = 1f;
        transform.localScale = temp;
    }

    void Update()
    {
        rb.velocity = transform.right * -speed;
    }
}
