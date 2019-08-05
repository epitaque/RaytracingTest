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

	[Range(1, 8)]
	public int maxLevel = 5;
	public SampleFunctions.Type sampleType = SampleFunctions.Type.Custom1;

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

		// Rebuild SVO command
		if(UnityEngine.Input.GetKeyDown(KeyCode.R)) {
			SetSVOBuffer();
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
		Debug.Log("Setting svo buffer...");

		RT.CS.NaiveCreator creator = new RT.CS.NaiveCreator();
		RT.SVOData data = creator.Create(SampleFunctions.functions[(int)sampleType], maxLevel);
		_svoBuffer = new ComputeBuffer(data.childDescriptors.Count, 4);
		_svoAttachmentsBuffer = new ComputeBuffer(data.attachments.Count, 4);

		// Print SVO
		/* string output = "\n";
		for(int i = 0; i < data.childDescriptors.Count; i++) {
			int normal = (int)(data.attachments[i*2 + 1] >> 16);
			output += "CD: " + new RT.CS.ChildDescriptor(data.childDescriptors[i]) + "\n"; //, Normal: v" + Vector3.Normalize(RT.CS.NaiveCreator.decodeRawNormal16(normal)) + System.Convert.ToString(normal, 2).PadLeft(16, '0') + "(" + normal + ")\n";
		}

		GUIUtility.systemCopyBuffer = output;*/

		_svoBuffer.SetData(data.childDescriptors);
		_svoAttachmentsBuffer.SetData(data.attachments);
	}
} 
