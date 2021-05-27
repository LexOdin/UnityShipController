using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    // Start is called before the first frame update

    Rigidbody myRB; 

    void Start()
    {
        myRB = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        myRB.velocity = Vector3.zero;

        Vector3 translation = Vector3.zero;
        if(Input.GetKey(KeyCode.W))
        {
            translation += transform.forward * 0.3f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            translation -= transform.right * 0.3f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            translation -= transform.forward * 0.3f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            translation += transform.right * 0.3f;
        }
        myRB.MovePosition(translation + transform.position);
    }
}
