using System.Collections.Generic;
using UnityEngine;

namespace RT {
public class SVOCreatorMorton : SVOCreator {
	public SVO Create(UtilFuncs.Sampler sampler, int maxDepth) {
		Voxel[][] buffers = new Voxel[maxDepth+1][];
		for(int i = 0; i < maxDepth+1; i++) {
			buffers[i] = new Voxel[8];
		}

		List<uint> childDecsriptors = new List<uint>();
		List<Voxel> voxels = new List<Voxel>();

		uint currentVoxel = 0;

		BufferInfo rootInfo = FillBuffer(buffers, maxDepth, ref currentVoxel, voxels, sampler);

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
			uint vcode = SVOFunctions.ConstructChildDescriptor(v.ChildPointer, v.ValidMask, v.LeafMask);
			childDecsriptors.Add(vcode);
		}
		Debug.Log(s);

		s = "Decoded Voxels (array of child descriptors): ";
		for(int i = 0; i < childDecsriptors.Count; i++) {
			ChildDescriptorInfo cd = SVOFunctions.DecodeChildDescriptor(childDecsriptors[i]);
			s += "[cptr" + cd.ChildPointer + ", vm" + System.Convert.ToString(cd.ValidMask, 2) + ", lm" + System.Convert.ToString(cd.LeafMask, 2) + "]\n";
		}
		Debug.Log(s);

		SVO svo = new SVO();
		svo.ChildDescriptors = childDecsriptors.ToArray();
		return svo;
	}

  	private class BufferInfo {
		public bool PartiallyFull;
		public bool CompletelyFull;

		public byte LeafMask;
		public byte ValidMask;
		public ushort ChildPointer;
	}

	private static BufferInfo FillBuffer(Voxel[][] buffers, int level, ref uint currentVoxel, List<Voxel> voxelList, UtilFuncs.Sampler sampler) {
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
				BufferInfo bufferInfo = FillBuffer(buffers, level - 1, ref currentVoxel, voxelList, sampler);

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
				Voxel v = GetVoxelFromMorton(currentVoxel++, sampler);
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

	private static Voxel GetVoxelFromMorton(uint morton, UtilFuncs.Sampler sampler) {
		int x = 0, y = 0, z = 0;
		MortonUtil.MortonDecode(morton, ref x, ref y, ref z);
		Voxel v = new Voxel();
			v.PartiallyFull = sampler(x, y, z) > 0;
			if(v.PartiallyFull) {
				v.CompletelyFull = true;
			}
		//Debug.Log("v coords: " + "(" + x + ", " + y + ", " + z + ")");
		//Debug.Log("v.PartiallyFull: " + v.PartiallyFull);
		return v;
	}
	
}
}