using System.Collections.Generic;
using UnityEngine;

namespace RT.SL {

public class ChildDescriptor {
	public ushort childPointer;
	public byte validMask;
	public byte nonLeafMask;

	public ChildDescriptor(int code) {
		this.childPointer = (ushort)(code);
		this.validMask = (byte)((code >> 16) & 255);
		this.nonLeafMask = (byte)((code >> 24) & 255);
	}

	public ChildDescriptor(ushort childPointer, byte validMask, byte nonLeafMask) {
		this.childPointer = childPointer;
		this.validMask = validMask;
		this.nonLeafMask = nonLeafMask;
	}

	public bool Valid(int childNum) { return (validMask & (1 << childNum)) != 0; }
	public bool Leaf(int childNum) { return (nonLeafMask & (1 << childNum)) == 0; }

	public int GetCode() {
		return (int)(childPointer | (validMask << 16) | (nonLeafMask << 24));
	}

	public static int ToCode(int childPointer, int validMask, int nonLeafMask) {
		return (int)(childPointer | (validMask << 16) | (nonLeafMask << 24));
	}

	public override string ToString() {
		return "[ChildDescriptor childPointer: " + childPointer + ", validMask: " + System.Convert.ToString(validMask, 2).PadLeft(8, '0') + ", nonLeafMask: " + System.Convert.ToString(nonLeafMask, 2).PadLeft(8, '0') + "]";
	}
}

public static class SliceBasedSVO {

// example beginning of an octree
// CD: [ChildDescriptor childPointer: 1, validMask: 11111111, nonLeafMask: 11111111], Normal: v(-0.6, -0.6, -0.6)1001000001100001(36961)
// CD: [ChildDescriptor childPointer: 9, validMask: 01111111, nonLeafMask: 01111111], Normal: v(-0.6, -0.6, -0.6)1001000001100001(36961)
// CD: [ChildDescriptor childPointer: 630, validMask: 10111111, nonLeafMask: 10111111], Normal: v(0.6, -0.6, -0.6)1011000010011110(45214)
// CD: [ChildDescriptor childPointer: 1251, validMask: 11011111, nonLeafMask: 11011111], Normal: v(-0.6, 0.6, -0.6)1000111110100010(36770)

// steps to add a slice:
// 1. create a list of new child descriptors based on the slice and append them to the block
// 2. adjust the pointers of the child descriptors in the block

// the number of new child descriptors is numActiveVoxelsInSliceResHalved
// you know exactly what all those cds look like without any info outside of the slice. all that's needed is the slice
// once you've made those cds, you must adjust the cds in the block accordingly
// iterate through the last n cds
// 


	// slice tells whether or not a space contains a voxel
	public static void AddSlice(List<int> block, bool[,,] slice) {
		// do a depth first traversal of the block
		AddSliceAux(0, 0, 0, 0, block, slice, (int)Mathf.Log(slice.GetLength(0), 2) - 1);
	}

	// can be optimized? to take one parameter instead of x y z parameters
	// sliceDepth is number of levels that have to be jumped to get to the child descriptor (i.e. log(slice.length(0), 64))
	// slice is a multidimensional array of booleans that tell whether or not there is a voxel x, y, z at sliceDepth
	// DO A DEPTH FIRST TRAVERSAL OF THE BLOCK
	public static void AddSliceAux(int cdIndex, int x, int y, int z, List<int> block, bool[,,] slice, int sliceDepth) {
		//1. Create a list of child descriptors.
		ChildDescriptor descriptor = new ChildDescriptor(block[cdIndex]);

		int pointer = descriptor.childPointer;
		for(int i = 0; i < 8; i++) {
			if(descriptor.Valid(i)) {
				if(!descriptor.Leaf(i)) {
					Vector3Int offset = Constants.ioffsets[i];
					AddSliceAux(pointer, (x + offset.x) << 1, (y + offset.y) << 1, (z + offset.z) << 1, block, slice, sliceDepth - 1);
					pointer++;
				}
				else if(sliceDepth == 1) {
					// we hit a child of a child descriptor that's a leaf. let's see if there's any more detail to add here
					for(int child = 0; child < 8; child++) {
						Vector3Int offset = Constants.ioffsets[child] * 2;
						byte validMask = 0;
						for(int j = 0; j < 8; j++) {
							Vector3Int pos = new Vector3Int(x, y, z) + offset + Constants.ioffsets[j];
							if(slice[pos.x, pos.y, pos.z]) {
								validMask++;
							}
							validMask <<= 1;
						}
						if(validMask != 0) {
							if(descriptor.childPointer == 0) {
								descriptor.childPointer = (ushort)block.Count;
							}
							descriptor.nonLeafMask |= (byte)(1 << i);
							block[cdIndex] = descriptor.GetCode();

							ChildDescriptor newDescriptor = new ChildDescriptor(0, validMask, 0);
							block.Add(newDescriptor.GetCode());
						}
					}
				}
				else {
					Debug.LogError("Trying to add detail where there's already detail...");
				}
			}
		}
	}
}
}
