using UnityEngine;
using System.Collections.Generic;

/*
    Class that stores the SVO in the following format
    child pointer | valid mask | leaf mask
        16			   8			8

    Child pointer:
     - Points to the first child

    Status of child in given slot:
     - Neither bit is set: the slot is not intersected by a surface, and is therefore empty.
     - The bit in valid mask is set: the slot contains a non-leaf voxel that is subdivided further.
     - Both bits are set: the slot contains a leaf voxel.

 */
namespace RT {
public class CompactSVO : SVO {
    private List<uint> svo;
	private UtilFuncs.Sampler sample;
    private int maxLevel;

	public class Node : SVONode {
		public Node(Vector3 position, double size, int level, bool leaf) {
			Position = position;
			Size = size;
			Level = level;
			Leaf = leaf;
		}

		public Node[] Children;
	}

	public CompactSVO(UtilFuncs.Sampler sample, int maxLevel) {
		Create(sample, maxLevel);
	}

	public void Create(UtilFuncs.Sampler sample, int maxLevel) {
		this.maxLevel = maxLevel;
		this.sample = sample;
		BuildTree();
	}

	private void BuildTree() {
		Node root = new Node(new Vector3(-1, -1, -1), 2, 1, false);
        // First, build tree conventionally, as it was done in NaiveSVO.cs
		BuildTreeAux(root, 1);
        // Then, convert this tree to a compact format
        svo = new List<uint>();
	}

	/*
		Tree building methods

		Constructs subtree from node.
		Will return null if node does not contain surface.
		Will return node if node contains surface.
	 */
	private Node BuildTreeAux(Node node, int level) {
		// Node is leaf. Determine if within surface. If so, return node.
		if(node.Leaf) {
			float s = sample(node.Position.x + (float)(node.Size / 2), node.Position.y + (float)(node.Size / 2), node.Position.z + (float)(node.Size / 2));
			if(s <= 0) {
				bool isEdge = IsEdge(node);
				if(isEdge) {
					return node;
				}
			}
		}

		// Node is not leaf. Construct 8 children. If any of them intersect surface, return node.
		else {
			bool childExists = false;
			int numLeaves = 0;
			node.Children = new Node[8];

			for(int i = 0; i < 8; i++) {
				double half = node.Size/2d;
				Node child = new Node(node.Position + Constants.vfoffsets[i] * (float)(half), half, level, level + 1 == maxLevel);
				node.Children[i] = BuildTreeAux(child, level + 1);
				if(node.Children[i] != null) {
					childExists = true;
					if(node.Children[i].Leaf) {
						numLeaves++;
					}
				}
			}

			if(childExists) {
				return node;
			}
		}
		return null;
	}

	// Given that node resides inside the surface, detects if it's an edge voxel (has air next to it)
	private bool IsEdge(Node node) {
		foreach(Vector3 direction in Constants.vdirections) {
			Vector3 pos = node.GetCenter() + direction * (float)(node.Size);
			float s = sample(pos.x, pos.y, pos.z);
			if(s > 0) {
				return true;
			}
		}
		return false;
	}

    private List<uint> CompressSVO(Node root) {
        List<uint> compressedNodes = new List<uint>();
        CompressSVOAux(root, compressedNodes);
        return compressedNodes;
    } 

    private void CompressSVOAux(Node node, List<uint> compressedNodes) {
        if(node == null || node.Leaf) { return; }

        uint childPointer = (uint)compressedNodes.Count + 1;
        uint validMask = 0;
        uint leafMask = 0;
        for(int childNum = 0; childNum < 8; childNum++) {
            uint bit = (uint)1 << childNum;
            if(node.Children[childNum] != null) {
                validMask |= bit;
                if(node.Children[childNum].Leaf) {
                    leafMask |= bit;
                }
            }
        }
        uint result = childPointer + (validMask << 16) + (leafMask << 24);
        compressedNodes.Add(result);
        for(int childNum = 0; childNum < 8; childNum++) {
            CompressSVOAux(node.Children[childNum], compressedNodes);
        }
    }

    public List<SVONode> Trace(UnityEngine.Ray ray) {
        return null;
    }
    public List<SVONode> GetAllNodes() {
        return null;
    }

    public void DrawGizmos(float scale) {
        
    }
}
}
