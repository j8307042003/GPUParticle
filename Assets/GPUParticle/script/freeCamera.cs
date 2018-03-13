using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class freeCamera : MonoBehaviour {

    public float speed = 10.0f;
    public float rotationSpeed = 10.0f;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 dir = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            dir += transform.forward;
        if (Input.GetKey(KeyCode.S))
            dir += -transform.forward;
        if (Input.GetKey(KeyCode.A))
            dir += -transform.right;
        if (Input.GetKey(KeyCode.D))
            dir += transform.right;

        if (dir.magnitude > 0.0f) transform.position += dir.normalized * speed * Time.deltaTime;


        transform.eulerAngles += new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X") , 0.0f);


	}
}
