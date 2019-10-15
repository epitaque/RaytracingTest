using UnityEngine;

public class Octree {
	public Node root;
	
	public Octree() {
		root = new Node(Vector3.zero, 16); // root is at (0, 0, 0) with size 16
	}

	// chunk may be outside of root.
	public void AddChunk(Chunk chunk) {
		Vector3 newPosition = Vector3.zero;
		if(!ChunkInBounds(root, chunk, ref newPosition)) {
			Node newRoot = new Node(root.position + newPosition * (root.size), root.size * 2);
			byte index = (byte)((-(int)newPosition.x) + (-(int)newPosition.y) * 2 + ( -(int)newPosition.z) * 4);
			newRoot.children[index] = root;
			newRoot.childMask |= (byte)(1 << index);
			root.index = index;
			root.parent = newRoot;
			root = newRoot;	
			AddChunk(chunk);
		} else {
			AddChunk(chunk, root);
		}
	}

	// Precondition: Chunk's bounds are within in "parent"
	public void AddChunk(Chunk chunk, Node parent) {
		if(parent.size != chunk.size) {
			Vector3 center = parent.position + (Vector3.one * parent.size / 2);
			byte index = 0;
			Vector3 newChildPosition = parent.position;
			if(chunk.position.x >= center.x) {
				index |= 1 << 0;
				newChildPosition.x += parent.size / 2;
			}
			if(chunk.position.y >= center.y) {
				index |= 1 << 1;
				newChildPosition.y += parent.size / 2;
			}
			if(chunk.position.z >= center.z) {
				index |= 1 << 2;
				newChildPosition.z += parent.size / 2;
			}

			if(parent.children[index] == null) {
				parent.children[index] = new Node(newChildPosition, parent.size / 2);
				parent.children[index].parent = parent;
				parent.children[index].index = index;
				parent.childMask |= (byte)(1 << index);
			}
			AddChunk(chunk, parent.children[index]);
		} else {
			if(parent.chunk != null) {
				Debug.LogError("Trying to add a chunk to a node that already has a chunk!");
			}
			chunk.node = parent;
			parent.chunk = chunk;
		}
	}

	public void RemoveChunk(Chunk chunk) {
		RemoveNode(chunk.node);
	}

	public void RemoveNode(Node node) {
		Debug.Assert(node.childMask == 0, "Trying to remove a node that has children! This should never happen.");
		if(node.parent == null) {
			Debug.Log("Reached node with no parent.");
		}
		node.parent.children[node.index] = null;
		byte test = (byte)(~(1 << node.index));
		node.parent.childMask &= test;
		if(node.parent.childMask == 0) {
			RemoveNode(node.parent);
		} else if(node.parent == root) {
			Debug.Log("Reached root."); 
			SimplifyOctree();
		}
	}

	public void SimplifyOctree() {
		if(root.childMask == 1 || root.childMask == 2 || root.childMask == 4 || root.childMask == 8
		|| root.childMask == 16 || root.childMask == 32 || root.childMask == 64 || root.childMask == 128) {
			root = root.children[MaskToIndex(root.childMask)];
			root.parent = null;
			root.index = 0;
			SimplifyOctree();
		}

		
	}

	public Chunk FindChunk(Vector4 position) {
		return FindChunk(position, root);
	}

	public Chunk FindChunk(Vector4 position, Node node) {
		if(node.size != position.w) {
			Vector3 center = node.position + (Vector3.one * node.size / 2);
			byte index = 0;
			Vector3 newChildPosition = node.position;
			if(position.x >= center.x) {
				index |= 1 << 0;
				newChildPosition.x += node.size / 2;
			}
			if(position.y >= center.y) {
				index |= 1 << 1;
				newChildPosition.y += node.size / 2;
			}
			if(position.z >= center.z) {
				index |= 1 << 2;
				newChildPosition.z += node.size / 2;
			}

			if(node.children[index] == null) {
				Debug.Log("Couldn't find a chunk there...");
			}
			return FindChunk(position, node.children[index]);
		} else {
			if(node.chunk == null) {
				Debug.LogError("That node doesn't have a chunk...");
			}
			return node.chunk;
		}

	}

	// newPosition will be the suggested relative coordinates of the new root. The octree should grow in the direction of the chunk.
	public bool ChunkInBounds(Node node, Chunk chunk, ref Vector3 newPosition) {
		bool result = true;
		if(chunk.position.x < node.position.x) {
			newPosition.x -= 1;
			result = false;
		}
		if(chunk.position.y < node.position.y) {
			newPosition.y -= 1;
			result = false;
		}
		if(chunk.position.z < node.position.z) {
			newPosition.z -= 1;
			result = false;
		}

		return result &&
		  !(chunk.position.x + chunk.size > node.position.x + node.size ||
		    chunk.position.y + chunk.size > node.position.y + node.size ||
		    chunk.position.z + chunk.size > node.position.z + node.size);
	}

	public static byte MaskToIndex(byte mask) {
		byte index = 0;
		while(mask > 1) {
			index++;
			mask >>= 1;
		}
		return index;
	}
	public static byte IndexToMask(byte index) {
		return (byte)(1 << index);
	}

	
}