// Helper Library compute shader


float4 Quaternion( float3 axis, float angle ) {
	// assume axis vector is normalized
	float s = sin(0.5f * angle);

	return float4(axis.x * s, axis.y * s, axis.z * s, cos(0.5f * angle));
}

float4 Quaternion( float w, float x, float y, float z ) {
	return float4(x, y, z, w);
}

float4 Quaternion() {
	return float4(0.0, 0.0, 0.0, 1.0);
}

float4 EulerToQuaternion(float3 euler)
{
	float4 q;
        // Abbreviations for the various angular functions
	float cy = cos(euler.y * 0.5);
	float sy = sin(euler.y * 0.5);
	float cr = cos(euler.z * 0.5);
	float sr = sin(euler.z * 0.5);
	float cp = cos(euler.x * 0.5);
	float sp = sin(euler.x * 0.5);

	q.w = cy * cr * cp + sy * sr * sp;
	q.x = cy * sr * cp - sy * cr * sp;
	q.y = cy * cr * sp + sy * sr * cp;
	q.z = sy * cr * cp - cy * sr * sp;
	return q;
}

float4 InvertQuternion(float4 quaternion) {
	return Quaternion(quaternion.w, quaternion.x, quaternion.y, quaternion.z);
}

float4 NormalizeQuaternion(float4 q)
{
	float4 q1 = float4(0.0, 0.0, 0.0, 0.0);
	float len_inv = 1.0 / sqrt(q.w * q.w + q.x * q.x + q.y * q.y + q.z * q.z);
	q1.w = q.w * len_inv;
	q1.x = q.x * len_inv;
	q1.y = q.y * len_inv;
	q1.z = q.z * len_inv;
	return q1;
}


// Quaternion multiplication
// http://mathworld.wolfram.com/Quaternion.html
float4 qmul(float4 q1, float4 q2)
{
	return float4(
		q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
		q1.w * q2.w - dot(q1.xyz, q2.xyz)
	);
}

float QDot(float4 q1, float4 q2) 
{
	return q1.w * q2.w + q1.x * q2.x + q1.y * q2.y + q1.z * q2.z;
}

float4 QSlerp(float4 q1, float4 q2, float t)
{
	// the clamp takes care of floating-point errors
	float omega = acos(clamp(QDot(q1,q2), -1.0f, 1.0f));
	float sin_inv = 1.0f / sin(omega);

	return
		sin((1.0f - t) * omega) * sin_inv * q1
		+ sin(t * omega) * sin_inv * q2;
}


// Vector rotation with a quaternion
// http://mathworld.wolfram.com/Quaternion.html
float3 rotate_vector(float3 v, float4 r)
{
	float4 r_c = r * float4(-1, -1, -1, 1);
	return qmul(r, qmul(float4(v, 0), r_c)).xyz;
}

float4x4 QuaternionMatrix(float4 q) {
	return float4x4(
		1 - 2 * q.y * q.y - 2 * q.z * q.z, 2 * q.x* q.y + 2 * q.w * q.z, 2 * q.x * q.z - 2 * q.w * q.y, 0.0,
		2 * q.x * q.y - 2 * q.w * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z, 2 * q.y * q.z + 2 * q.w * q.x, 0.0,
		2 * q.x * q.z + 2 * q.w * q.y, 2 * q.y * q.z - 2 * q.w* q.x, 1 - 2 * q.x * q.x - 2 * q.y * q.y, 0.0,
		0.0, 0.0, 0.0, 1.0
	);
}

float4x4 ScaleMatrix(float3 scale) {
	return float4x4(
		scale.x,		0.0,		0.0,		0.0,
		0.0,			scale.y,	0.0,		0.0,
		0.0,			0.0,		scale.z,	0.0,
		0.0,			0.0,		0.0,		1.0
	);
}

float4x4 TranslationMatrix(float3 pos) {
	return float4x4(
		1.0, 0.0, 0.0, pos.x,
		0.0, 1.0, 0.0, pos.y,
		0.0, 0.0, 1.0, pos.z,
		0.0, 0.0, 0.0, 1.0
	);
}