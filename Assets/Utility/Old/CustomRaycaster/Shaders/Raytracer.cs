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
		Camera.main.targetTexture = render_texture;
    }
   
    void Update()
    {
        compute_shader.SetVector("camera_origin",Camera.main.gameObject.transform.position);
		compute_shader.SetVector("camera_up",Camera.main.gameObject.transform.up);
		compute_shader.SetVector("camera_right",Camera.main.gameObject.transform.right);
		compute_shader.SetVector("camera_direction",Camera.main.gameObject.transform.forward);
		compute_shader.SetFloat("camera_fov",Camera.main.fieldOfView);
		compute_shader.SetFloat("camera_fov_multiplier",Mathf.Tan(Camera.main.fieldOfView / 2f * Mathf.PI / 180f));
		compute_shader.SetMatrix("camera_to_world", Matrix4x4.Transpose(Camera.main.cameraToWorldMatrix));

        compute_shader.SetFloat("height",Screen.height);
        compute_shader.SetFloat("width",Screen.width);      
        compute_shader.Dispatch(0,render_texture.width/8,render_texture.height/8,1);

		
    }
   
	void UpdateTexture() {

	}

	void OnDrawGizmos() {
		float height = 5;
		float width = (int)(height * Camera.main.aspect);

		float aspectRatio = width / height;
		float fov = Camera.main.fieldOfView;
		float fovmodifier = Mathf.Tan(Camera.main.fieldOfView / 2f * Mathf.PI / 180f);

		Vector3 camPos = Camera.main.gameObject.transform.position;

		for(int x = 0; x < width; x++) {
			for(int y = 0; y < height; y++) {

				float Px = (2f * ((x + 0.5f) / width) - 1) * fovmodifier * aspectRatio;
				float Py = (1f - (2f * ((y + 0.5f) / height)) ) * fovmodifier; 


				//* Mathf.Tan(fov / 2f * Mathf.PI / 180f) * 

				Vector3 ray_origin = new Vector3(0, 0, 0);
				Vector3 ray_direction = new Vector3(Px, Py, -1) - ray_origin;

				ray_origin = Camera.main.cameraToWorldMatrix * ray_origin;
				ray_direction = Camera.main.cameraToWorldMatrix * ray_direction;

				//Debug.Log("Drawing ray. Origin: " + ray_origin + ", direction: " + ray_direction);

				//Gizmos.DrawRay(camPos, ray_direction);
			}
		}
	}

    void OnRenderImage (RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit (source, destination, material);
    }
   
}
 
