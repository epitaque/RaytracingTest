using UnityEngine;
using System.Collections.Generic;

namespace RT {
public class NaiveSVO : SVO {
	private int maxLevel;
	private UtilFuncs.Sampler sample;
	private Node root;

	public class Node {
		public Vector3 position;
		public double size;
		public bool leaf;
		public int level;

		public Node(Vector3 position, double size, int level, bool leaf) {
			this.position = position;
			this.size = size;
			this.leaf = leaf;
			this.level = level;
		}

		public Vector3 GetCenter() {
			return position + Vector3.one * (float)(size/2d);
		}

		public ColoredBox GetDebugBox() {
			ColoredBox box = new ColoredBox();
			box.Center = position + Vector3.one * (float)(size / 2d);
			box.Color = UtilFuncs.SinColor(level * 2f);
			box.Color.a = 0.2f;
			box.Size = Vector3.one * (float)size;
			return box;
		}

		public override string ToString() {
			return "[Node, Position " + position + ", Size: " + size + ", Leaf: " + leaf + ", Level: " + level + "]";
		}

		public Node[] children;
	}

	public NaiveSVO(UtilFuncs.Sampler sample, int maxLevel) {
		Create(sample, maxLevel);
	}

	public void Create(UtilFuncs.Sampler sample, int maxLevel) {
		this.maxLevel = maxLevel;
		this.sample = sample;
		BuildTree();
	}

	public void BuildTree() {
		root = new Node(new Vector3(-1, -1, -1), 2, 1, false);
		BuildTreeAux(root, 1);
	}

	/*
		Tree building methods

		Constructs subtree from node.
		Will return null if node does not contain surface.
		Will return node if node contains surface.
	 */
	public Node BuildTreeAux(Node node, int level) {
		// Node is leaf. Determine if within surface. If so, return node.
		if(node.leaf) {
			float s = sample(node.position.x + (float)(node.size / 2), node.position.y + (float)(node.size / 2), node.position.z + (float)(node.size / 2));
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
			node.children = new Node[8];

			for(int i = 0; i < 8; i++) {
				double half = node.size/2d;
				Node child = new Node(node.position + Constants.vfoffsets[i] * (float)(half), half, level, level + 1 == maxLevel);
				node.children[i] = BuildTreeAux(child, level + 1);
				if(node.children[i] != null) {
					childExists = true;
					if(node.children[i].leaf) {
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
	public bool IsEdge(Node node) {
		foreach(Vector3 direction in Constants.vdirections) {
			Vector3 pos = node.GetCenter() + direction * (float)(node.size);
			float s = sample(pos.x, pos.y, pos.z);
			if(s > 0) {
				return true;
			}
		}
		return false;
	}

	/*
		Tracing methods
	 */

	public Vector4[] Trace(UnityEngine.Ray ray) {
		List<Node> intersectedNodes = TraceAux(ray);
		Vector4[] intersectedNodesArray = new Vector4[intersectedNodes.Count];
		for(int i = 0; i < intersectedNodes.Count; i++) {
			Node n = intersectedNodes[i];
			intersectedNodesArray[i] = new Vector4(n.position.x, n.position.y, n.position.z, (float)n.size);
		}
		return intersectedNodesArray;
	}

	private List<Node> TraceAux(UnityEngine.Ray ray) {
		List<Node> intersectedNodes = new List<Node>();
		RayStep(root, ray.origin, ray.direction, intersectedNodes);
		return intersectedNodes;
	}

	private void RayStep(Node node, Vector3 rayOrigin, Vector3 rayDirection, List<Node> intersectedNodes) 
	{ 
		Vector3 nodeMax = node.position + Vector3.one * (float)node.size;
		float  tx0 = (node.position.x - rayOrigin.x) / rayDirection.x; 
		float  tx1 = (nodeMax.x - rayOrigin.x) / rayDirection.x;  
		float  ty0 = (node.position.y - rayOrigin.y) / rayDirection.y; 
		float  ty1 = (nodeMax.y - rayOrigin.y) / rayDirection.y;  
		float  tz0 = (node.position.z - rayOrigin.z) / rayDirection.z; 
		float  tz1 = (nodeMax.z - rayOrigin.z) / rayDirection.z;

		ProcSubtree(tx0,ty0,tz0,tx1,ty1,tz1, node, intersectedNodes); 
	}

	private void ProcSubtree( float tx0, float ty0, float tz0, 
								float tx1, float ty1, float tz1, 
								Node n, List<Node> intersectedNodes ) 
	{ 
		if (n == null || !(Mathf.Max(tx0,ty0,tz0) < Mathf.Min(tx1,ty1,tz1)) ) 
			return;

		if (n.leaf) 
		{ 
			intersectedNodes.Add(n);
			return; 
		}

		float txM = 0.5f * (tx0 + tx1); 
		float tyM = 0.5f * (ty0 + ty1); 
		float tzM = 0.5f * (tz0 + tz1);

		// Note, this is based on the assumption that the children are ordered in a particular 
		// manner.  Different octree libraries will have to adjust. 
		ProcSubtree(tx0,ty0,tz0,txM,tyM,tzM, n.children[0], intersectedNodes); // x0y0z0
		ProcSubtree(tx0,ty0,tzM,txM,tyM,tz1, n.children[1], intersectedNodes); //x0y0z1
		ProcSubtree(tx0,tyM,tz0,txM,ty1,tzM, n.children[2], intersectedNodes); //x0y1z0
		ProcSubtree(tx0,tyM,tzM,txM,ty1,tz1, n.children[3], intersectedNodes); //x0y1z1
		ProcSubtree(txM,ty0,tz0,tx1,tyM,tzM, n.children[4], intersectedNodes); //x1y0z0
		ProcSubtree(txM,ty0,tzM,tx1,tyM,tz1, n.children[5], intersectedNodes); //x1y0z1
		ProcSubtree(txM,tyM,tz0,tx1,ty1,tzM, n.children[6], intersectedNodes); //x1y1z0
		ProcSubtree(txM,txM,tzM,tx1,ty1,tz1, n.children[7], intersectedNodes); //x1y1z1
	}
	/*
		Debug Methods
	 */

	public ColoredBox[] GenerateDebugBoxes(bool onlyShowLeaves) {
		List<ColoredBox> debugBoxes = new List<ColoredBox>();
		GenerateDebugBoxesAux(root, debugBoxes, onlyShowLeaves);
		return debugBoxes.ToArray();
	}

	public void GenerateDebugBoxesAux(Node node, List<ColoredBox> debugBoxes, bool onlyShowLeaves) {
		if(node == null) { return; }
		
		if(!onlyShowLeaves || node.leaf) {
			debugBoxes.Add(node.GetDebugBox());
		}

		if(node.children != null) {
			for(int i = 0; i < 8; i++) {
				GenerateDebugBoxesAux(node.children[i], debugBoxes, onlyShowLeaves);
			}
		}
	}

	public ColoredBox[] GenerateDebugBoxesAlongRay(Ray ray, bool onlyShowLeaves) {
		List<Node> nodes = TraceAux(ray);
		List<ColoredBox> debugBoxes = new List<ColoredBox>();
		foreach(Node node in nodes) {
			if(!onlyShowLeaves || node.leaf) {
				ColoredBox box = node.GetDebugBox();
				box.Color.a = 0.9f;
				debugBoxes.Add(box);
			}
		}
		return debugBoxes.ToArray();
	}
}
}
