using System.Collections.Generic;
using UnityEngine;

namespace RT {
public static class SVOVisualizer {
	public static void DrawSVOGizmos(SVO svo) {
		if(svo.GizmoBoxes == null || svo.GizmoBoxes.Count != svo.ChildDescriptors.Length) {
			svo.GizmoBoxes = ConstructSVOGizmoBoxes(svo);
		}
		for(int i = 0; i < svo.GizmoBoxes.Count; i++) {
			Gizmos.color = svo.GizmoBoxes[i].Color;
			Gizmos.DrawCube(svo.GizmoBoxes[i].Center, svo.GizmoBoxes[i].Size);
		}
	}

	public static List<ColoredBox> ConstructSVOGizmoBoxes(SVO svo) {
		List<ColoredBox> debugBoxes = new List<ColoredBox>();
		ConstructDebugBoxesFromVoxelArray(debugBoxes, svo.ChildDescriptors, Vector3.one * -1, 2, 0, 0);
		return debugBoxes;
	}

	public static void ConstructDebugBoxesFromVoxelArray(
							List<ColoredBox> debugBoxes,
							uint[] svo, 
							Vector3 nodeMin, float nodeSize,
							int nodeIndex, // will be -1 if leaf 
							int nodeDepth) {

		//Debug.Log("ConstructDebugBoxesFromVoxelArray called with min " + nodeMin + ", size " + nodeSize);

		ColoredBox box = new ColoredBox();
		box.Center = nodeMin + Vector3.one * (nodeSize/2f);
		box.Size = new Vector3(nodeSize, nodeSize, nodeSize);
		box.Color = UtilFuncs.SinColor(((float)nodeDepth) * 3f);
		box.Color.a = (float)(nodeDepth + 1) / 10f;
		debugBoxes.Add(box);

		if (nodeIndex == -1) 
		{
			return; 
		}

		uint descriptor = svo[nodeIndex];
		ChildDescriptorInfo info = SVOFunctions.DecodeChildDescriptor(descriptor);

		//Debug.Log("Info: " + StringifyChildDescriptor(descriptor));

		int currPtr = info.ChildPointer;
		int ptr;
		float halfSize = nodeSize / 2f;

		if((info.ValidMask & 1) == 1) {
			if((info.LeafMask & 1) == 1) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(debugBoxes, svo,nodeMin+ST.Constants.vfoffsets[0]*halfSize,halfSize,ptr, nodeDepth + 1); // x0y0z0
		}
		if((info.ValidMask & 2) == 2) {
			if((info.LeafMask & 2) == 2) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(debugBoxes, svo,nodeMin+ST.Constants.vfoffsets[1]*halfSize,halfSize,ptr, nodeDepth + 1); // x0y0z1
		}
		if((info.ValidMask & 4) == 4) {
			if((info.LeafMask & 4) == 4) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(debugBoxes, svo,nodeMin+ST.Constants.vfoffsets[2]*halfSize,halfSize,ptr,nodeDepth + 1); // x0y1z0
		}
		if((info.ValidMask & 8) == 8) {
			if((info.LeafMask & 8) == 8) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(debugBoxes, svo,nodeMin+ST.Constants.vfoffsets[3]*halfSize,halfSize,ptr,nodeDepth + 1); // x0y1z1
		}
		if((info.ValidMask & 16) == 16) {
			if((info.LeafMask & 16) == 16) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(debugBoxes, svo,nodeMin+ST.Constants.vfoffsets[4]*halfSize,halfSize,ptr,nodeDepth + 1); // x1y0z0
		}
		if((info.ValidMask & 32) == 32) {
			if((info.LeafMask & 32) == 32) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(debugBoxes, svo,nodeMin+ST.Constants.vfoffsets[5]*halfSize,halfSize,ptr,nodeDepth + 1); // x1y0z1
		}
		if((info.ValidMask & 64) == 64) {
			if((info.LeafMask & 64) == 64) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(debugBoxes, svo,nodeMin+ST.Constants.vfoffsets[6]*halfSize,halfSize,ptr,nodeDepth + 1); // x1y1z0
		}
		if((info.ValidMask & 128) == 128) {
			if((info.LeafMask & 128) == 128) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(debugBoxes, svo,nodeMin+ST.Constants.vfoffsets[7]*halfSize,halfSize,ptr,nodeDepth + 1); // x1y1z1
		}
	}

}
}
