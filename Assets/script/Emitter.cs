using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

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
    public int emitKind;
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
    public EmitKind emitKind = EmitKind.Cone;
    public float radius;
    [Range(0.0f, 360.0f)]
    public float coneEmitDegree = 0.0f;
    public Vector3 boxEmitSize;
    public Vector3 rotation;
    public ComputeShader[] _emitCS = new ComputeShader[5];
    public Mesh _mesh;
    public Material _material;

    public bool _debug = false;
        
    enum ComputeShaderKind
    {
        InitBuffer=0,
        EmitCount=1,
        EmitParticle=2,
        UpdateParticle=3,
        SetDrawBufferArg = 4,
    };

    public enum EmitKind
    {
        Sphere=1,
        Cone=2,
        Box=3,
    };


    float emitCount;
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
    int alivelistSecId = Shader.PropertyToID("alivelistSec") ;
    int instancingArgId = Shader.PropertyToID("instancingArg") ;
    int updateIndirectBufferId = Shader.PropertyToID("updateIndirectBuffer");

    
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    Particle[] particle;

    int deadlistCount;
    int alivelistCount;
    EmitParticleInfo emitInfo;

    Vector3 prevPosition;

	// Use this for initialization
	void Start () {
        InitBuffer();
        emitInfo = new EmitParticleInfo();
    }

    void OnEnable()
    {
        prevPosition = transform.position;
    }

    void OnDestroy()
    {
        DisposeBuffer();
    }


    void DisposeBuffer()
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

    void InitBuffer()
    {
        deadlistCB = new ComputeBuffer(maxParticle, sizeof(uint), ComputeBufferType.Append);
        alivelistCB = new ComputeBuffer(maxParticle, sizeof(uint), ComputeBufferType.Append);
        alivelistSecCB = new ComputeBuffer(maxParticle, sizeof(uint), ComputeBufferType.Append);
        emitParticleInfoCB = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EmitParticleInfo)));
        particlePoolCB = new ComputeBuffer(maxParticle, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Particle)));
        particleCounterCB = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ParticleCounter)));
        instancingArgCB = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        updateIndirectCB = new ComputeBuffer(1, 3 * sizeof(uint), ComputeBufferType.IndirectArguments);



        // can't assign to third index of IndirectArguments buffer.  so set it at first
        updateIndirectCB.SetData(new uint[3] { 1, 1, 1 });
        particleCounterCB.SetData(new ParticleCounter[] { new ParticleCounter() });
        alivelistCB.SetCounterValue(0);
        deadlistCB.SetCounterValue(0);



        csID[(int)ComputeShaderKind.InitBuffer] = _emitCS[(int)ComputeShaderKind.InitBuffer].FindKernel("InitDeadlist");
        csID[(int)ComputeShaderKind.EmitCount] = _emitCS[(int)ComputeShaderKind.EmitCount].FindKernel("EmitCount");
        csID[(int)ComputeShaderKind.EmitParticle] = _emitCS[(int)ComputeShaderKind.EmitParticle].FindKernel("EmitParticle");
        csID[(int)ComputeShaderKind.UpdateParticle] = _emitCS[(int)ComputeShaderKind.UpdateParticle].FindKernel("UpdateParticle");
        csID[(int)ComputeShaderKind.SetDrawBufferArg] = _emitCS[(int)ComputeShaderKind.SetDrawBufferArg].FindKernel("SetDrawBufferArg");

 
        DispatchInitDeadList();         
    }

    void DispatchInitDeadList()
    {
        ComputeShader cs = _emitCS[(int)ComputeShaderKind.InitBuffer];
        int kernelId = csID[(int)ComputeShaderKind.InitBuffer];
        deadlistCB.SetCounterValue(0);
        cs.SetBuffer(kernelId, deadlistId, deadlistCB);
        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.Dispatch(kernelId, maxParticle, 1, 1);
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

    void SetEmitInfoBuffer()
    {
        // setting emitter Data
        Quaternion q = Quaternion.Euler(rotation);
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
        emitInfo.rotation.Set(rotation.x, rotation.y, rotation.z, q.w);
        emitInfo.emitKind = (int)emitKind;
        emitInfo.boxEmitSize.Set(boxEmitSize.x / 2.0f, boxEmitSize.y / 2.0f, boxEmitSize.z / 2.0f);
        EmitParticleInfo[] emitInfoParam = new EmitParticleInfo[] { emitInfo };
        emitParticleInfoCB.SetData(emitInfoParam);
    }

    void DispatchEmitCount()
    {
        ComputeShader cs;
        int kernelId;
        cs = _emitCS[(int)ComputeShaderKind.EmitCount];
        kernelId = csID[(int)ComputeShaderKind.EmitCount];
        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.SetBuffer(kernelId, emitParticleInfoId, emitParticleInfoCB);
        cs.SetBuffer(kernelId, updateIndirectBufferId, updateIndirectCB);
        cs.Dispatch(kernelId, 1, 1, 1);
    }

    void DispatchEmitParticle()
    {
        ComputeShader cs;
        int kernelId;
        cs = _emitCS[(int)ComputeShaderKind.EmitParticle];
        kernelId = csID[(int)ComputeShaderKind.EmitParticle];
        cs.SetBuffer(kernelId, emitParticleInfoId, emitParticleInfoCB);
        cs.SetBuffer(kernelId, deadlistId, deadlistCB);
        cs.SetBuffer(kernelId, alivelistId, alivelistCB);
        cs.SetBuffer(kernelId, particlePoolId, particlePoolCB);
        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.Dispatch(kernelId, (int)Mathf.Ceil(emitInfo.emitCount / 1024.0f), 1, 1);
    }

    void DispatchUpdateParticle()
    {
        ComputeShader cs;
        int kernelId;
        uint numIndices = (_mesh != null) ? (uint)_mesh.GetIndexCount(0) : 0;
        args[0] = numIndices;
        args[1] = 0;
        instancingArgCB.SetData(args);

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
    }

    void DispatchDrawArg()
    {
        ComputeShader cs;
        int kernelId;
        cs = _emitCS[(int)ComputeShaderKind.SetDrawBufferArg];
        kernelId = csID[(int)ComputeShaderKind.SetDrawBufferArg];
        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.SetBuffer(kernelId, instancingArgId, instancingArgCB);
        cs.Dispatch(kernelId, 1, 1, 1);
    }

    void FixedUpdate() {
        emitCount += emitRate * Time.deltaTime;

        SetEmitInfoBuffer();
        DispatchEmitCount();    
        if (emitInfo.emitCount > 0) DispatchEmitParticle();
        DispatchUpdateParticle();        
        SwapCB(ref alivelistCB, ref alivelistSecCB); // Swap alive list
        DispatchDrawArg();



        /*
        if (_debug)
        {
            EmitParticleInfo[] emitInfoParam = new EmitParticleInfo[] { emitInfo };
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
        }*/

        prevPosition = transform.position;
        emitCount -= (uint)emitCount;
	}

    void Update()
    {
        /*
        _material.SetBuffer(alivelistId, alivelistCB);
        _material.SetBuffer(particlePoolId, particlePoolCB);
        */
        
        if (mpb == null) { mpb = new MaterialPropertyBlock(); }
        mpb.SetBuffer(alivelistId, alivelistCB);
        mpb.SetBuffer(particlePoolId, particlePoolCB);
        
        //Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, new Bounds(Vector3.zero, new Vector3(10000.0f, 10000.0f, 10000.0f)), instancingArgCB, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true);
        //Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, new Bounds(Vector3.zero, new Vector3(10000.0f, 10000.0f, 10000.0f)), instancingArgCB);
        Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, new Bounds(Vector3.zero, new Vector3(10000.0f, 10000.0f, 10000.0f)), instancingArgCB, 0, mpb);
    }

}
