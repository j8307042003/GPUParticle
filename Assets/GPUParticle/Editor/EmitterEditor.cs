using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Emitter))]
public class EmitterEditor : Editor {

    SerializedProperty emitter;

    SerializedProperty emitRate;
    SerializedProperty _maxParticle;
    SerializedProperty lifespan;
    SerializedProperty startVelocity;
    SerializedProperty acceleration;
    SerializedProperty scaleRandomness;
    SerializedProperty emitKind;
    SerializedProperty radius;
    SerializedProperty coneEmitDegree;
    SerializedProperty boxEmitSize;
    SerializedProperty rotation;
    SerializedProperty _mesh;
    SerializedProperty _material;
    SerializedProperty _debug;
    SerializedProperty receiveShadow;
    SerializedProperty castShadow;
    SerializedProperty startVelocityRandomness;

    void OnEnable()
    {

        emitRate = serializedObject.FindProperty("emitRate");
		_maxParticle = serializedObject.FindProperty("_maxParticle");
		lifespan = serializedObject.FindProperty("lifespan");
		startVelocity = serializedObject.FindProperty("startVelocity");
		acceleration = serializedObject.FindProperty("acceleration");
		scaleRandomness = serializedObject.FindProperty("scaleRandomness");
		emitKind = serializedObject.FindProperty("emitKind");
		radius = serializedObject.FindProperty("radius");
		coneEmitDegree = serializedObject.FindProperty("coneEmitDegree");
		boxEmitSize = serializedObject.FindProperty("boxEmitSize");
		rotation = serializedObject.FindProperty("rotation");
		_mesh = serializedObject.FindProperty("_mesh");
		_material = serializedObject.FindProperty("_material");
		_debug = serializedObject.FindProperty("_debug");
        receiveShadow = serializedObject.FindProperty("receiveShadow");
        castShadow = serializedObject.FindProperty("castShadow");
        startVelocityRandomness = serializedObject.FindProperty("startVelocityRandomness");
    }

    public override void OnInspectorGUI()
    {
        Emitter emitter = target as Emitter;

        serializedObject.Update();

		EditorGUILayout.PropertyField(emitRate);

        EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(_maxParticle);
        if (EditorGUI.EndChangeCheck()) { emitter.WantReset(); }
        

		EditorGUILayout.PropertyField(lifespan);
		GUILayout.Label ("Speed", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(startVelocity);
        EditorGUILayout.PropertyField(startVelocityRandomness);

		EditorGUILayout.PropertyField(acceleration);

		GUILayout.Label ("Scale", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(scaleRandomness);

		GUILayout.Label ("Emit Style", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(emitKind);

        switch (emitKind.intValue)
        {
            case (int)Emitter.EmitKind.Box:
                EditorGUILayout.PropertyField(boxEmitSize);
                break;
            case (int)Emitter.EmitKind.Sphere:
                EditorGUILayout.PropertyField(radius);
                break;
            case (int)Emitter.EmitKind.Cone:
                EditorGUILayout.PropertyField(coneEmitDegree);
                break;
        }

        GUILayout.Label ("Rotation", EditorStyles.boldLabel);	
		EditorGUILayout.PropertyField(rotation);

		GUILayout.Label ("Emit Object", EditorStyles.boldLabel);		
		EditorGUILayout.PropertyField(_mesh);
		EditorGUILayout.PropertyField(_material);

        EditorGUILayout.PropertyField(receiveShadow);
        EditorGUILayout.PropertyField(castShadow);


        EditorGUILayout.PropertyField(_debug);
        serializedObject.ApplyModifiedProperties();
    }
}
