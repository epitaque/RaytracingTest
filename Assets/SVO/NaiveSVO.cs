using UnityEngine;
using System.Collections.Generic;

namespace RT {
public class NaiveSVO : SVO {
	private int maxLevel;
	private UtilFuncs.Sampler sample;
	private Node root;

	/*
		Debug Fields
	 */
	private Ray reflectedRay;

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
		Ray Tracing methods
		Returns a list of nodes that intersect a ray (in sorted order)
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

	private int FirstNode(double tx0, double ty0, double tz0, double txm, double tym, double tzm){
		sbyte answer = 0;	// initialize to 00000000
		// select the entry plane and set bits
		if(tx0 > ty0){
			if(tx0 > tz0){ // PLANE YZ
				if(tym < tx0) answer|=2;	// set bit at position 1
				if(tzm < tx0) answer|=1;	// set bit at position 0
				if(txm < ty0) answer|=4;	// set bit at position 2
				if(tzm < ty0) answer|=1;	// set bit at position 0
				return (int) answer;
			}
		}
		// PLANE XY
		if(txm < tz0) answer|=4;	// set bit at position 2
		if(tym < tz0) answer|=2;	// set bit at position 1
		return (int) answer;
	}
 	private int NewNode(double txm, int x, double tym, int y, double tzm, int z){
		if(txm < tym){
			if(txm < tzm){return x;}  // YZ plane
		}
		else{
			if(tym < tzm){return y;} // XZ plane
		}
		return z; // XY plane;
	}
 	private void ProcSubtree (Vector3 rayOrigin, Vector3 rayDirection, double tx0, double ty0, double tz0, double tx1, double ty1, double tz1, Node node, List<Node> intersectedNodes, sbyte a){
		float txm, tym, tzm;
		int currNode;
 		if(tx1 < 0 || ty1 < 0 || tz1 < 0 || node == null) return; 	
		if(node.leaf){
			intersectedNodes.Add(node);
			Debug.Log("Reached leaf node " + node);
			if(node.ToString().Equals("[Node, Position (0.0, -0.3, -1.0), Size: 0.25, Leaf: True, Level: 3]")) {
				Debug.Log("Reached error node.");
			}
			return;
		}
		else{ 
			//cout << "Reached node " << node->debug_ID << endl;
		} 	
		txm = (float)(0.5*(tx0 + tx1)); 	
		tym = (float)(0.5*(ty0 + ty1)); 	
		tzm = (float)(0.5*(tz0 + tz1)); 	
		currNode = FirstNode(tx0,ty0,tz0,txm,tym,tzm); 	
		do{ 		
			switch (currNode) { 		
			case 0: {  			
				ProcSubtree(rayOrigin, rayDirection, tx0,ty0,tz0,txm,tym,tzm,node.children[a], intersectedNodes, a);
				currNode = NewNode(txm,4,tym,2,tzm,1);
				break;}
			case 1: {
				ProcSubtree(rayOrigin, rayDirection, tx0,ty0,tzm,txm,tym,tz1,node.children[1^a], intersectedNodes, a);
				currNode = NewNode(txm,5,tym,3,tz1,8);
				break;}
			case 2: {
				ProcSubtree(rayOrigin, rayDirection, tx0,tym,tz0,txm,ty1,tzm,node.children[2^a], intersectedNodes, a);
				currNode = NewNode(txm,6,ty1,8,tzm,3);
				break;}
			case 3: {
				ProcSubtree(rayOrigin, rayDirection, tx0,tym,tzm,txm,ty1,tz1,node.children[3^a], intersectedNodes, a);
				currNode = NewNode(txm,7,ty1,8,tz1,8);
				break;}
			case 4: {
				ProcSubtree(rayOrigin, rayDirection, txm,ty0,tz0,tx1,tym,tzm,node.children[4^a], intersectedNodes, a);
				currNode = NewNode(tx1,8,tym,6,tzm,5);
				break;}
			case 5: {
				ProcSubtree(rayOrigin, rayDirection, txm,ty0,tzm,tx1,tym,tz1,node.children[5^a], intersectedNodes, a);
				currNode = NewNode(tx1,8,tym,7,tz1,8);
				break;}
			case 6: {
				ProcSubtree(rayOrigin, rayDirection, txm,tym,tz0,tx1,ty1,tzm,node.children[6^a], intersectedNodes, a);
				currNode = NewNode(tx1,8,ty1,8,tzm,7);
				break;}
			case 7: {
				ProcSubtree(rayOrigin, rayDirection, txm,tym,tzm,tx1,ty1,tz1,node.children[7^a], intersectedNodes, a);
				currNode = 8;
				break;}
			}
		} while (currNode < 8);
	}
 	public void RayStep(Node node, Vector3 rayOrigin, Vector3 rayDirection, List<Node> intersectedNodes)  {
		Vector3 nodeMax = node.position + Vector3.one * (float)node.size;
		sbyte a = 0;
 		if(rayDirection[0] < 0) {
			rayOrigin[0] = - rayOrigin[0];
			rayDirection[0] = - rayDirection[0];
			a |= 4 ; //bitwise OR (latest bits are XYZ)
		}
		if(rayDirection[1] < 0){ 		
			rayOrigin[1] = - rayOrigin[1];
			rayDirection[1] = - rayDirection[1];
			a |= 2 ;
		}
		if(rayDirection[2] < 0){ 		
			rayOrigin[2] =  - rayOrigin[2];
			rayDirection[2] = - rayDirection[2];
			a |= 1 ;
		}

		reflectedRay = new Ray(rayOrigin, rayDirection);

 		double divx = 1 / rayDirection[0]; // IEEE stability fix
		double divy = 1 / rayDirection[1];
		double divz = 1 / rayDirection[2];
 		double tx0 = (node.position[0] - rayOrigin[0]) * divx;
		double tx1 = (nodeMax[0] - rayOrigin[0]) * divx;
		double ty0 = (node.position[1] - rayOrigin[1]) * divy;
		double ty1 = (nodeMax[1] - rayOrigin[1]) * divy;
		double tz0 = (node.position[2] - rayOrigin[2]) * divz;
		double tz1 = (nodeMax[2] - rayOrigin[2]) * divz;

 		if(Mathd.Max(tx0,ty0,tz0) < Mathd.Min(tx1,ty1,tz1)){ 		
			ProcSubtree(rayOrigin, rayDirection, tx0,ty0,tz0,tx1,ty1,tz1,node,intersectedNodes, a);
		}
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

	public void DrawGizmos(float scale) {
	}

	private void DrawReflectedRay(float scale) {
		Gizmos.color = Color.green;
		Gizmos.DrawLine(reflectedRay.origin * scale, reflectedRay.direction * 10000);
	}
}
}
