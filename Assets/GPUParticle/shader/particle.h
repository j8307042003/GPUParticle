
struct EmitParticleInfo
{
	uint emitCount;
	uint realEmitCount;
	float lifespan;
	float _dt;
	float3 scale;
	float startVelocity;
	float startVelocityRandomness;
	float3 originPos;
	float radius;
	float4 emitterRot;
	float scaleRandom;
	float3 acceleration;
	float coneEmitAngle;
	float3 prevPosition;
	int emitKind;
	float3 angularSpeed;
	float3 boxEmitSize;
	
};

struct ParticleCounter{
  uint alivelistCount;
  uint deadlistCount;
  uint updateParticleCount;
};

struct Particle {
	float lifespan;
	float3 position;
	float3 velocity;
	float4x4 model;
	float3 scale;
	float4 quaternion;
	uint id;
};

struct IndirectArgumentBuffer
{
	uint arg[5];
};

struct IndirectDispatchBuffer
{
	int arg[3];
};
