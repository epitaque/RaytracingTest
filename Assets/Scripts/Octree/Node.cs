using UnityEngine;

public class Node {
	public Node[] children;
	public Node parent;
	public byte childMask;
	public byte index; // index of this node in parent
	public Vector3 position;
	public int size;
	public Chunk chunk; // null if not a leaf
	public bool IsLeaf => chunk == null;

	public Node(Vector3 position, int size) {
		this.position = position;
		this.size = size;
		this.children = new Node[8];
		childMask = 0;
	}
}