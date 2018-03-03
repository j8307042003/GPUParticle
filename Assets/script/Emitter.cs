using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
/*
struct EmitParticleInfo
{
    public uint emitCount;
    public uint realEmitCount;
    public float lifespan;
    public float _dt;
    public Vector3 scale;
    public Vector3 originPos;
    public Vector3 forwardDir;
    public float startVelocity;
    public Vector3 acceleration;
    public float radius;
}*/

struct EmitParticleInfo
{
    public uint emitCount;
    public uint realEmitCount;
    public float lifespan;
    public float _dt;
    public Vector3 scale;
    public float startVelocity;
    public Vector3 originPos;
    public float radius;
    public Vector3 forwardDir;
    public float scaleRandom;
    public Vector3 acceleration;
    public float coneEmitAngle;
    public Vector3 prevPosition;
    public float pad1;
    public Vector4 rotation;
    public Vector3 boxEmitSize;
}

struct ParticleCounter
{
    public uint alivelistCount;
    public uint deadlistCount;
    public uint updateParticleCount;
}


struct Particle
{
    public float lifespan;
    public Vector3 position;
    public Vector3 velocity;
    public Matrix4x4 model;
    public Vector3 scale;
    public Vector4 quaternion;
}




public class Emitter : MonoBehaviour {

    public float emitRate=0;
    public int maxParticle;
    public float lifespan;
    public float startVelocity;
    public Vector3 acceleration;
    [Range(0.0f, 1.0f)]
    public float scaleRandomness = 0.0f;
    public float radius;
    [Range(0.0f, 360.0f)]
    public float coneEmitDegree = 0.0f;

    public Vector3 boxEmitSize;

    public Vector3 rotation;

    float emitCount;
    
    enum ComputeShaderKind
    {
        InitBuffer=0,
        EmitCount=1,
        EmitParticle=2,
        UpdateParticle=3,
        SetDrawBufferArg = 4,
    };

    public ComputeShader[] _emitCS = new ComputeShader[5];
    public Mesh _mesh;
    public Material _material;

    public bool _debug = false;

    int[] csID = new int[5];

    MaterialPropertyBlock mpb;

    ComputeBuffer deadlistCB;
    ComputeBuffer alivelistCB;
    ComputeBuffer emitParticleInfoCB;
    ComputeBuffer particlePoolCB;
    ComputeBuffer particleCounterCB;
    ComputeBuffer alivelistSecCB;
    ComputeBuffer instancingArgCB;
    ComputeBuffer updateIndirectCB;


    int deadlistId = Shader.PropertyToID("deadlist") ;
    int alivelistId = Shader.PropertyToID("alivelist") ;
    int emitParticleInfoId = Shader.PropertyToID("emitParticleInfo") ;
    int particlePoolId = Shader.PropertyToID("particlePool") ;
    int particleCounterId = Shader.PropertyToID("particleCounter") ;
    int waitDeadId = Shader.PropertyToID("waitDead") ;
    int alivelistSecId = Shader.PropertyToID("alivelistSec") ;
    int instancingArgId = Shader.PropertyToID("instancingArg") ;
    int updateIndirectBufferId = Shader.PropertyToID("updateIndirectBuffer");


    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    //uint[] deadlist;
    //uint[] alivelist;

    Particle[] particle;

    int deadlistCount;
    int alivelistCount;

    Vector3 prevPosition;

	// Use this for initialization
	void Start () {
        deadlistCB = new ComputeBuffer(maxParticle, sizeof(uint), ComputeBufferType.Append);
        alivelistCB = new ComputeBuffer(maxParticle, sizeof(uint), ComputeBufferType.Append);
        alivelistSecCB = new ComputeBuffer(maxParticle, sizeof(uint), ComputeBufferType.Append);
        emitParticleInfoCB = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EmitParticleInfo)));
        particlePoolCB = new ComputeBuffer(maxParticle, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Particle)));
        particleCounterCB = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ParticleCounter)));
        instancingArgCB = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        updateIndirectCB = new ComputeBuffer(1, 3 * sizeof(uint), ComputeBufferType.IndirectArguments);


        //particle = new Particle[maxParticle];

        // IndirectArguments bugs. can't assign to third index.  so set it at first
        updateIndirectCB.SetData(new uint[3] { 1, 1, 1 });
        particleCounterCB.SetData(new ParticleCounter[] { new ParticleCounter() });
        alivelistCB.SetCounterValue(0);
        deadlistCB.SetCounterValue(0);

        prevPosition = transform.position;

        //deadlist = new uint[maxParticle];
        //alivelist = new uint[maxParticle];

        csID[(int)ComputeShaderKind.InitBuffer] = _emitCS[(int)ComputeShaderKind.InitBuffer].FindKernel("InitDeadlist");
        csID[(int)ComputeShaderKind.EmitCount] = _emitCS[(int)ComputeShaderKind.EmitCount].FindKernel("EmitCount");
        csID[(int)ComputeShaderKind.EmitParticle] = _emitCS[(int)ComputeShaderKind.EmitParticle].FindKernel("EmitParticle");
        csID[(int)ComputeShaderKind.UpdateParticle] = _emitCS[(int)ComputeShaderKind.UpdateParticle].FindKernel("UpdateParticle");
        csID[(int)ComputeShaderKind.SetDrawBufferArg] = _emitCS[(int)ComputeShaderKind.SetDrawBufferArg].FindKernel("SetDrawBufferArg");

        /*
        particlePoolCB.SetData(particle);
        alivelistCB.SetData(alivelist);
        deadlistCB.SetData(deadlist);
        */
        InitDeadList();
    }

    void OnEnable()
    {
        prevPosition = transform.position;
    }

    void OnDestroy()
    {
        deadlistCB.Dispose();
        alivelistCB.Dispose();
        emitParticleInfoCB.Dispose();
        particlePoolCB.Dispose();
        particleCounterCB.Dispose();
        alivelistSecCB.Dispose();
        instancingArgCB.Dispose();
        updateIndirectCB.Dispose();
    }


    void InitDeadList()
    {
        ComputeShader cs = _emitCS[(int)ComputeShaderKind.InitBuffer];
        int kernelId = csID[(int)ComputeShaderKind.InitBuffer];
        deadlistCB.SetCounterValue(0);
        cs.SetBuffer(kernelId, deadlistId, deadlistCB);
        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.Dispatch(kernelId, maxParticle, 1, 1);

        //deadlistCB.GetData(deadlist);
    }

    int GetBufferCount(ComputeBuffer cb)
    {

        int[] args = new int[] { 0 };
        ComputeBuffer copyCountCB = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        ComputeBuffer.CopyCount(cb, copyCountCB, 0);
        copyCountCB.GetData(args);

        copyCountCB.Dispose();
        return args[0]; 
    }

    void updateDeadlist()
    {
        deadlistCount = GetBufferCount(deadlistCB);
    }

    void updateAlivelist()
    {
        alivelistCount = GetBufferCount(alivelistCB);
    }
	
    void SwapCB( ref ComputeBuffer a, ref ComputeBuffer b)
    {
        ComputeBuffer tmp = a;
        a = b;
        b = tmp;
    }

    // Update is called once per frame
    void FixedUpdate() {
        emitCount += emitRate * Time.deltaTime;

        ComputeShader cs;
        int kernelId;
        // setting emitter Data
        Quaternion q = Quaternion.Euler(rotation);
        EmitParticleInfo emitInfo = new EmitParticleInfo();
        emitInfo.emitCount = (uint)emitCount;
        emitInfo.lifespan = lifespan;
        emitInfo._dt = Time.deltaTime;
        emitInfo.scaleRandom = scaleRandomness;
        emitInfo.originPos = transform.position;
        emitInfo.forwardDir = transform.forward;
        emitInfo.startVelocity = startVelocity;
        emitInfo.acceleration = acceleration;
        emitInfo.scale = transform.localScale;
        emitInfo.prevPosition = prevPosition;
        emitInfo.radius = radius;
        emitInfo.coneEmitAngle = Mathf.Deg2Rad * coneEmitDegree;
        emitInfo.rotation.Set(q.x, q.y, q.z, q.w);
        emitInfo.boxEmitSize.Set(boxEmitSize.x / 2.0f, boxEmitSize.y / 2.0f, boxEmitSize.z / 2.0f);
        EmitParticleInfo[] emitInfoParam = new EmitParticleInfo[] { emitInfo };
        emitParticleInfoCB.SetData(emitInfoParam);


        cs = _emitCS[(int)ComputeShaderKind.EmitCount];
        kernelId = csID[(int)ComputeShaderKind.EmitCount];
        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.SetBuffer(kernelId, emitParticleInfoId, emitParticleInfoCB);
        cs.SetBuffer(kernelId, updateIndirectBufferId, updateIndirectCB);
        cs.Dispatch(kernelId, 1, 1, 1);

        // Stage 1 Emit Particle
        if (emitInfo.emitCount > 0)
        {
            cs = _emitCS[(int)ComputeShaderKind.EmitParticle];
            kernelId = csID[(int)ComputeShaderKind.EmitParticle];
            cs.SetBuffer(kernelId, emitParticleInfoId, emitParticleInfoCB);
            cs.SetBuffer(kernelId, deadlistId, deadlistCB);
            cs.SetBuffer(kernelId, alivelistId, alivelistCB);
            cs.SetBuffer(kernelId, particlePoolId, particlePoolCB);
            cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
            cs.Dispatch(kernelId, (int)Mathf.Ceil(emitInfo.emitCount / 1024.0f), 1, 1);
        }

        uint numIndices = (_mesh != null) ? (uint)_mesh.GetIndexCount(0) : 0;
        args[0] = numIndices;
        args[1] = 0;
        instancingArgCB.SetData(args);

        // Stage 2 Update Particle
        cs = _emitCS[(int)ComputeShaderKind.UpdateParticle];
        alivelistSecCB.SetCounterValue(0);
        kernelId = csID[(int)ComputeShaderKind.UpdateParticle];
        cs.SetBuffer(kernelId, emitParticleInfoId, emitParticleInfoCB);
        cs.SetBuffer(kernelId, deadlistId, deadlistCB);
        cs.SetBuffer(kernelId, alivelistId, alivelistCB);
        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.SetBuffer(kernelId, particlePoolId, particlePoolCB);
        cs.SetBuffer(kernelId, alivelistSecId, alivelistSecCB);
        cs.SetBuffer(kernelId, instancingArgId, instancingArgCB);
        cs.DispatchIndirect(kernelId, updateIndirectCB, 0);
        
        // Swap alive list
        SwapCB(ref alivelistCB, ref alivelistSecCB);


        
        cs = _emitCS[(int)ComputeShaderKind.SetDrawBufferArg];
        kernelId = csID[(int)ComputeShaderKind.SetDrawBufferArg];
        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.SetBuffer(kernelId, instancingArgId, instancingArgCB);
        cs.Dispatch(kernelId, 1, 1, 1);
        
        
        if (_debug)
        {
            ParticleCounter[] particleC = new ParticleCounter[] { new ParticleCounter() };
            uint[] arg = new uint[5] { 0, 0, 0, 0, 0 };
            uint[] indirectB = new uint[3];
            uint[] alivelist = new uint[maxParticle];
            uint[] alivelistSec = new uint[maxParticle];
            particle = new Particle[maxParticle];
            particlePoolCB.GetData(particle);
            alivelistCB.GetData(alivelist);
            alivelistSecCB.GetData(alivelistSec);
            int aliveC = GetBufferCount(alivelistCB);
            int aliveSecC = GetBufferCount(alivelistSecCB);
            int deadlistC = GetBufferCount(deadlistCB);
            instancingArgCB.GetData(arg);
            particleCounterCB.GetData(particleC);
            updateIndirectCB.GetData(indirectB);
            emitParticleInfoCB.GetData(emitInfoParam);
            ;
        }

        

        // Render Particle

        //particlePoolCB.GetData(particle);
        /* borken in unity  2017.3.0f3
        if (mpb == null) { mpb = new MaterialPropertyBlock();}
        mpb.SetBuffer("alivelist", alivelistCB);
        mpb.SetBuffer("particlePool", particlePoolCB);
        *//*
        _material.SetBuffer(alivelistId, alivelistCB);
        _material.SetBuffer(particlePoolId, particlePoolCB);


        //Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, new Bounds(Vector3.zero, new Vector3(10000.0f, 10000.0f, 10000.0f)), instancingArgCB, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true);
        Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, new Bounds(Vector3.zero, new Vector3(10000.0f, 10000.0f, 10000.0f)), instancingArgCB);
        //Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, new Bounds(Vector3.zero, new Vector3(10000.0f, 10000.0f, 10000.0f)), instancingArgCB, 0, mpb);

    */
        prevPosition = transform.position;
        emitCount -= (uint)emitCount;
	}

    void Update()
    {
        _material.SetBuffer(alivelistId, alivelistCB);
        _material.SetBuffer(particlePoolId, particlePoolCB);


        //Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, new Bounds(Vector3.zero, new Vector3(10000.0f, 10000.0f, 10000.0f)), instancingArgCB, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true);
        Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, new Bounds(Vector3.zero, new Vector3(10000.0f, 10000.0f, 10000.0f)), instancingArgCB);
        //Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, new Bounds(Vector3.zero, new Vector3(10000.0f, 10000.0f, 10000.0f)), instancingArgCB, 0, mpb);
    }

}
