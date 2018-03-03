using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour {
    public ComputeShader cs;
    ComputeBuffer csB;
	// Use this for initialization
	void Start () {
        int kernelid = cs.FindKernel("test");
        csB = new ComputeBuffer(1, 3 * sizeof(int), ComputeBufferType.IndirectArguments);
        csB.SetData(new int[]{ 1,1,1 });

        cs.DispatchIndirect(kernelid, csB, 0);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
