using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Emitter))]
public class EmitterEditor : Editor {

    SerializedProperty emitter;

    SerializedProperty emitRate;
    SerializedProperty maxParticle;
    SerializedProperty lifespan;
    SerializedProperty startVelocity;
    SerializedProperty acceleration;
    SerializedProperty scaleRandomness;
    SerializedProperty emitKind;
    SerializedProperty radius;
    SerializedProperty coneEmitDegree;
    SerializedProperty boxEmitSize;
    SerializedProperty rotation;
    SerializedProperty _emitCS;
    SerializedProperty _mesh;
    SerializedProperty _material;
    SerializedProperty _debug;

    void OnEnable()
    {

        emitRate = serializedObject.FindProperty("emitRate");
		maxParticle = serializedObject.FindProperty("maxParticle");
		lifespan = serializedObject.FindProperty("lifespan");
		startVelocity = serializedObject.FindProperty("startVelocity");
		acceleration = serializedObject.FindProperty("acceleration");
		scaleRandomness = serializedObject.FindProperty("scaleRandomness");
		emitKind = serializedObject.FindProperty("emitKind");
		radius = serializedObject.FindProperty("radius");
		coneEmitDegree = serializedObject.FindProperty("coneEmitDegree");
		boxEmitSize = serializedObject.FindProperty("boxEmitSize");
		rotation = serializedObject.FindProperty("rotation");
		_emitCS = serializedObject.FindProperty("_emitCS");
		_mesh = serializedObject.FindProperty("_mesh");
		_material = serializedObject.FindProperty("_material");
		_debug = serializedObject.FindProperty("_debug");		

    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

		EditorGUILayout.PropertyField(emitRate);
		EditorGUILayout.PropertyField(maxParticle);
		EditorGUILayout.PropertyField(lifespan);
		GUILayout.Label ("Speed", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(startVelocity);
		EditorGUILayout.PropertyField(acceleration);

		GUILayout.Label ("Scale", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(scaleRandomness);

		GUILayout.Label ("Emit Style", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(emitKind);

        if(emitKind.intValue == (int) Emitter.EmitKind.Box)
        {
           EditorGUILayout.PropertyField(boxEmitSize);
        }
        else if (emitKind.intValue == (int)Emitter.EmitKind.Sphere)
        {
            EditorGUILayout.PropertyField(radius);
        }
        else if (emitKind.intValue == (int)Emitter.EmitKind.Cone)
        {
            EditorGUILayout.PropertyField(coneEmitDegree);
        }
	
		GUILayout.Label ("Rotation", EditorStyles.boldLabel);	
		EditorGUILayout.PropertyField(rotation);

		GUILayout.Label ("Compute Shader", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(_emitCS, true);

		GUILayout.Label ("Emit Object", EditorStyles.boldLabel);		
		EditorGUILayout.PropertyField(_mesh);
		EditorGUILayout.PropertyField(_material);
		EditorGUILayout.PropertyField(_debug);
        serializedObject.ApplyModifiedProperties();
    }
}
