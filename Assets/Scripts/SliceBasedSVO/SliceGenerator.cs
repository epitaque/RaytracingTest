using UnityEngine;

namespace RT.SL {
public static class SliceGenerator {


	// "level" is the number of jumps needed to get to root
	// Level 0 is 1x1x1 (1 bool)
	// Level 1 is 2x2x2 (16 bools)
	// Level 2 is 4x4x4 (64 bools), etc.

	// a slice bool returns true if the cube contains a surface
	public static bool[][,,] GetSlices(UtilFuncs.Sampler sample, int maxLevel) {
		// generate deepest level first. then downsample

		// i.e. if maxLevel = 2, then max resolution is 2^2 = 4
		int maxResolution = (int)Mathf.Pow(2, maxLevel);
		bool[][,,] slices = new bool[maxLevel + 1][,,];
		for(int i = maxLevel, res = maxResolution; i >= 0; res >>= 1, i--) {
			slices[i] = new bool[res,res,res];
		}

		float factor = 1f / (float)maxResolution;
		
		bool[] containsVoxel = new bool[maxLevel + 1];

		int[,] directions = { {0, 0, 1}, {0, 1, 0}, {1, 0, 0}, {0, 0, -1}, {0, -1, 0}, {-1, 0, 0} };
		int[,] offsets = {{0, 0, 0}, {0, 0, 1}, {0, 1, 0}, {0, 1, 1}, {1, 0, 0}, {1, 0, 1}, {1, 1, 0}, {1, 1, 1} };

		for(int i = 0; i < maxResolution * maxResolution * maxResolution; i++) {
			int x = 0, y = 0, z = 0;
			MortonUtil.MortonDecode((ulong)i, ref x, ref y, ref z);
			float density = sample((float)x * factor, (float)y * factor, (float)z * factor);

			bool hasAirAsNeighbor = false;
			for(int j = 0; j < 6 && !hasAirAsNeighbor; j++) {
				float density2 = sample((float)(x + directions[j,0]) * factor, 
										(float)(y + directions[j,1]) * factor, 
										(float)(z + directions[j,2]) * factor);
				hasAirAsNeighbor = density2 < 0;
			}

			slices[maxLevel][x,y,z] = (density > 0) && hasAirAsNeighbor;
		}

		for(int level = maxLevel - 1; level >= 0; level--) {
			int res = (int)Mathf.Pow(2, level);

			for(int x = 0; x < res; x++) {
				for(int y = 0; y < res; y++) {
					for(int z = 0; z < res; z++) {
						bool containsSurface = false;

						for(int i = 0; i < 8 && !containsSurface; i++) {
							int x2 = x * 2 + offsets[i,0];
							int y2 = y * 2 + offsets[i,1];
							int z2 = z * 2 + offsets[i,2];

							containsSurface = slices[level + 1][x2,y2,z2];
						}

						slices[level][x,y,z] = containsSurface;
					}
				}
			}
		}

		return slices;
	}
}
}