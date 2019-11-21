using UnityEngine;

namespace RT.SL {
public static class SliceGenerator {
	public static bool[][,,] GetSlices(UtilFuncs.Sampler sample, int depth) {
		// generate deepest depth first. then downsample
		int dim = (int)Mathf.Pow(2, depth);
		bool[][,,] slices = new bool[depth][,,];

		slices[depth - 1] = new bool[dim,dim,dim];
		float factor = 1f / (float)dim;
		
		int x = 0, y = 0, z = 0;
		int[] voxelCounts = new int[depth - 1];

		for(int i = 0; i < dim * dim * dim; i++) {
			MortonUtil.MortonDecode((ulong)i, ref x, ref y, ref z);
			float density = sample((float)x * factor, (float)y * factor, (float)z * factor);
			slices[depth - 1][x,y,z] = density > 0;
			voxelCounts[depth - 2]++;

			int j = i;
			int depth2 = depth - 2;
			x /= 8; y /= 8; z /= 8;
			while(j % 8 == 0) {
				if(voxelCounts[depth2] > 1) {
					slices[depth2][x,y,z] = true;
					voxelCounts[depth2 - 1]++;
				}
				if(voxelCounts[depth2] == 8) {
					// er
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

		for(int i = depth - 2; i >= 0; i--) {
			dim = (int)Mathf.Pow(2, i + 1);
			slices[i] = new bool[dim,dim,dim];

			for(int x = 0; x < dim; x++) {
				for(int y = 0; y < dim; y++) {
					for(int z = 0; z < dim; z++) {

					}
				}
			}

		}

		return slices;
	}
}
}