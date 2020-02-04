using UnityEngine;
using UnityEditor;

public class SliceVisualizer : MonoBehaviour {
	public int maxLevel = 4;

	[Range(0, 10)]
	public int visualizedLevel = 2;

	[Range(0, 64f)]
	public float visualizationScale = 16f;

	public SampleFunctions.Type sampleType = SampleFunctions.Type.Custom1;
	private int prevSampleType = -1;

	private bool[][,,] slices;

	public void Start() {
		Update();
	}

	public void Update() {
		if(prevSampleType != (int)sampleType) {
			prevSampleType = (int)sampleType;
			slices = RT.SL.SliceGenerator.GetSlices(SampleFunctions.functions[prevSampleType], maxLevel);
		}
	}

	public void OnDrawGizmos() {
		if(!Application.isPlaying) {
			return;
		}

		int res = slices[visualizedLevel].GetLength(0);
		float cubeSize = (1f / (float)res) * visualizationScale;
		Gizmos.color = Color.blue;

		// string output = "solid: [";

		for(int x = 0; x < res; x++) {
			for(int y = 0; y < res; y++) {
				for(int z = 0; z < res; z++) {
					bool containsSurface = slices[visualizedLevel][x,y,z];
					// output += containsSurface + ", ";
					if(containsSurface) {
						Vector3 pos = new Vector3(x, y, z);
						pos /= (float)res;
						pos -= Vector3.one * 0.5f;
						pos *= visualizationScale;
						pos += Vector3.one * cubeSize * 0.5f;
						Gizmos.DrawCube(pos, Vector3.one * cubeSize);
					}
				}
			}
		}

		// Debug.Log(output + "]");
	}


}