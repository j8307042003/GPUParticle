using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Emitter : MonoBehaviour {

    #region Data Structure
    struct EmitParticleInfo
    {
        public uint emitCount;
        public uint realEmitCount;
        public float lifespan;
        public float _dt;
        public Vector3 scale;
        public float startVelocity;
        public float startVelocityRandomness;
        public Vector3 originPos;
        public float radius;
        public Quaternion emitterRot;
        public float scaleRandom;
        public Vector3 acceleration;
        public float coneEmitAngle;
        public Vector3 prevPosition;
        public int emitKind;
        public Vector3 angularSpeed;
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

    public enum EmitKind
    {
        Sphere = 1,
        Cone = 2,
        Box = 3,
    };
    #endregion

    #region Public Variable
    public float emitRate = 0;

    [SerializeField]
    int _maxParticle;   
    public int maxParticle { get { return _maxParticle; }
                             set { _maxParticle = value; _reset = true; } }
    public float lifespan;
    public float startVelocity;
    [Range(0.0f, 1.0f)]
    public float startVelocityRandomness = 0.0f;
    public Vector3 acceleration;
    [Range(0.0f, 1.0f)]
    public float scaleRandomness = 0.0f;
    public EmitKind emitKind = EmitKind.Cone;
    public float radius;
    [Range(0.0f, 360.0f)]
    public float coneEmitDegree = 0.0f;
    public Vector3 boxEmitSize;
    public Vector3 rotation;
    [SerializeField]
    public Mesh _mesh;
    public Material _material;
    public bool receiveShadow = false;
    public bool castShadow = false;
    public bool _debug = false;
        
    public ComputeShader InitBufferCS;
    public ComputeShader EmitCountCS;
    public ComputeShader EmitParticleCS;
    public ComputeShader UpdateParticleCS;
    public ComputeShader SetDrawBufferArgCS;
    #endregion

    #region Private Variable
    int InitBufferCSID;
    int EmitCountCSID;
    int EmitParticleCSID;
    int UpdateParticleCSID;
    int SetDrawBufferArgCSID;

    float emitCount;

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
    int timeId = Shader.PropertyToID("time");
    
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    bool _reset = false;

    int deadlistCount;
    int alivelistCount;
    Vector3 prevPosition;
    // cache push array. reduce CG
    EmitParticleInfo[] emitInfoParam;
    #endregion

    #region MonoBehaviour

    void Start () {
        LoadDefaultComputeShader();
        InitBuffer();
    }

    void OnEnable()
    {
        prevPosition = transform.position;
    }

    void OnDestroy()
    {
        DisposeBuffer();
    }
    #endregion

    #region Resource Init And Setting 
    void ResetBuffer()
    {
        DisposeBuffer();
        InitBuffer();
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
        deadlistCB = new ComputeBuffer(_maxParticle, sizeof(uint), ComputeBufferType.Append);
        alivelistCB = new ComputeBuffer(_maxParticle, sizeof(uint), ComputeBufferType.Append);
        alivelistSecCB = new ComputeBuffer(_maxParticle, sizeof(uint), ComputeBufferType.Append);
        emitParticleInfoCB = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EmitParticleInfo)));
        particlePoolCB = new ComputeBuffer(_maxParticle, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Particle)));
        particleCounterCB = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ParticleCounter)));
        instancingArgCB = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        updateIndirectCB = new ComputeBuffer(1, 3 * sizeof(uint), ComputeBufferType.IndirectArguments);



        // can't assign to third index of IndirectArguments buffer in compute shader somehow.  so set it at first
        updateIndirectCB.SetData(new uint[3] { 1, 1, 1 });
        particleCounterCB.SetData(new ParticleCounter[] { new ParticleCounter() });
        alivelistCB.SetCounterValue(0);
        deadlistCB.SetCounterValue(0);

        InitBufferCSID = InitBufferCS.FindKernel("InitDeadlist");
        EmitCountCSID = EmitCountCS.FindKernel("EmitCount");
        EmitParticleCSID = EmitParticleCS.FindKernel("EmitParticle");
        UpdateParticleCSID = UpdateParticleCS.FindKernel("UpdateParticle");
        SetDrawBufferArgCSID = SetDrawBufferArgCS.FindKernel("SetDrawBufferArg");


        DispatchInitDeadList();
        _reset = false;      
    }

    void LoadDefaultComputeShader()
    {

        if (InitBufferCS == null) InitBufferCS = Resources.Load<ComputeShader>("shader/InitParticleBuffer");
        if (EmitCountCS == null) EmitCountCS = Resources.Load<ComputeShader>("shader/EmitCount");
        if (EmitParticleCS == null) EmitParticleCS = Resources.Load<ComputeShader>("shader/EmitParticle");
        if (UpdateParticleCS == null) UpdateParticleCS = Resources.Load<ComputeShader>("shader/UpdateParticle");
        if (SetDrawBufferArgCS == null) SetDrawBufferArgCS = Resources.Load<ComputeShader>("shader/SetDrawBufferArg");
    }

    void SetEmitInfoBuffer()
    {
        if (emitInfoParam == null)
        {
            emitInfoParam = new EmitParticleInfo[1];
        }

        // setting emitter Data
        emitInfoParam[0].emitCount = (uint)emitCount;
        emitInfoParam[0].lifespan = lifespan;
        emitInfoParam[0]._dt = Time.deltaTime;
        emitInfoParam[0].scaleRandom = scaleRandomness;
        emitInfoParam[0].originPos = transform.position;
        emitInfoParam[0].emitterRot = transform.rotation;
        emitInfoParam[0].startVelocity = startVelocity;
        emitInfoParam[0].startVelocityRandomness = startVelocityRandomness;
        emitInfoParam[0].acceleration = acceleration;
        emitInfoParam[0].scale = transform.localScale;
        emitInfoParam[0].prevPosition = prevPosition;
        emitInfoParam[0].radius = radius;
        emitInfoParam[0].coneEmitAngle = Mathf.Deg2Rad * coneEmitDegree;
        emitInfoParam[0].angularSpeed.Set(rotation.x * Mathf.Deg2Rad, rotation.y * Mathf.Deg2Rad, rotation.z * Mathf.Deg2Rad);
        emitInfoParam[0].emitKind = (int)emitKind;
        emitInfoParam[0].boxEmitSize.Set(boxEmitSize.x / 2.0f, boxEmitSize.y / 2.0f, boxEmitSize.z / 2.0f);

        emitParticleInfoCB.SetData(emitInfoParam);
    }
    #endregion

    #region Public Method
    public void WantReset()
    {
        _reset = true;
    }

    public void WantEmit(int num)
    {
        if (num > _maxParticle) num = _maxParticle;
        emitCount += num;
    }
    #endregion

    #region Utility
    static void Swap<T>(ref T lhs, ref T rhs)
    {
        T temp;
        temp = lhs;
        lhs = rhs;
        rhs = temp;
    }
    #endregion

    #region Compute Shader Dispatch
    void DispatchInitDeadList()
    {
        ComputeShader cs = InitBufferCS;
        int kernelId = InitBufferCSID;
        if (cs == null) return;

        deadlistCB.SetCounterValue(0);
        cs.SetBuffer(kernelId, deadlistId, deadlistCB);
        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.Dispatch(kernelId, _maxParticle, 1, 1);
    }

    // Update Stage 1
    // calculate how many particle can be emitted.
    void DispatchEmitCount()
    {
        ComputeShader cs;
        int kernelId;
        cs = EmitCountCS;
        kernelId = EmitCountCSID;
        if (cs == null) return;

        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.SetBuffer(kernelId, emitParticleInfoId, emitParticleInfoCB);
        cs.SetBuffer(kernelId, updateIndirectBufferId, updateIndirectCB);
        cs.Dispatch(kernelId, 1, 1, 1);
    }

    // Update Stage 2
    // Emit particle
    void DispatchEmitParticle()
    {
        ComputeShader cs;
        int kernelId;
        cs = EmitParticleCS;
        kernelId = EmitParticleCSID;
        if (cs == null) return;

        cs.SetBuffer(kernelId, emitParticleInfoId, emitParticleInfoCB);
        cs.SetBuffer(kernelId, deadlistId, deadlistCB);
        cs.SetBuffer(kernelId, alivelistId, alivelistCB);
        cs.SetBuffer(kernelId, particlePoolId, particlePoolCB);
        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.SetFloat(timeId, Time.time);
        cs.Dispatch(kernelId, (int)Mathf.Ceil(emitCount / 1024.0f), 1, 1);
    }

    // Update Stage 3
    // Update particle
    void DispatchUpdateParticle()
    {
        ComputeShader cs;
        int kernelId;
        uint numIndices = (_mesh != null) ? (uint)_mesh.GetIndexCount(0) : 0;
        args[0] = numIndices;
        args[1] = 0;
        instancingArgCB.SetData(args);

        cs = UpdateParticleCS;
        alivelistSecCB.SetCounterValue(0);
        kernelId = UpdateParticleCSID;
        if (cs == null) return;

        cs.SetBuffer(kernelId, emitParticleInfoId, emitParticleInfoCB);
        cs.SetBuffer(kernelId, deadlistId, deadlistCB);
        cs.SetBuffer(kernelId, alivelistId, alivelistCB);
        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.SetBuffer(kernelId, particlePoolId, particlePoolCB);
        cs.SetBuffer(kernelId, alivelistSecId, alivelistSecCB);
        cs.SetBuffer(kernelId, instancingArgId, instancingArgCB);
        cs.DispatchIndirect(kernelId, updateIndirectCB, 0);
    }

    // Update Stage 5
    // update instancing indirect buffer
    void DispatchDrawArg()
    {
        ComputeShader cs;
        int kernelId;
        cs = SetDrawBufferArgCS;
        kernelId = SetDrawBufferArgCSID;
        if (cs == null) return;

        cs.SetBuffer(kernelId, particleCounterId, particleCounterCB);
        cs.SetBuffer(kernelId, instancingArgId, instancingArgCB);
        cs.Dispatch(kernelId, 1, 1, 1);
    }
    #endregion

    #region MonoBehaviour Update

    void FixedUpdate() {
        emitCount += emitRate * Time.deltaTime;

        if (_reset)
        {
            ResetBuffer();
            return;
        }

        SetEmitInfoBuffer();
        DispatchEmitCount();    
        if (emitCount > 0) DispatchEmitParticle();
        DispatchUpdateParticle();        
        Swap<ComputeBuffer>(ref alivelistCB, ref alivelistSecCB); // Swap alive list
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

        if (_mesh == null || _material == null) return;

        if (mpb == null) { mpb = new MaterialPropertyBlock(); }
        mpb.SetBuffer(alivelistId, alivelistCB);
        mpb.SetBuffer(particlePoolId, particlePoolCB);
        
        Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, new Bounds(Vector3.zero, new Vector3(10000.0f, 10000.0f, 10000.0f)), instancingArgCB, 0, mpb, castShadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadow );
    }
    #endregion

    #region Debug Function
    // for Debug 
    int GetBufferCount(ComputeBuffer cb)
    {

        int[] args = new int[] { 0 };
        ComputeBuffer copyCountCB = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        ComputeBuffer.CopyCount(cb, copyCountCB, 0);
        copyCountCB.GetData(args);

        copyCountCB.Dispose();
        return args[0];
    }

    // for Debug
    void updateDeadlist()
    {
        deadlistCount = GetBufferCount(deadlistCB);
    }

    // for Debug
    void updateAlivelist()
    {
        alivelistCount = GetBufferCount(alivelistCB);
    }
    #endregion
}
