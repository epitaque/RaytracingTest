using UnityEngine;

public class OctreeDebugger : MonoBehaviour {
	private Octree octree;
	public Transform debugInsert;
	public int insertSize;

	Vector4 currentChunkBounds;

	void Start() {
		octree = new Octree();
	}

	void Update()
    {
		currentChunkBounds = new Vector4(((int)debugInsert.position.x/insertSize) * insertSize, ((int)debugInsert.position.y / insertSize) * insertSize, ((int)debugInsert.position.z / insertSize) * insertSize, insertSize);
        if (Input.GetKeyDown(KeyCode.R))
        {
            print("Inserting that cube into the octree...");
			Chunk c = new Chunk();
			c.position = currentChunkBounds;
			c.size = (int)currentChunkBounds.w;
			octree.AddChunk(c);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            print("Deleting chunk from octree...");
			Chunk c = octree.FindChunk(currentChunkBounds);
			octree.RemoveChunk(c);
        }
    }

	void OnDrawGizmos() {
		if(!Application.isPlaying) { return; }
		Gizmos.color = new Color(1, 0, 0, 0.5f);
		Gizmos.DrawCube(new Vector3(currentChunkBounds.x, currentChunkBounds.y, currentChunkBounds.z) + (Vector3.one * currentChunkBounds.w) / 2, currentChunkBounds.w * Vector3.one);

		DrawOctreeGizmosRecursive(octree.root, 1);
	}

	public static void DrawOctreeGizmosRecursive(Node node, int depth) {
		if(node == null) { return; }

		Gizmos.color = UtilFuncs.SinColor(depth);
		Gizmos.DrawWireCube(node.position + (Vector3.one * node.size)/2, node.size * Vector3.one);
		for(int i = 0; i < 8; i++) {
			DrawOctreeGizmosRecursive(node.children[i], depth + 1);
		}
	}


}