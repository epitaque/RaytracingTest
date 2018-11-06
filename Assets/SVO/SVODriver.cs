using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/*
	Class for testing/probing/visualizing octrees
 */
namespace RT {
public class SVODriver : MonoBehaviour {
	SVO svo;
	public SampleFunctions.Type sampleType = SampleFunctions.Type.FlatGround;

	[Range(1, 8)]
	public int maxLevel = 4;

	[Range(1, 256)]
	public float scale = 16;
	public bool onlyShowLeaves = false;
	
	public GameObject rayStart;
	public GameObject rayEnd;
	private Vector3 lastRayStartPosition = Vector3.zero;
	private Vector3 lastRayEndPosition = Vector3.zero;

	/*
		Debug Fields
	 */
	private Ray currentRay = new Ray(new Vector3(0, 0, 0), new Vector3(16, 0, 0));
	private Ray reflectedRay = new Ray(new Vector3(0, 0, 0), new Vector3(16, 0, 0));
	private IEnumerable<ColoredBox> debugBoxes;
	private IEnumerable<ColoredBox> intersectedNodesBoxes;

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
		debugBoxes = svo.GetAllNodes()
			.Where(node => node.Leaf || onlyShowLeaves)
			.Select(node => node.GetColoredBox());
	}

	void UpdateRaycast() {
		currentRay = new Ray(lastRayStartPosition / scale, lastRayEndPosition - lastRayStartPosition);
		Debug.Log("Current ray: " + currentRay.ToString("F4"));

		intersectedNodesBoxes = svo.Trace(currentRay)
			.Where(node => node.Leaf || onlyShowLeaves)
			.Select(node => node.GetColoredBox());
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
		svo.DrawGizmos(scale);
    }
}
}
