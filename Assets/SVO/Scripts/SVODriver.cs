using UnityEngine;

namespace RT {
public class SVODriver : MonoBehaviour {
	SVO svo;
	ColoredBox[] debugBoxes;
	public SampleFunctions.Type sampleType = SampleFunctions.Type.FlatGround;
	public int maxLevel = 4;
	public float scale = 16;
	public bool onlyShowLeaves = false;
	
	public GameObject rayStart;
	public GameObject rayEnd;
	private Vector3 lastRayStartPosition = Vector3.zero;
	private Vector3 lastRayEndPosition = Vector3.zero;

	Ray currentRay = new Ray(new Vector3(0, 0, 0), new Vector3(16, 0, 0));
	private ColoredBox[] intersectedNodesBoxes;

	public void Start() {
		UpdateSVO();
		UpdateRaycast();
	}

	void OnValidate() {
		UpdateSVO();
		UpdateRaycast();
	}

	void Update() {
		if(rayStart.transform.position != lastRayStartPosition || 
			rayEnd.transform.position != lastRayEndPosition) {
			lastRayStartPosition = rayStart.transform.position;
			lastRayEndPosition = rayEnd.transform.position;
			UpdateRaycast();
		}
	}

	void UpdateSVO() {
		svo = new NaiveSVO(SampleFunctions.functions[(int)sampleType], maxLevel);
		debugBoxes = svo.GenerateDebugBoxes(onlyShowLeaves);
	}

	void UpdateRaycast() {
		currentRay = new Ray(lastRayStartPosition / scale, lastRayEndPosition - lastRayStartPosition);
		intersectedNodesBoxes = svo.GenerateDebugBoxesAlongRay(currentRay, onlyShowLeaves);
	}

    void OnDrawGizmos() {
		if(!UnityEditor.EditorApplication.isPlaying) {return;}
		foreach(ColoredBox box in debugBoxes) {
        	Gizmos.color = box.Color;
			Gizmos.DrawCube(box.Center * scale, box.Size * scale);
		}
		foreach(ColoredBox box in intersectedNodesBoxes) {
        	Gizmos.color = box.Color;
			Gizmos.DrawCube(box.Center * scale, box.Size * scale);
		}
		Gizmos.color = Color.blue;
		Gizmos.DrawLine(lastRayStartPosition, currentRay.direction * 1000);
    }
}
}
