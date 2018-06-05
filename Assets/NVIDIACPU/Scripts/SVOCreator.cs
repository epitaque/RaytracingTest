
  
public class SVO {
	VoxelData vdata;

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

	ulong buildOctree(int x, int y, int z, int size, ulong descriptorIndex) {
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
	}

	uint constructChildDescriptor(ushort childPointer, byte validMask, byte leafMask) {
		return 
	}

}