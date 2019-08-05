using UnityEngine;

public static class AttributeCompressor {
	public class ColorNormal {
		public Color color;
		public Vector3 normal;
	}
	
	// Returns 6 32-bit words that stores colors and normals of 16 voxels
	// See page 9 of NVIDIA SVO paper
	public static int[] compressAttributes(ColorNormal[] voxels) {
		int[] compressedWords = {0, 0, 0, 0, 0, 0};

		// First, compress all the colors. Choose one from first 8 voxels and one from second 8 voxels

		Color[] colors = new Color[2];

		for(int voxel = 0; voxel < 8; voxel++) {
			if(voxels[voxel] != null) colors[0] = voxels[voxel].color;
		}

		for(int voxelSet = 0; voxelSet < 2; voxelSet++) {
			for(int voxel = 0; voxel < 8; voxel++) {
				//if()
			}
		}

		return compressedWords;
	}

	public static ColorNormal[] decompressAttributes(int[] attributes) {
		ColorNormal[] voxels = new ColorNormal[16];

		return voxels;
	}
}