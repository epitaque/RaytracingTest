using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseTextureTest : MonoBehaviour {
	public ComputeShader shader;
	public GameObject quad;

	public int resolution = 128;

	private RenderTexture currentTex;

	public void Start() {
		InitializeShader();
		RunShader();
	}

	public void InitializeShader() {
		int kernelHandle = shader.FindKernel("CSMain");

		RenderTextureDescriptor rtd = new RenderTextureDescriptor();
		rtd.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
		rtd.width = resolution;
		rtd.height = resolution;
		rtd.volumeDepth = 1;
		rtd.msaaSamples = 4;
		rtd.enableRandomWrite = true;
		rtd.colorFormat = RenderTextureFormat.Default;
		rtd.autoGenerateMips = false;

		currentTex = new RenderTexture(rtd);
		currentTex.wrapMode = TextureWrapMode.Mirror;
		currentTex.Create();

		shader.SetTexture(kernelHandle, "_densitiesRW", currentTex);
		quad.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", currentTex);
	}

	public void RunShader() {
		int kernelHandle = shader.FindKernel("CSMain");
		
		shader.SetTexture(kernelHandle, "_densitiesRW", currentTex);
		shader.Dispatch(kernelHandle, 1,resolution/4,resolution/4);
		quad.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", currentTex);

		//Debug.Log("mc: " + )
	}

	public void Update() {
		//RunShader();	
	}
}