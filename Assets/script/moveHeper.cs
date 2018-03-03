using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveHeper : MonoBehaviour {

    public float speed = 1.0f;
    public float range = 1.0f;
    public float length = 20.0f;
    float timer;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            transform.position = Camera.main.transform.position + ray.direction * length;
        }
	}

    void FixedUpdate()
    {
        timer += Time.deltaTime;
        //transform.position += new Vector3(Mathf.Sin(timer * speed), 0.0f, 0.0f);
        transform.rotation = Quaternion.Euler(-90 - range* ( 2 * Mathf.Sin(timer * speed) - 1), range* (2*Mathf.Cos(timer * speed)-1), 0.0f);
    }
}
