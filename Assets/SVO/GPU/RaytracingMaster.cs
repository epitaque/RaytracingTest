using System.Collections.Generic;
using UnityEngine;

public class RaytracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
	public Texture SkyboxTexture;
	public Material AddMaterial;
	public Light DirectionalLight;

    private RenderTexture _target;
	private Camera _camera;
	private uint _currentSample = 0;
	private ComputeBuffer _svoBuffer;
	private ComputeBuffer _svoAttachmentsBuffer;

	private void Awake()
	{
		Application.targetFrameRate = 10000;
		_camera = GetComponent<Camera>();
		InitializeShaderParameters();
	}

	private void InitializeShaderParameters() {
		RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
		SetSVOBuffer();
	}

	private void UpdateShaderParameters()
	{
		RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
		RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
		RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
		Vector3 l = DirectionalLight.transform.forward;
		RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
		RayTracingShader.SetBuffer(0, "_SVO", _svoBuffer);
		RayTracingShader.SetBuffer(0, "_SVOAttachments", _svoAttachmentsBuffer);

		
	}

	private void Update() {
		if (transform.hasChanged)
		{
			_currentSample = 0;
			transform.hasChanged = false;
		}

	}

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
		UpdateShaderParameters();
        Render(destination);
    } 

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();



        // Set the target and dispatch the compute shader
        RayTracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

		// Blit the result texture to the screen
		AddMaterial.SetFloat("_Sample", _currentSample);
		Graphics.Blit(_target, destination, AddMaterial);
		_currentSample++;
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();

            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }

	private void SetSVOBuffer() {
		RT.CS.NaiveCreator creator = new RT.CS.NaiveCreator();
		RT.SVOData data = creator.Create(SampleFunctions.functions[(int)SampleFunctions.Type.Simplex], 7);
		_svoBuffer = new ComputeBuffer(data.childDescriptors.Count, 4);
		_svoAttachmentsBuffer = new ComputeBuffer(data.attachments.Count, 4);

		// Print SVO
		string output = "Raytracing SVO Results\n";
		output += "Compressed:\n" + string.Join("\n", data.childDescriptors.ConvertAll(code => new RT.CS.ChildDescriptor(code)));
		Debug.Log(output);

		_svoBuffer.SetData(data.childDescriptors);
		_svoAttachmentsBuffer.SetData(data.attachments);
	}
} 
