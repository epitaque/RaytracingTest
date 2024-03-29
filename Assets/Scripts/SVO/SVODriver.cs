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

	[Range(1, 8)]
	public int lowerNodeDrawBound = 1;
	[Range(1, 8)]
	public int upperNodeDrawBound = 8;
	
	public enum SVOType {
		NaiveSVO,
		CompactSVO
	}
	public SVOType svoType = SVOType.CompactSVO;

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
		Debug.Log("Testing svo...");
		UpdateSVO();
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
		return;
		if(svoType == SVOType.CompactSVO) {
			svo = new CompactSVO(SampleFunctions.functions[(int)sampleType], maxLevel);
		} else if(svoType == SVOType.NaiveSVO) {
			svo = new NaiveSVO(SampleFunctions.functions[(int)sampleType], maxLevel);
		}
		debugBoxes = svo.GetAllNodes()
			.Where(node => node.level <= upperNodeDrawBound && node.level >= lowerNodeDrawBound)
			.Select(node => node.GetColoredBox());
	}

	void UpdateRaycast() {
		return;
		//if(UnityEditor.EditorApplication != null && !UnityEditor.EditorApplication.isPlaying) {return;}
		currentRay = new Ray(lastRayStartPosition / scale, lastRayEndPosition - lastRayStartPosition);

		intersectedNodesBoxes = svo.Trace(currentRay)
			.Where(node => node != null && node.level <= upperNodeDrawBound && node.level >= lowerNodeDrawBound)
			.Select(node => {
				ColoredBox b = node.GetColoredBox();
				b.Color.a = 0.5f;
				return b;
			});

	}

    void OnDrawGizmos() {
		return;
		//if(!UnityEditor.EditorApplication.isPlaying) {return;}
		
 		foreach(ColoredBox box in debugBoxes) {
        	Gizmos.color = box.Color;
			Gizmos.DrawCube(box.Center * scale, box.Size * scale);
		}
		float i = 1;
		foreach(ColoredBox box in intersectedNodesBoxes) {
        	Color c = box.Color;
			c.b = i;
			Gizmos.color = c;
			Gizmos.DrawCube(box.Center * scale, box.Size * scale);
			i -= 0.2f;
		}
		Gizmos.color = Color.blue;
		Gizmos.DrawLine(lastRayStartPosition, currentRay.direction * 1000);
		svo.DrawGizmos(scale);
    }
}
}
