using System.Collections.Generic;
using UnityEngine;

public class Clipmap : MonoBehaviour {
	public int minimumChunkSize = 16;
	public int radius = 4;
	public int lods = 4;

	private Chunk[][, , ] chunks;
	private Chunk[] chunkArray;
	private int arrayMin;
	private int arrayMax;
	private int ChunkArraySize => radius * 4;
	private bool initialized;
	private Vector3Int previousPosition;
	private Octree octree;

	void Start () {
		initialized = false;
		arrayMin = 0;
		arrayMax = 0;
		chunks = new Chunk[lods][,,];
		octree = new Octree();
		for (int i = 0; i < lods; i++) {
			chunks[i] = new Chunk[ChunkArraySize, ChunkArraySize, ChunkArraySize];
		}
		previousPosition = Vector3Int.one * int.MinValue;

		chunkArray = new Chunk[ChunkArraySize * ChunkArraySize * ChunkArraySize * 500];
		Update();
	}

	public void Update () {
		DoChunkUpdate ();
	}

	public void DoChunkUpdate () {
		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch ();
		sw.Start ();

		Vector3 pos = transform.position;
		float updateTime = Time.realtimeSinceStartup;

		int numChunksAdded = 0;
		int minCoord = -radius + 1;
		int maxCoord = radius + 1;

		Vector3Int prevMin = Vector3Int.zero;
		Vector3Int prevMax = Vector3Int.zero;
		int chunkArraySize = 4 * radius;

		for (int lod = 0; lod < lods; lod++) {
			int scale = (int) Mathf.Pow (2, lod);
			int chunkSize = minimumChunkSize * scale;
			int snapSize = lod == (lods - 1) ? chunkSize : chunkSize * 2;

			Vector3Int snapped = new Vector3Int (
				(int) Mathf.Floor ((pos.x + chunkSize / 2) / snapSize) * snapSize,
				(int) Mathf.Floor ((pos.y + chunkSize / 2) / snapSize) * snapSize,
				(int) Mathf.Floor ((pos.z + chunkSize / 2) / snapSize) * snapSize);

			if (lod == 0) {
				if (initialized && snapped == previousPosition) {
					return; // snapped position didn't change, so no new chunks will be added
				}
				previousPosition = snapped;
				initialized = true;
			}

			Vector3Int min = Vector3Int.one * ((minCoord - 1) * chunkSize) + snapped;
			Vector3Int max = Vector3Int.one * ((maxCoord - 2) * chunkSize) + snapped;

			Vector3Int chunkPos = Vector3Int.zero;
			for (int x = minCoord; x < maxCoord; x++) {
				chunkPos.x = ((x - 1) * chunkSize) + snapped.x;
				bool xInBounds = chunkPos.x >= prevMin.x && chunkPos.x <= prevMax.x;

				for (int y = minCoord; y < maxCoord; y++) {
					chunkPos.y = ((y - 1) * chunkSize) + snapped.y;
					bool yInBounds = chunkPos.y >= prevMin.y && chunkPos.y <= prevMax.y;

					for (int z = minCoord; z < maxCoord; z++) {
						chunkPos.z = ((z - 1) * chunkSize) + snapped.z;
						bool zInBounds = chunkPos.z >= prevMin.z && chunkPos.z <= prevMax.z;

						if (lod != 0 && (xInBounds && yInBounds && zInBounds)) {
							continue;
						}

						int arrx = ((chunkPos.x / chunkSize) % chunkArraySize + chunkArraySize) % chunkArraySize;
						int arry = ((chunkPos.y / chunkSize) % chunkArraySize + chunkArraySize) % chunkArraySize;
						int arrz = ((chunkPos.z / chunkSize) % chunkArraySize + chunkArraySize) % chunkArraySize;

						Vector4Int key = new Vector4Int (arrx, arry, arrz, lod);

						if (chunks[lod][arrx, arry, arrz] == null) {
							Chunk chunk = new Chunk ();
							chunk.position = chunkPos;
							chunk.key = key;
							chunk.lod = lod;
							chunk.creationTime = updateTime;
							chunk.index = arrayMax++;
							chunk.size = chunkSize;

							numChunksAdded++;
							chunks[lod][arrx, arry, arrz] = chunk;
							chunkArray[chunk.index] = chunk;
							octree.AddChunk(chunk);
						} else {
							chunks[lod][arrx, arry, arrz].creationTime = updateTime;
						}
					}
				}
			}

			prevMax = max;
			prevMin = min;
		}

		double ElapsedMilliseconds1 = sw.Elapsed.TotalMilliseconds;
		sw.Restart ();

		RemoveOldChunks (updateTime);
		UpdateArrayMin ();

		double ElapsedMilliseconds2 = sw.Elapsed.TotalMilliseconds;
		Debug.LogFormat ("Chunk Update: S1 {0} ms, S2 {1} ms, Total {2}ms ({3} chunks added)",
			ElapsedMilliseconds1, ElapsedMilliseconds2, (ElapsedMilliseconds1 + ElapsedMilliseconds2), numChunksAdded);
		sw.Stop ();
	}

	void RemoveOldChunks (float updateTime) {
		for (int i = arrayMin; i < arrayMax; i++) {
			Chunk c = chunkArray[i];
			if (c != null) {
				if (c.creationTime != updateTime) {
					chunkArray[i] = null;
					chunks[c.key.w][c.key.x, c.key.y, c.key.z] = null;
					octree.RemoveChunk(c);
				}
			}
		}
	}

	void UpdateArrayMin () {
		for (int i = arrayMin; i < arrayMax; i++) {
			if (chunkArray[i] != null) {
				arrayMin = i;
				break;
			}
		}
	}

	void OnDrawGizmos () {
		if (!Application.isPlaying) {
			return;
		}
		// Debug.Log ("Drawing gizmos");

		// for (int i = arrayMin; i < arrayMax; i++) {
		// 	if (chunkArray[i] != null) {
		// 		Chunk chunk = chunkArray[i];
		// 		Gizmos.color = UtilFuncs.SinColor (chunk.lod * 3f);
		// 		Gizmos.DrawSphere (chunk.position + (chunk.size / 2) * Vector3.one, chunk.lod + 0.5f);
		// 	}
		// }

		OctreeDebugger.DrawOctreeGizmosRecursive(octree.root, 1);
	}

}