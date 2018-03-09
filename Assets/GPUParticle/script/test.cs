using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour {

    public Vector3 dir;
    public float angle;
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
            
  
        if (Input.GetKeyDown(KeyCode.X))
          transform.rotation *= Quaternion.AngleAxis(angle, dir.normalized); ;
	}

    Vector3 rotate_vector(Vector3 v, Quaternion r)
    {
        Quaternion r_c = r * new Quaternion(-1, -1, -1, 1);
        Quaternion val = r * (Quaternion.AngleAxis(0.0f, v) *  r_c);
        return new Vector3(val.x, val.y, val.z);

        //return qmul(r, qmul(float4(v, 0), r_c)).xyz;
    }
}
