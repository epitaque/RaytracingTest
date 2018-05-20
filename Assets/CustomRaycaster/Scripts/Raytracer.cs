using UnityEngine;
using System.Collections;
 
public class Raytracer : MonoBehaviour
{  
    public Material material;
    public ComputeShader compute_shader;
    RenderTexture render_texture;
    float width,height;
   
    void Start ()
    {
        render_texture = new RenderTexture(Screen.width,Screen.height,0);
        render_texture.enableRandomWrite = true;
        render_texture.Create();
        //compute_shader = (ComputeShader)Resources.Load("raymarching_direct_compute");
        compute_shader.SetTexture(0,"render_texture",render_texture);      
        material.SetTexture("MainTex",render_texture);
    }
   
    void Update()
    {
        compute_shader.SetVector("camera_origin",Camera.main.gameObject.transform.position);
		compute_shader.SetVector("camera_up",Camera.main.gameObject.transform.up);
		compute_shader.SetVector("camera_right",Camera.main.gameObject.transform.right);
		compute_shader.SetVector("camera_direction",Camera.main.gameObject.transform.forward);
		compute_shader.SetMatrix("cameraToWorld",Camera.main.cameraToWorldMatrix);

        compute_shader.SetFloat("height",Screen.height);
        compute_shader.SetFloat("width",Screen.width);      
        compute_shader.Dispatch(0,render_texture.width/8,render_texture.height/8,1);
    }
   
    void OnRenderImage (RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit (source, destination, material);
    }
   
}
 
