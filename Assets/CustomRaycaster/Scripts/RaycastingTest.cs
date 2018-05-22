using System.Collections.Generic;
using UnityEngine;

namespace RT {
public class Node {
	public Node Parent;
	public Node[] Children;

	public Vector3 Min;
	public float Size;
	public bool IsLeaf;
	public int Depth;
	public bool ContainsSurface;
	public bool CompletelyFilled;
	public Color Color;
}

public class RaycastingTest : MonoBehaviour {
	[Range (0, 8)]
	[SerializeField]
	public int DrawDepth;
	
	[Range (0, 8)]
	[SerializeField]
	public int MaxLOD = 4;

	Node root;

	public void Start() {
		root = GenerateOctree((float x, float y, float z) => UtilFuncs.Sphere(x, y, z, 0.6f), MaxLOD);
		Debug.Log("Generated octree. ContainsSurface: " + root.ContainsSurface);
	}

	public Node GenerateOctree(UtilFuncs.Sampler sample, int maxDepth) {
		Node root = new Node();
		root.Min = new Vector3(-1, -1, -1);
		root.Size = 2f;
		root.Depth = 0;

		ConstructNode(root, maxDepth, sample);

		return root;
	}

	public void ConstructNode(Node node, int maxDepth, UtilFuncs.Sampler sample) {
		node.Color = UtilFuncs.SinColor(node.Depth);
		node.Color.a = 0.2f;

		if(node.Depth == maxDepth) {
			node.IsLeaf = true;
			Vector3 center = node.Min + (Vector3.one * node.Size) / 2f;
			node.ContainsSurface = sample(center.x, center.y, center.z) < 0 ? true : false;
			node.CompletelyFilled = node.ContainsSurface;
		}
		else {
			node.IsLeaf = false;
			node.Children = new Node[8];

			node.CompletelyFilled = true;

			for(int i = 0; i < 8; i++) {
				//Debug.Log("Created child with depth " + (node.Depth + 1));
				node.Children[i] = new Node();
				node.Children[i].Parent = node;
				node.Children[i].Size = node.Size / 2f;
				node.Children[i].Min = node.Min + Constants.vfoffsets[i] * (node.Size / 2f);
				node.Children[i].Depth = node.Depth + 1;

				ConstructNode(node.Children[i], maxDepth, sample);
				node.ContainsSurface |= node.Children[i].ContainsSurface;
				if(!node.Children[i].CompletelyFilled) {
					node.CompletelyFilled = false;
				}
			}

			if(!node.ContainsSurface) {
				node.Children = null;
				node.IsLeaf = true;
			}
			if(node.CompletelyFilled) { // All children have surface
				node.Children = null;
				node.IsLeaf = true;
			}
		}
	}

	public void OnDrawGizmos() {
		DrawNodeRecursive(root, 16f);
	}

	public void DrawNodeRecursive(Node node, float scale) {
		if(node == null) return;
		//Debug.Log("Drawing gizmos");

		Gizmos.color = node.Color;
		if(node.IsLeaf && node.ContainsSurface) {
			//Debug.Log("Drawing cube at depth" + node.Depth);
			if(node.Depth <= DrawDepth) {
				UnityEngine.Gizmos.DrawCube((node.Min + Vector3.one * (node.Size / 2f)) * scale, Vector3.one * node.Size*scale);

			}
		}
		else if(!node.IsLeaf) {
			for(int i = 0; i < 8; i++) {
				if(node.Children[i].ContainsSurface) {
					DrawNodeRecursive(node.Children[i], scale);

				}
			}
		}
	}
}
}


