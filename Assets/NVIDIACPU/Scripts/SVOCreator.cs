
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

	public SVO() {
		svodata = new uint[maxDescriptors];
	}



	/*ulong buildOctree(int x, int y, int z, int size, ulong descriptorIndex) {
		int halfSize = size >> 1;

		int[] posX = {x + halfSize, x, x + halfSize, x, x + halfSize, x, x + halfSize, x};
		int[] posY = {y + halfSize, y + halfSize, y, y, y + halfSize, y + halfSize, y, y};
		int[] posZ = {z + halfSize, z + halfSize, z + halfSize, z + halfSize, z, z, z, z};

		ulong childOffset = svoIndex - descriptorIndex;

		int childCount = 0;
		int[] childIndices = new int[8];
		uint childMask = 0;
		for (int i = 0; i < 8; i++) {
			if (vdata.CubeContainsVoxel(posX[i], posY[i], posZ[i], halfSize)) {
				childMask |= 128u >> i;
				childIndices[childCount++] = i;
			}
		}

		bool hasLargeChildren = false;
		short childPointer;
		byte leafMask;
		byte validMask;
		if (halfSize == 1) {
			leafMask = 0;

			for (int i = 0; i < childCount; i++) {
				int idx = childIndices[childCount - i - 1];
				
				bool childExists = vdata.CubeContainsVoxel(posX[x], posY[idx], posZ[idx], 1);
				if(childExists) {
					leafMask |= (byte)(1 << i);
				}
				//svodata[descriptorIndex]


				allocator.pushBack(vdata->getVoxelDestructive(posX[idx], posY[idx], posZ[idx]));
			}
		} else {
			leafMask = childMask;
			for (int i = 0; i < childCount; i++)
				allocator.pushBack(0);

			ulong grandChildOffsets[8];
			ulong delta = 0;
			ulong insertionCount = allocator.insertionCount();
			for (int i = 0; i < childCount; i++) {
				int idx = childIndices[childCount - i - 1];
				grandChildOffsets[i] = delta + buildOctree(allocator, posX[idx], posY[idx], posZ[idx],
					halfSize, descriptorIndex + childOffset + i);
				delta += allocator.insertionCount() - insertionCount;
				insertionCount = allocator.insertionCount();
				if (grandChildOffsets[i] > 0x3FFF)
					hasLargeChildren = true;
			}

			for (int i = 0; i < childCount; i++) {
				ulong childIndex = descriptorIndex + childOffset + i;
				ulong offset = grandChildOffsets[i];
				if (hasLargeChildren) {
					offset += childCount - i;
					allocator.insert(childIndex + 1, uint(offset));
					allocator[childIndex] |= 0x20000;
					offset >>= 32;
				}
				allocator[childIndex] |= uint(offset << 18);
			}
		}

		allocator[descriptorIndex] = (childMask << 8) | leafMask;
		if (hasLargeChildren)
			allocator[descriptorIndex] |= 0x10000;

		return childOffset;
	}*/
	public struct voxel {
		public bool containsSurface;
		public bool completelyFull;
		public byte leafMask;
		public byte validMask;
		public ushort childPointer;
	}

	public void BuildSVO(int maxDepth) {
		int resolution = (int)Mathf.Pow(2, maxDepth);
		ulong resolution_3 = (ulong)(resolution * resolution * resolution);

		// step 1: initial state
		voxel[][] buffers = new voxel[maxDepth][];
		ulong[] divisors = new ulong[maxDepth];

		for(int i = 0; i < maxDepth; i++) {
			buffers[i] = new voxel[8];
			divisors[i] = (ulong)Mathf.Pow(2, i);
		}

		List<uint> childDecsriptors = new List<uint>();
		List<voxel> voxels = new List<voxel>();

		for(ulong currentVoxel = 0; currentVoxel < resolution_3; currentVoxel += 8) {
			// step 2: read 8 voxels into lowest buffer
			for(ulong i = 0; i < 8; i++) {
				buffers[0][i] = getVoxelFromMorton(currentVoxel + i);
			}
			
			for(int level = 1; level < maxDepth; level++) {
				if(currentVoxel % divisors[level] == 0) {
					int numActiveVoxels = 0;
					bool partiallyFull = false;
					bool completelyFull = false;

					byte validMask = 0;
					byte leafMask = 0;

					for(ulong i = 0; i < 8; i++) {
						voxel v = buffers[level - 1][i];
						if(v.containsSurface) {
							numActiveVoxels++;
							validMask |= (byte)(1 << (int)i);
							if(v.completelyFull) {
								leafMask |= (byte)(1 << (int)i);
							}
						}
					}
					if(numActiveVoxels > 0) {
						partiallyFull = true;
						if(numActiveVoxels == 8) {
							completelyFull = true;
						}
						// Write to disk if not a leaf
						voxel v;
						v.validMask = validMask;
						v.leafMask = leafMask;

						voxels.Add(v);
					}

				}
			}
		}




	}

	public void fillBuffer(int maxDepth, int currentDepth) {

	}

	voxel getVoxelFromMorton(ulong morton) {
		int x = 0, y = 0, z = 0;
		mortonDecode(morton, ref x, ref y, ref z);
		voxel v;
		v.containsSurface = UtilFuncs.Sample(x, y, z) > 0;
		v.completelyFull = true;

		v.childPointer = 0;
		v.leafMask = 0;
		v.validMask = 0;
		return v;
	}

	void mortonDecode(ulong morton, ref int x, ref int y, ref int z){
		x = 0;
		y = 0;
		z = 0;
		for (ulong i = 0; i < (sizeof(ulong) * 8)/3; ++i) {
			x |= (int)((morton & ((ulong)( 1ul ) << (int)((3ul * i) + 0ul))) >> (int)(((3ul * i) + 0ul)-i));
			y |= (int)((morton & ((ulong)( 1ul ) << (int)((3ul * i) + 1ul))) >> (int)(((3ul * i) + 1ul)-i));
			z |= (int)((morton & ((ulong)( 1ul ) << (int)((3ul * i) + 2ul))) >> (int)(((3ul * i) + 2ul)-i));
		}
	}

	uint constructChildDescriptor(ushort childPointer, byte validMask, byte leafMask) {
		return (uint)(((int)childPointer << 16) + ((int)validMask << 8) + (int)leafMask);
	}

}