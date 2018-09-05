using System.Collections.Generic;
using UnityEngine;

namespace RT {
public static class SVOFunctions {
	public static uint ConstructChildDescriptor(ushort childPointer, byte validMask, byte leafMask) {
		return (uint)(((int)childPointer << 16) + ((int)validMask << 8) + (int)leafMask);
	}

	public static ChildDescriptorInfo DecodeChildDescriptor(uint cd) {
		ChildDescriptorInfo info = new ChildDescriptorInfo();
		info.LeafMask = (byte)(cd & 255);
		info.ValidMask = (byte)(cd >> 8 & 255);
		info.ChildPointer = (ushort)(cd >> 16 & 65535);
		return info;
	}

	public static string StringifyChildDescriptor(uint cd) {
		ChildDescriptorInfo ci = DecodeChildDescriptor(cd);
		return "[cptr" + ci.ChildPointer + ", vm" + System.Convert.ToString(ci.ValidMask, 2) + ", lm" + System.Convert.ToString(ci.LeafMask, 2) + "]";
	}

	public static List<Vector4> CastRay(SVO svo, Ray ray) {
		List<Vector4> intersectedNodes = new List<Vector4>();
		RayStep(svo.ChildDescriptors, ray.origin, ray.direction, intersectedNodes);
		return intersectedNodes;
	}

	static void RayStep(uint[] svo, Vector3 rayOrigin, Vector3 rayDirection, List<Vector4> intersectedNodes) 
	{
		Vector3 max = Vector3.one;
		Vector3 min = Vector3.one * -1;

		float  tx0 = (min.x - rayOrigin.x) / rayDirection.x; 
		float  tx1 = (max.x - rayOrigin.x) / rayDirection.x;  
		float  ty0 = (min.y - rayOrigin.y) / rayDirection.y; 
		float  ty1 = (max.y - rayOrigin.y) / rayDirection.y;  
		float  tz0 = (min.z - rayOrigin.z) / rayDirection.z; 
		float  tz1 = (max.z - rayOrigin.z) / rayDirection.z;

		ProcSubtree(svo,tx0,ty0,tz0,tx1,ty1,tz1, min, 0, 2, intersectedNodes); 
	}

	static void ProcSubtree(uint[] svo,
							 float tx0, float ty0, float tz0, 
							 float tx1, float ty1, float tz1, 
							 Vector3 nodeMin, float nodeSize,
							 int nodeIndex, // will be -1 if leaf
							 List<Vector4> intersectedNodes) 
	{ 
		if ( !(Mathf.Max(tx0,ty0,tz0) < Mathf.Min(tx1,ty1,tz1)) ) {
			return; // this node is not intersected
		}

		if (nodeIndex == -1) 
		{ 
			intersectedNodes.Add(new Vector4(nodeMin.x, nodeMin.y, nodeMin.z, nodeSize));
			return; 
		}

		float txM = 0.5f * (tx0 + tx1); 
		float tyM = 0.5f * (ty0 + ty1); 
		float tzM = 0.5f * (tz0 + tz1);

		// Note, this is based on the assumption that the children are ordered in a particular 
		// manner.  Different octree libraries will have to adjust.

		uint descriptor = svo[nodeIndex];
		ChildDescriptorInfo info = SVOFunctions.DecodeChildDescriptor(descriptor);

		int currPtr = info.ChildPointer;
		int ptr;
		if((info.ValidMask & 1) == 1) {
			if((info.LeafMask & 1) == 1) { ptr = -1; }
			else { ptr = currPtr++; }
			ProcSubtree(svo,tx0,ty0,tz0,txM,tyM,tzM,nodeMin+ST.Constants.vfoffsets[0]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x0y0z0
		}
		if((info.ValidMask & 2) == 2) {
			if((info.LeafMask & 2) == 2) { ptr = -1; }
			else { ptr = currPtr++; }
			ProcSubtree(svo,tx0,ty0,tzM,txM,tyM,tz1,nodeMin+ST.Constants.vfoffsets[1]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x0y0z1
		}
		if((info.ValidMask & 4) == 4) {
			if((info.LeafMask & 4) == 4) { ptr = -1; }
			else { ptr = currPtr++; }
			ProcSubtree(svo,tx0,tyM,tz0,txM,ty1,tzM,nodeMin+ST.Constants.vfoffsets[2]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x0y1z0
		}
		if((info.ValidMask & 8) == 8) {
			if((info.LeafMask & 8) == 8) { ptr = -1; }
			else { ptr = currPtr++; }
			ProcSubtree(svo,tx0,tyM,tzM,txM,ty1,tz1,nodeMin+ST.Constants.vfoffsets[3]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x0y1z1
		}
		if((info.ValidMask & 16) == 16) {
			if((info.LeafMask & 16) == 16) { ptr = -1; }
			else { ptr = currPtr++; }
			ProcSubtree(svo,txM,ty0,tz0,tx1,tyM,tzM,nodeMin+ST.Constants.vfoffsets[4]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x1y0z0
		}
		if((info.ValidMask & 32) == 32) {
			if((info.LeafMask & 32) == 32) { ptr = -1; }
			else { ptr = currPtr++; }
			ProcSubtree(svo,txM,ty0,tzM,tx1,tyM,tz1,nodeMin+ST.Constants.vfoffsets[5]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x1y0z1
		}
		if((info.ValidMask & 64) == 64) {
			if((info.LeafMask & 64) == 64) { ptr = -1; }
			else { ptr = currPtr++; }
			ProcSubtree(svo,txM,tyM,tz0,tx1,ty1,tzM,nodeMin+ST.Constants.vfoffsets[6]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x1y1z0
		}
		if((info.ValidMask & 128) == 128) {
			if((info.LeafMask & 128) == 128) { ptr = -1; }
			else { ptr = currPtr++; }
			ProcSubtree(svo,txM,txM,tzM,tx1,ty1,tz1,nodeMin+ST.Constants.vfoffsets[7]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x1y1z1
		}
	}
}
}
