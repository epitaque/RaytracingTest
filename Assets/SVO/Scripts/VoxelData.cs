using UnityEngine;

namespace RT {
public class VoxelData {
	public bool[][] Data;
	public int Resolution;
	public int Levels;

	public VoxelData(UtilFuncs.Sampler sample, int resolution) {
		Debug.Assert((resolution > 0) && ((resolution & (resolution - 1)) == 0));

		Resolution = resolution;
		Levels = (int)Mathf.Log(2, resolution);


		Data = new bool[Levels][];
		int currentRes = resolution;
		for(int lvl = 0; lvl < Levels; lvl++) {
			Data[lvl] = new bool[currentRes*currentRes*currentRes];
			currentRes >>= 1;
		}
		FillData(sample);
	}
	public void FillData(UtilFuncs.Sampler sampler) {
		int[] indices = new int[Levels];
		int index;
		for(int z = 0; z < Resolution; z++) {
			for(int y = 0; y < Resolution; y++) {
				for(int x = 0; x < Resolution; x++) {
					index = GetIndex(x, y, z, Resolution);
					Data[0][index] = sampler(x, y, z) < 0;

					if(Data[0][index]) {
						int tempres = Resolution / 2;
						int divider = 2;

						for(int level = 1; level < Levels; level++) {
							index = GetIndex(x/divider, y/divider, z/divider, tempres);

							Data[level][index] = true;

							tempres /= 2;
							divider *= 2;
						}
					}
				}
			}
		}
	}
	public bool CubeContainsVoxel(int x, int y, int z, int size) {
		int bitpos = 0;
		int s2 = size;
		while(s2 != 0) {
			bitpos++; s2 >>= 1;
		}
		int lod = Levels - bitpos - 1;


		return Data[lod][GetIndex(x/size, y/size, z/size, 1 << lod)];
	} 

	public int GetIndex(int x, int y, int z, int res) {
		return z * res * res + y * res + x;
	}
}
}
