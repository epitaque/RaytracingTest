using UnityEngine;

public class VoxelData {
	public bool[][] Data;
	public int Resolution;
	public int Levels;

	public VoxelData(UtilFuncs.Sampler sample, int resolution) {
		Debug.Assert((resolution > 0) && ((resolution & (resolution - 1)) == 0));

		Resolution = resolution;
		Levels = (int)Mathf.Log(2, resolution) - 1;

		Data = new bool[Levels][];
		int currentRes = resolution;
		for(int lvl = 0; lvl < Levels; lvl++) {
			Data[lvl] = new bool[currentRes*currentRes*currentRes];
			currentRes >>= 1;
		}
		FillData(sample);
	}
	public void FillData(UtilFuncs.Sampler sampler) {
		int index = 0;
		int[] indices = new int[Levels];
		for(int z = 0; z < Resolution; z++) {
			for(int y = 0; y < Resolution; y++) {
				for(int x = 0; x < Resolution; x++) {
					Data[0][index] = sampler(x, y, z) < 0;
					index++;
					int divider = 2;
					for(int i = 1; i < Levels; i++) {
						if(index % divider == 0) {
							indices[i]++;
						}
						divider *= 2;
					}
				}
			}
		}
		for(int level = 0;)
	}
}