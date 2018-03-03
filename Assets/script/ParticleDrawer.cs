using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleDrawer : MonoBehaviour {

    Emitter emitter;
    public float range = 1.0f;
    public float length = 20.0f;
    public float speed = 1.0f;

    float timer = 0.0f;
	// Use this for initialization
	void Start () {
        emitter = GetComponent<Emitter>();
	}


    // Update is called once per frame
    void Update()
    {
        bool mouseDown = Input.GetMouseButton(0);
        if (emitter != null)
        {
            emitter.emitRate = mouseDown ? 10000.0f : 0.0f;
        }

        if (mouseDown)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            transform.position = Camera.main.transform.position + ray.direction * length;
        }
    }

    void FixedUpdate()
    {
        timer += Time.deltaTime;
        //transform.position += new Vector3(Mathf.Sin(timer * speed), 0.0f, 0.0f);
        transform.rotation = Quaternion.Euler(-90 - range * (2 * Mathf.Sin(timer * speed) - 1), range * (2 * Mathf.Cos(timer * speed) - 1), 0.0f);
    }
}
