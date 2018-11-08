using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RT.CS {
public class NaiveCreator : CompactSVO.CompactSVOCreator {
	public List<uint> Create(UtilFuncs.Sampler sample, int maxLevel) {
		Node root = new Node(new Vector3(-1, -1, -1), 2, 1, false);
		BuildTree(root, 1, sample, maxLevel);
		List<uint> nodes = CompressSVO(root);
		return nodes;
	}
	/*
		Tree building methods

		Constructs subtree from node.
		Will return null if node does not contain surface.
		Will return node if node contains surface.
	 */
	private Node BuildTree(Node node, int level, UtilFuncs.Sampler sample, int maxLevel) {
		// Node is leaf. Determine if within surface. If so, return node.
		if(node.Leaf) {
			Vector3 p = node.Position + Vector3.one * (float)node.Size / 2;
			if(sample(p.x, p.y, p.z) <= 0 && IsEdge(node, sample)) {
				return node;
			}
		}


		// Node is not leaf. Construct 8 children. If any of them intersect surface, return node.
		else {
			bool childExists = false;
			int numLeaves = 0;
			node.Children = new Node[8];
			double half = node.Size/2d;

			for(int i = 0; i < 8; i++) {
				Node child = new Node(node.Position + Constants.vfoffsets[i] * (float)(half), half, level + 1, level + 1 == maxLevel);
				node.Children[i] = BuildTree(child, level + 1, sample, maxLevel);
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
	private bool IsEdge(Node node, UtilFuncs.Sampler sample) {
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
		compressedNodes.Add(0);
        CompressSVOAux(root, 0, compressedNodes);
        return compressedNodes;
    } 

    private void CompressSVOAux(Node node, int nodeIndex, List<uint> compressedNodes) {
        if(node == null || node.Leaf) { return; }

		uint childPointer = 0;
        uint validMask = 0;
        uint leafMask = 0;

        for(int childNum = 0; childNum < 8; childNum++) { 
            uint bit = (uint)1 << childNum;
            if(node.Children[childNum] != null) {
                validMask |= bit;
                if(node.Children[childNum].Leaf) {
                    leafMask |= bit;
                }
				else {
					if(childPointer == 0) {
						childPointer = (uint)compressedNodes.Count;
					}
					compressedNodes.Add(0);
				}
            }
        }

		int childPointerClone = (int)childPointer;
		for(int childNum = 0; childNum < 8; childNum++) {
			if(node.Children[childNum] != null && !node.Children[childNum].Leaf) {
				CompressSVOAux(node.Children[childNum], childPointerClone++, compressedNodes);
			}
		}

        uint result = childPointer | (validMask << 16) | (leafMask << 24);
		compressedNodes[nodeIndex] = result;
    }

	static NaiveCreator() {
		TestSVOCompaction();
	}

	public static void TestSVOCompaction() {
		/*NaiveCreator creator = new NaiveCreator();
		List<uint> nodes = new List<uint>();
		Node root = new Node(new Vector3(-1, -1, -1), 2, 1, false);
		creator.BuildTree(root, 1, SampleFunctions.functions[(int)SampleFunctions.Type.Sphere], 4);
		nodes = creator.CompressSVO(root);
		string output = "NaiveCreator SVO Compaction Test\n";
		output += "Original Hierarchy:\n" + root.StringifyHierarchy() + "\n\n";
		output += "Compressed:\n" + string.Join("\n", nodes.ConvertAll(code => new ChildDescriptor(code)));
		Debug.Log(output); */
	}
}
}