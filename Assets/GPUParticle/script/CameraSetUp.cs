using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CameraSetUp : MonoBehaviour {
    [SerializeField]
    private Material postprocessMaterial;

    public Material _debugMaterial;

    Camera cam = null;

    static RenderTexture rt_texture;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode = DepthTextureMode.DepthNormals;
        //cam.depthTextureMode = DepthTextureMode.Depth;
    }

    /*
    //method which is automatically called by unity after the camera is done rendering
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        
        //get viewspace to worldspace matrix and pass it to shader
        Matrix4x4 viewToWorld = cam.cameraToWorldMatrix;
        postprocessMaterial.SetMatrix("_viewToWorld", viewToWorld);
        //draws the pixels from the source texture to the destination texture
        Graphics.Blit(source, destination, postprocessMaterial);
        rt_texture = destination;
        
    }
    */
}
