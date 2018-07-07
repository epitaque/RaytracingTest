
using System.Collections.Generic;
using UnityEngine;
  
public class SVO {
	uint[] svodata; // consists of pure child descriptors. just getting a pure basic svo working.
	// structure:
	// child pointer | valid mask | leaf mask
	//    16			   8			8
	
	//int pageSize = 4096;
	int maxDescriptors = 100000;
	ulong svoIndex = 0;

	List<ColoredBox> nodes = new List<ColoredBox>();

	public SVO() {
		Debug.Log("Constructing SVO...");

		svodata = new uint[maxDescriptors];

		BuildSVO(2);
		//MortonTest();
	}


	public class Voxel {
		public bool PartiallyFull;
		public bool CompletelyFull;

		public byte LeafMask;
		public byte ValidMask;
		public ushort ChildPointer;

		public int Level;
	}

	public class Node {	
		public Node[] Children;
		public bool PartiallyFull;
		public bool CompletelyFull;
		public Vector3 Position;
		public int LevelsToMax;
		public float Size;
	}

	public struct ColoredBox {
		public Color color;
		public Vector3 center;
		public Vector3 size;
	}

	List<Vector4> IntersectedNodes = new List<Vector4>();
	public void DrawGizmos() {
		foreach(Vector4 node in IntersectedNodes) {
			Gizmos.DrawCube(new Vector3(node.x, node.y, node.z) + Vector3.one * (node.w/2f), node.w * Vector3.one);
		}
		foreach(ColoredBox box in nodes) {
			Gizmos.color = box.color;
			Gizmos.DrawCube(box.center, box.size);
		}
	}

	public void Raycast(Vector3 rayOrigin, Vector3 rayDirection) {
		IntersectedNodes = new List<Vector4>();

		ray_step(svodata, rayOrigin, rayDirection, IntersectedNodes);
	}

	static void ray_step(uint[] svo, Vector3 rayOrigin, Vector3 rayDirection, List<Vector4> intersectedNodes) 
	{
		Vector3 max = Vector3.one;
		Vector3 min = Vector3.one * -1;

		float  tx0 = (min.x - rayOrigin.x) / rayDirection.x; 
		float  tx1 = (max.x - rayOrigin.x) / rayDirection.x;  
		float  ty0 = (min.y - rayOrigin.y) / rayDirection.y; 
		float  ty1 = (max.y - rayOrigin.y) / rayDirection.y;  
		float  tz0 = (min.z - rayOrigin.z) / rayDirection.z; 
		float  tz1 = (max.z - rayOrigin.z) / rayDirection.z;

		proc_subtree(svo,tx0,ty0,tz0,tx1,ty1,tz1, min, 0, 2, intersectedNodes); 
	}

	static void proc_subtree(uint[] svo,
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
		ChildDescriptorInfo info = decodeChildDescriptor(descriptor);
		
		int currPtr = info.childPointer;
		int ptr;
		if((info.validMask & 1) == 1) {
			if((info.leafMask & 1) == 1) { ptr = -1; }
			else { ptr = currPtr++; }
			proc_subtree(svo,tx0,ty0,tz0,txM,tyM,tzM,nodeMin+RT.Constants.vfoffsets[0]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x0y0z0
		}
		if((info.validMask & 2) == 2) {
			if((info.leafMask & 2) == 2) { ptr = -1; }
			else { ptr = currPtr++; }
			proc_subtree(svo,tx0,ty0,tzM,txM,tyM,tz1,nodeMin+RT.Constants.vfoffsets[1]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x0y0z1
		}
		if((info.validMask & 4) == 4) {
			if((info.leafMask & 4) == 4) { ptr = -1; }
			else { ptr = currPtr++; }
			proc_subtree(svo,tx0,tyM,tz0,txM,ty1,tzM,nodeMin+RT.Constants.vfoffsets[2]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x0y1z0
		}
		if((info.validMask & 8) == 8) {
			if((info.leafMask & 8) == 8) { ptr = -1; }
			else { ptr = currPtr++; }
			proc_subtree(svo,tx0,tyM,tzM,txM,ty1,tz1,nodeMin+RT.Constants.vfoffsets[3]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x0y1z1
		}
		if((info.validMask & 16) == 16) {
			if((info.leafMask & 16) == 16) { ptr = -1; }
			else { ptr = currPtr++; }
			proc_subtree(svo,txM,ty0,tz0,tx1,tyM,tzM,nodeMin+RT.Constants.vfoffsets[4]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x1y0z0
		}
		if((info.validMask & 32) == 32) {
			if((info.leafMask & 32) == 32) { ptr = -1; }
			else { ptr = currPtr++; }
			proc_subtree(svo,txM,ty0,tzM,tx1,tyM,tz1,nodeMin+RT.Constants.vfoffsets[5]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x1y0z1
		}
		if((info.validMask & 64) == 64) {
			if((info.leafMask & 64) == 64) { ptr = -1; }
			else { ptr = currPtr++; }
			proc_subtree(svo,txM,tyM,tz0,tx1,ty1,tzM,nodeMin+RT.Constants.vfoffsets[6]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x1y1z0
		}
		if((info.validMask & 128) == 128) {
			if((info.leafMask & 128) == 128) { ptr = -1; }
			else { ptr = currPtr++; }
			proc_subtree(svo,txM,txM,tzM,tx1,ty1,tz1,nodeMin+RT.Constants.vfoffsets[7]*nodeSize,nodeSize/2f,ptr,intersectedNodes); // x1y1z1
		}
	}

	public void BuildSVO(int maxDepth) {
		Voxel[][] buffers = new Voxel[maxDepth+1][];
		for(int i = 0; i < maxDepth+1; i++) {
			buffers[i] = new Voxel[8];
		}

		List<uint> childDecsriptors = new List<uint>();
		List<Voxel> voxels = new List<Voxel>();

		uint currentVoxel = 0;

		BufferInfo rootInfo = FillBuffer(buffers, maxDepth, ref currentVoxel, voxels);

		Voxel rv = new Voxel();
		rv.ChildPointer = rootInfo.ChildPointer;
		rv.CompletelyFull = rootInfo.CompletelyFull;
		rv.LeafMask = rootInfo.LeafMask;
		rv.Level = maxDepth + 1;
		rv.ValidMask = rootInfo.ValidMask;
		rv.PartiallyFull = rootInfo.PartiallyFull;


		voxels.Add(rv);

		string s = "Voxels: ";
		for(int i = 0; i < voxels.Count; i++) {
			Voxel v = voxels[i];
			s += "[" + i + ", lvl" + v.Level  + ", cptr" + v.ChildPointer + ", vm" + System.Convert.ToString(v.ValidMask, 2) + ", lm" + System.Convert.ToString(v.LeafMask, 2) + "]\n";
		}
		for(int i = voxels.Count - 1; i >= 0; i--) {
			Voxel v = voxels[i];
			if(v.ChildPointer != 0) {
				v.ChildPointer = (ushort)(voxels.Count - 1 - v.ChildPointer);
			}
			uint vcode = constructChildDescriptor(v.ChildPointer, v.ValidMask, v.LeafMask);
			childDecsriptors.Add(vcode);
		}
		Debug.Log(s);

		s = "Decoded Voxels (array of child descriptors): ";
		for(int i = 0; i < childDecsriptors.Count; i++) {
			ChildDescriptorInfo cd = decodeChildDescriptor(childDecsriptors[i]);
			s += "[cptr" + cd.childPointer + ", vm" + System.Convert.ToString(cd.validMask, 2) + ", lm" + System.Convert.ToString(cd.leafMask, 2) + "]\n";
		}
		Debug.Log(s);


	}

	public void ConstructDebugBoxesFromVoxelArray(uint[] svo, 
							 Vector3 nodeMin, float nodeSize,
							 int nodeIndex, int nodeDepth // will be -1 if leaf 
							 ) {
		if (nodeIndex == -1) 
		{
			ColoredBox box = new ColoredBox();
			box.center = nodeMin + Vector3.one * nodeSize;
			box.size = new Vector3(nodeSize, nodeSize, nodeSize);
			box.color = UtilFuncs.SinColor(((float)nodeDepth) / 3f);
			nodes.Add(box);
			return; 
		}

		uint descriptor = svo[nodeIndex];
		ChildDescriptorInfo info = decodeChildDescriptor(descriptor);
		
		int currPtr = info.childPointer;
		int ptr;
		if((info.validMask & 1) == 1) {
			if((info.leafMask & 1) == 1) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(svo,nodeMin+RT.Constants.vfoffsets[0]*nodeSize,nodeSize/2f,ptr, nodeDepth + 1); // x0y0z0
		}
		if((info.validMask & 2) == 2) {
			if((info.leafMask & 2) == 2) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(svo,nodeMin+RT.Constants.vfoffsets[1]*nodeSize,nodeSize/2f,ptr, nodeDepth + 1); // x0y0z1
		}
		if((info.validMask & 4) == 4) {
			if((info.leafMask & 4) == 4) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(svo,nodeMin+RT.Constants.vfoffsets[2]*nodeSize,nodeSize/2f,ptr,nodeDepth + 1); // x0y1z0
		}
		if((info.validMask & 8) == 8) {
			if((info.leafMask & 8) == 8) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(svo,nodeMin+RT.Constants.vfoffsets[3]*nodeSize,nodeSize/2f,ptr,nodeDepth + 1); // x0y1z1
		}
		if((info.validMask & 16) == 16) {
			if((info.leafMask & 16) == 16) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(svo,nodeMin+RT.Constants.vfoffsets[4]*nodeSize,nodeSize/2f,ptr,nodeDepth + 1); // x1y0z0
		}
		if((info.validMask & 32) == 32) {
			if((info.leafMask & 32) == 32) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(svo,nodeMin+RT.Constants.vfoffsets[5]*nodeSize,nodeSize/2f,ptr,nodeDepth + 1); // x1y0z1
		}
		if((info.validMask & 64) == 64) {
			if((info.leafMask & 64) == 64) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(svo,nodeMin+RT.Constants.vfoffsets[6]*nodeSize,nodeSize/2f,ptr,nodeDepth + 1); // x1y1z0
		}
		if((info.validMask & 128) == 128) {
			if((info.leafMask & 128) == 128) { ptr = -1; }
			else { ptr = currPtr++; }
			ConstructDebugBoxesFromVoxelArray(svo,nodeMin+RT.Constants.vfoffsets[7]*nodeSize,nodeSize/2f,ptr,nodeDepth + 1); // x1y1z1
		}
	}

	public void VisualizeOctree() {
		this.nodes = new List<ColoredBox>();
		ConstructDebugBoxesFromVoxelArray(svodata, new Vector3(-1, -1, -1), 2f, 0, 0);
	}

  	public class BufferInfo {
		public bool PartiallyFull;
		public bool CompletelyFull;

		public byte LeafMask;
		public byte ValidMask;
		public ushort ChildPointer;
	}

	public BufferInfo FillBuffer(Voxel[][] buffers, int level, ref uint currentVoxel, List<Voxel> voxelList) {
		//Debug.Log("Filling buffer at level " + level);
		Voxel[] buffer = buffers[level];
		// clear the buffer
		for(int i = 0; i < 8; i++) {
			buffer[i] = null;
		}

		BufferInfo info = new BufferInfo();

		if(level != 0) {
			int fullCount = 0;

			for(int i = 0; i < 8; i++) {
				// fill lower depth buffer
				BufferInfo bufferInfo = FillBuffer(buffers, level - 1, ref currentVoxel, voxelList);

				// fill the current voxel in this buffer if it contains surface
				if(bufferInfo.PartiallyFull) {
					info.PartiallyFull = true;

					Voxel v = new Voxel();
					v.CompletelyFull = bufferInfo.CompletelyFull;
					v.PartiallyFull = bufferInfo.PartiallyFull;
					v.LeafMask = bufferInfo.LeafMask;
					v.ValidMask = bufferInfo.ValidMask;
					v.Level = level;
					v.ChildPointer = bufferInfo.ChildPointer;
					buffer[i] = v;

					info.ValidMask |= (byte)(1 << i);
					if(bufferInfo.CompletelyFull) {
						info.LeafMask |= (byte)(1 << i);
						fullCount++;
					}
				} 
			}

			if(fullCount == 8) { 
				info.CompletelyFull = true; 
			}
			else {
				// after filling buffer, add all children to the voxel list, keeping track of the pointer to the first child
				bool firstChild = true;

				if(!info.CompletelyFull) {
					for(int i = 0; i < 8; i++) {
						if(buffer[i] != null && !buffer[i].CompletelyFull) {
							if(firstChild == true) {
								info.ChildPointer = (ushort)voxelList.Count;
							}
							voxelList.Add(buffer[i]);
						}
					}
				}
			}
		}
		else {
			int fullCount = 0;

			for(ulong i = 0; i < 8; i++) {
				Voxel v = GetVoxelFromMorton(currentVoxel++);
				buffers[0][i] = v;

				if(v.CompletelyFull) {
					fullCount++;
					info.PartiallyFull = true;
					info.ValidMask |= (byte)(1 << (int)i);
					info.LeafMask |= (byte)(1 << (int)i);
				}
			}

			if(fullCount == 8) {
				info.CompletelyFull = true;
			}
		}
		return info;
	}

	Voxel GetVoxelFromMorton(uint morton) {
		int x = 0, y = 0, z = 0;
		Morton.mortonDecode(morton, ref x, ref y, ref z);
		Voxel v = new Voxel();
			v.PartiallyFull = UtilFuncs.Sample(x, y, z) > 0;
			if(v.PartiallyFull) {
				v.CompletelyFull = true;
			}
		//Debug.Log("v coords: " + "(" + x + ", " + y + ", " + z + ")");
		//Debug.Log("v.PartiallyFull: " + v.PartiallyFull);
		return v;
	}
	
	uint constructChildDescriptor(ushort childPointer, byte validMask, byte leafMask) {
		return (uint)(((int)childPointer << 16) + ((int)validMask << 8) + (int)leafMask);
	}

	struct ChildDescriptorInfo {
		public ushort childPointer;
		public byte validMask;
		public byte leafMask;
	}

	static ChildDescriptorInfo decodeChildDescriptor(uint cd) {
		ChildDescriptorInfo info = new ChildDescriptorInfo();
		info.leafMask = (byte)(cd & 255);
		info.validMask = (byte)(cd >> 8 & 255);
		info.childPointer = (ushort)(cd >> 16 & 65535);
		return info;
	}
}