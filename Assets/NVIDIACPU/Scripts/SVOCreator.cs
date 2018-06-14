
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
	public class Voxel {
		public bool PartiallyFull;
		public bool CompletelyFull;

		public byte LeafMask;
		public byte ValidMask;
		public ushort ChildPointer;
	}

	public void BuildSVO(int maxDepth) {
		Voxel[][] buffers = new Voxel[maxDepth][];
		for(int i = 0; i < maxDepth; i++) {
			buffers[i] = new Voxel[8];
		}

		List<uint> childDecsriptors = new List<uint>();
		List<Voxel> voxels = new List<Voxel>();

		ulong currentVoxel = 0;

		FillBuffer(buffers, maxDepth, ref currentVoxel, voxels);

		
	}
  
	public enum BufferState {
		CompletelyFull,
		PartiallyFull,
		Empty
	}

	public class BufferInfo {
		public bool PartiallyFull;
		public bool CompletelyFull;

		public byte LeafMask;
		public byte ValidMask;
		public ushort ChildPointer;
	}

	public BufferInfo FillBuffer(Voxel[][] buffers, int depth, ref ulong currentVoxel, List<Voxel> voxelList) {
		Voxel[] buffer = buffers[depth];
		// clear the buffer
		for(int i = 0; i < 8; i++) {
			buffer[i] = null;
		}

		BufferInfo info = new BufferInfo();

		if(depth != 0) {
			int fullCount = 0;

			for(int i = 0; i < 8; i++) {
				// fill lower depth buffer
				BufferInfo bufferInfo = FillBuffer(buffers, depth - 1, ref currentVoxel, voxelList);

				// fill the current voxel in this buffer if it contains surface
				if(bufferInfo.PartiallyFull) {
					info.PartiallyFull = true;

					Voxel v = new Voxel();
					v.CompletelyFull = bufferInfo.CompletelyFull;
					v.PartiallyFull = bufferInfo.PartiallyFull;
					v.LeafMask = bufferInfo.LeafMask;
					v.ValidMask = bufferInfo.ValidMask;
					buffer[i] = v;

					info.ValidMask |= (byte)(1 << i);
					if(bufferInfo.CompletelyFull) {
						info.LeafMask |= (byte)(1 << i);
					}
				} 
				
				fullCount++;
			}

			if(fullCount == 8) { info.CompletelyFull = true; }

			// after filling buffer, add all children to the voxel list, keeping track of the pointer to the first child
			bool firstChild = true;

			for(int i = 0; i < 8; i++) {
				if(buffer[i] != null) {
					if(firstChild == true) {
						info.ChildPointer = (ushort)voxelList.Count;
					}
					voxelList.Add(buffer[i]);
				}
			}

		}
		else {
			int fullCount = 0;

			for(ulong i = 0; i < 8; i++) {
				Voxel v = GetVoxelFromMorton(currentVoxel + i);
				buffers[0][i] = v;

				if(v.CompletelyFull) {
					fullCount++;
					info.PartiallyFull = true;
					info.ValidMask |= (byte)(1 << (int)i);
					info.LeafMask |= (byte)(1 << (int)i);
				}

				currentVoxel++;
			}

			if(fullCount == 8) {
				info.CompletelyFull = true;
			}
		}
		return info;
	}

	Voxel GetVoxelFromMorton(ulong morton) {
		int x = 0, y = 0, z = 0;
		MortonDecode(morton, ref x, ref y, ref z);
		Voxel v = new Voxel();
		v.PartiallyFull = UtilFuncs.Sample(x, y, z) > 0;
		if(v.PartiallyFull) {
			v.CompletelyFull = true;
		}
		return v;
	}

	void MortonDecode(ulong morton, ref int x, ref int y, ref int z){
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