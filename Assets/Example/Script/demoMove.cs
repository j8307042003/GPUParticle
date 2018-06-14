using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class demoMove : MonoBehaviour {


    public Emitter m_emitter;

    public float cycleRatio = 1.0f;
    public float length = 1.0f;


    float m_timer = 0.0f;

    float maxRate;
	// Use this for initialization
	void Start () {
        maxRate = m_emitter.emitRate;

    }
	

	// Update is called once per frame
	void Update () {

        m_timer += Time.deltaTime;
        float sinT = Mathf.Sin(m_timer * cycleRatio);
        float cosT = Mathf.Cos(m_timer * cycleRatio);

        m_emitter.emitRate = Random.value * maxRate;

        transform.localPosition += new Vector3( sinT * length, 0.0f, cosT * length);
        transform.rotation =  Quaternion.Euler(sinT * 50f, 0.0f, cosT * 50f);

	}
}
