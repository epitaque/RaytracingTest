using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RT.CS {
public class NaiveCreator : CompactSVO.CompactSVOCreator {
	public List<uint> Create(UtilFuncs.Sampler sample, int maxLevel) {
		List<uint> nodes = new List<uint>();
		Node root = new Node(new Vector3(-1, -1, -1), 2, 1, false);
		BuildTree(root, 1, sample, maxLevel);

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
        CompressSVOAux(root, compressedNodes);
        return compressedNodes;
    } 

    private uint CompressSVOAux(Node node, List<uint> compressedNodes) {
        if(node == null || node.Leaf) { return 0; }

		uint thisIndex = (uint)compressedNodes.Count;
        compressedNodes.Add(0);

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
            		uint childIndex = CompressSVOAux(node.Children[childNum], compressedNodes);
					if(childPointer == 0) childPointer = childIndex;
				}
            }
        }

        uint result = childPointer | (validMask << 16) | (leafMask << 24);
		compressedNodes[(int)thisIndex] = result;

		return thisIndex;
    }

	static NaiveCreator() {
		TestSVOCompaction();
	}

	public static void TestSVOCompaction() {
		NaiveCreator creator = new NaiveCreator();
		List<uint> nodes = new List<uint>();
		Node root = new Node(new Vector3(-1, -1, -1), 2, 1, false);
		creator.BuildTree(root, 1, SampleFunctions.functions[(int)SampleFunctions.Type.FlatGround], 3);
		nodes = creator.CompressSVO(root);
		string output = "SVO Compaction Test\n";
		output += "Hierarchy: " + root.StringifyHierarchy() + "\n\n";
		output += "Compressed: " + string.Join(", ", nodes.ConvertAll(code => new ChildDescriptor(code)));
		Debug.Log(output);
	}
}
}