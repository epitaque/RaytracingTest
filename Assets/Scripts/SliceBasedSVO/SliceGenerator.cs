using UnityEngine;

namespace RT.SL {
public static class SliceGenerator {
	// "level" is the number of jumps needed to get to root
	// Level 0 is 1x1x1 (1 bool)
	// Level 1 is 2x2x2 (16 bools)
	// Level 2 is 4x4x4 (64 bools), etc.
	public static bool[][,,] GetSlices(UtilFuncs.Sampler sample, int maxLevel) {
		// generate deepest level first. then downsample

		// i.e. if maxLevel = 2, then max resolution is 2^2 = 4
		int maxResolution = (int)Mathf.Pow(2, maxLevel);
		bool[][,,] slices = new bool[maxLevel + 1][,,];
		for(int i = maxLevel, res = maxResolution; i >= 0; res >>= 1, i--) {
			slices[i] = new bool[res,res,res];
		}

		float factor = 1f / (float)maxResolution;
		
		int x = 0, y = 0, z = 0;
		bool[] containsVoxel = new bool[maxLevel + 1];

		for(int i = 0; i < maxResolution * maxResolution * maxResolution; i++) {
			MortonUtil.MortonDecode((ulong)i, ref x, ref y, ref z);
			float density = sample((float)x * factor, (float)y * factor, (float)z * factor);
			slices[maxLevel - 1][x,y,z] = density > 0;
			containsVoxel[maxLevel - 2]++;

			int j = i;
			int depth2 = maxLevel - 2;
			x /= 8; y /= 8; z /= 8;
			while(j % 8 == 0) {
				if(voxelCounts[depth2] > 1) {
					slices[depth2][x,y,z] = true;
					voxelCounts[depth2 - 1]++;
				}
				if(voxelCounts[depth2] == 8) {
					// erase
				}
				
				voxelCounts[depth2] = 0;
				j /= 8;
				depth2--;
				x /= 8; y /= 8; z /= 8;
			}
		}
		// for(int x = 0; x < dim; x++) {
		// 	for(int y = 0; y < dim; y++) {
		// 		for(int z = 0; z < dim; z++) {

		// 			slices[depth - 1][x,y,z] = density > 0;
		// 		}
		// 	}
		// }

		// for(int i = depth - 2; i >= 0; i--) {
		// 	dim = (int)Mathf.Pow(2, i + 1);
		// 	slices[i] = new bool[dim,dim,dim];

		// 	for(int x = 0; x < dim; x++) {
		// 		for(int y = 0; y < dim; y++) {
		// 			for(int z = 0; z < dim; z++) {

		// 			}
		// 		}
		// 	}

		// }

		return slices;
	}
}
}