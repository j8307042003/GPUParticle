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
        public float bound;
        public int bCollision;
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
        public Vector4 color;
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
    public bool screenSpaceCollision = true;
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

    public Material _debugMaterial;
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
    int _ViewProjId = Shader.PropertyToID("_ViewProj");
    int CamerDepthSizeId = Shader.PropertyToID("CamerDepthSize");
    
    int _CameraDepthTextureId = Shader.PropertyToID("_CameraDepthNormalsTexture");
    int DepthNormalTex = Shader.PropertyToID("DepthNormalTex");

    Camera m_camera;

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
        m_camera = Camera.main;
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

        Vector3 boundV3 = _mesh.bounds.extents;
        boundV3.Scale(transform.localScale);


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
        emitInfoParam[0].bound = boundV3.sqrMagnitude;
        emitInfoParam[0].bCollision = screenSpaceCollision ? 1 : 0;

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


    float[] GetViewProjectionArray()
    {
        var view = m_camera.worldToCameraMatrix;
        var proj = GL.GetGPUProjectionMatrix(m_camera.projectionMatrix, true);
        var vp = proj * view;
        return new float[] {
            vp.m00, vp.m10, vp.m20, vp.m30,
            vp.m01, vp.m11, vp.m21, vp.m31,
            vp.m02, vp.m12, vp.m22, vp.m32,
            vp.m03, vp.m13, vp.m23, vp.m33
        };
    }

    float[] GetInvViewProjectionArray()
    {
        var view = m_camera.worldToCameraMatrix;
        var proj = GL.GetGPUProjectionMatrix(m_camera.projectionMatrix, true);
        var vp = proj * view;
        var invVp = vp.inverse;
        return new float[] {
            invVp.m00, invVp.m10, invVp.m20, invVp.m30,
            invVp.m01, invVp.m11, invVp.m21, invVp.m31,
            invVp.m02, invVp.m12, invVp.m22, invVp.m32,
            invVp.m03, invVp.m13, invVp.m23, invVp.m33
        };
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

        cs.SetFloats( _ViewProjId, GetViewProjectionArray());
        cs.SetBuffer( kernelId, emitParticleInfoId, emitParticleInfoCB);
        cs.SetBuffer( kernelId, deadlistId, deadlistCB);
        cs.SetBuffer( kernelId, alivelistId, alivelistCB);
        cs.SetBuffer( kernelId, particleCounterId, particleCounterCB);
        cs.SetBuffer( kernelId, particlePoolId, particlePoolCB);
        cs.SetBuffer( kernelId, alivelistSecId, alivelistSecCB);
        cs.SetBuffer( kernelId, instancingArgId, instancingArgCB);


        if (Shader.GetGlobalTexture(_CameraDepthTextureId) != null)
        {
            Texture depthTexture = Shader.GetGlobalTexture(_CameraDepthTextureId);
            cs.SetTexture(kernelId, DepthNormalTex, depthTexture);
            cs.SetVector(CamerDepthSizeId, new Vector4(depthTexture.width, depthTexture.height));
        }

        SetCamParams(cs);
        cs.DispatchIndirect(kernelId, updateIndirectCB, 0);
    }


    public string prefix = "_Cam";

    [SerializeField]
    string
        propModelToWorld = "_O2W",
        propWorldToModel = "_W2O",
        propWorldToCam = "_W2C",
        propCamToWorld = "_C2W",
        propCamProjection = "_C2P",
        propCamVP = "_VP",
        propScreenToCam = "_S2C",
        propProjectionParams = "_PParams",
        propScreenParams = "_SParams";

    void SetCamParams(ComputeShader cs)
    {
        var worldToCam = m_camera.worldToCameraMatrix.inverse;
        var camToWorld = m_camera.cameraToWorldMatrix;
        var projection = GL.GetGPUProjectionMatrix(m_camera.projectionMatrix, false);
        var inverseP = projection.inverse;
        var vp = projection * worldToCam;
        var projectionParams = new Vector4(1f, m_camera.nearClipPlane, m_camera.farClipPlane, 1f / m_camera.farClipPlane);
        var screenParams = new Vector4(m_camera.pixelWidth, m_camera.pixelHeight, 1f + 1f / (float)m_camera.pixelWidth, 1f + 1f / (float)m_camera.pixelHeight);

        if (cs != null)
        {
            cs.SetMatrix(prefix + propWorldToCam, worldToCam);
            cs.SetMatrix(prefix + propCamProjection, projection);
            cs.SetMatrix(prefix + propCamVP, vp);
            cs.SetMatrix(prefix + propScreenToCam, inverseP);
            cs.SetMatrix(prefix + propCamToWorld, camToWorld);
            cs.SetVector(prefix + propProjectionParams, projectionParams);
            cs.SetVector(prefix + propScreenParams, screenParams);
        }
        else
        {
            Shader.SetGlobalMatrix(prefix + propWorldToCam, worldToCam);
            Shader.SetGlobalMatrix(prefix + propCamProjection, projection);
            Shader.SetGlobalMatrix(prefix + propCamVP, vp);
            Shader.SetGlobalMatrix(prefix + propScreenToCam, inverseP);
            Shader.SetGlobalMatrix(prefix + propCamToWorld, camToWorld);
            Shader.SetGlobalVector(prefix + propProjectionParams, projectionParams);
            Shader.SetGlobalVector(prefix + propScreenParams, screenParams);
        }
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


               
        if (_debug)
        {
            ParticleCounter[] particleC = new ParticleCounter[] { new ParticleCounter() };
            uint[] arg = new uint[5] { 0, 0, 0, 0, 0 };
            uint[] indirectB = new uint[3];
            uint[] alivelist = new uint[maxParticle];
            uint[] alivelistSec = new uint[maxParticle];
            alivelistCB.GetData(alivelist);
            alivelistSecCB.GetData(alivelistSec);
            int aliveC = GetBufferCount(alivelistCB);
            int aliveSecC = GetBufferCount(alivelistSecCB);
            int deadlistC = GetBufferCount(deadlistCB);
            instancingArgCB.GetData(arg);
            particleCounterCB.GetData(particleC);
            updateIndirectCB.GetData(indirectB);
            emitParticleInfoCB.GetData(emitInfoParam);

            int z  = 0;
        }

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
    
    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (_debugMaterial)
        {
            Texture texture = Shader.GetGlobalTexture(_CameraDepthTextureId);


            if (texture == null) return;
            var w = texture.width / 4;
            var h = texture.height / 4;

            var rect = new Rect(0, 0, w, h);

            _debugMaterial.SetTexture("_CameraDepthTexture", texture);
            Graphics.DrawTexture(rect, texture, _debugMaterial);
        }
    }
    
    #endregion
}
