using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RT {
public class NaiveSVO : SVO {
	private int maxLevel;
	private UtilFuncs.Sampler sample;
	private Node root;

	/*
		Debug Fields
	 */
	private Ray reflectedRay;

	private class Node : SVONode {
		public Node(Vector3 position, float size, int level, bool leaf) {
			base.position = position;
			base.size = size;
			base.level = level;
			base.leaf = leaf;
		}

		public Node[] Children;
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
	private Node BuildTreeAux(Node node, int level) {
		// Node is leaf. Determine if within surface. If so, return node.
		if(node.leaf) {
			Vector3 p = node.position + Vector3.one * (float)node.size / 2;
			if(sample(p.x, p.y, p.z) <= 0 && IsEdge(node)) {
				return node;
			}
		}

		// Node is not leaf. Construct 8 children. If any of them intersect surface, return node.
		else {
			bool childExists = false;
			int numLeaves = 0;
			float half = node.size/2;
			node.Children = new Node[8];

			for(int i = 0; i < 8; i++) {
				Node child = new Node(node.position + Constants.vfoffsets[i] * half, half, level + 1, level + 1 == maxLevel);
				node.Children[i] = BuildTreeAux(child, level + 1);
				if(node.Children[i] != null) {
					childExists = true;
					if(node.Children[i].leaf) {
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
	public List<SVONode> Trace(UnityEngine.Ray ray) {
		List<Node> intersectedNodes = new List<Node>();
		RayStep(root, ray.origin, ray.direction, intersectedNodes);
		return intersectedNodes.ConvertAll(node => (SVONode)node).ToList();
	}

	private int FirstNode(double tx0, double ty0, double tz0, double txm, double tym, double tzm){
		sbyte answer = 0;

		if(tx0 > ty0){
			if(tx0 > tz0){
				if(tym < tx0) answer|=2;
				if(tzm < tx0) answer|=1;
				if(txm < ty0) answer|=4;
				if(tzm < ty0) answer|=1;
				return (int) answer;
			}
		}

		if(txm < tz0) answer|=4;
		if(tym < tz0) answer|=2;
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

		if(node == null || !(Mathd.Max(tx0,ty0,tz0) < Mathd.Min(tx1,ty1,tz1)) || Mathd.Min(tx1, ty1, tz1) < 0) { 
			return;
		}
		if(node.leaf){
			intersectedNodes.Add(node);
			return;
		}

 		txm = (float)(0.5*(tx0 + tx1)); 	
		tym = (float)(0.5*(ty0 + ty1)); 	
		tzm = (float)(0.5*(tz0 + tz1)); 	
		currNode = FirstNode(tx0,ty0,tz0,txm,tym,tzm); 	
		do{ 		
			switch (currNode) { 		
			case 0: {  			
				ProcSubtree(rayOrigin, rayDirection, tx0,ty0,tz0,txm,tym,tzm,node.Children[a], intersectedNodes, a);
				currNode = NewNode(txm,4,tym,2,tzm,1);
				break;}
			case 1: {
				ProcSubtree(rayOrigin, rayDirection, tx0,ty0,tzm,txm,tym,tz1,node.Children[1^a], intersectedNodes, a);
				currNode = NewNode(txm,5,tym,3,tz1,8);
				break;}
			case 2: {
				ProcSubtree(rayOrigin, rayDirection, tx0,tym,tz0,txm,ty1,tzm,node.Children[2^a], intersectedNodes, a);
				currNode = NewNode(txm,6,ty1,8,tzm,3);
				break;}
			case 3: {
				ProcSubtree(rayOrigin, rayDirection, tx0,tym,tzm,txm,ty1,tz1,node.Children[3^a], intersectedNodes, a);
				currNode = NewNode(txm,7,ty1,8,tz1,8);
				break;}
			case 4: {
				ProcSubtree(rayOrigin, rayDirection, txm,ty0,tz0,tx1,tym,tzm,node.Children[4^a], intersectedNodes, a);
				currNode = NewNode(tx1,8,tym,6,tzm,5);
				break;}
			case 5: {
				ProcSubtree(rayOrigin, rayDirection, txm,ty0,tzm,tx1,tym,tz1,node.Children[5^a], intersectedNodes, a);
				currNode = NewNode(tx1,8,tym,7,tz1,8);
				break;}
			case 6: {
				ProcSubtree(rayOrigin, rayDirection, txm,tym,tz0,tx1,ty1,tzm,node.Children[6^a], intersectedNodes, a);
				currNode = NewNode(tx1,8,ty1,8,tzm,7);
				break;}
			case 7: {
				ProcSubtree(rayOrigin, rayDirection, txm,tym,tzm,tx1,ty1,tz1,node.Children[7^a], intersectedNodes, a);
				currNode = 8;
				break;}
			}
		} while (currNode < 8);
	}
 	private void RayStep(Node node, Vector3 rayOrigin, Vector3 rayDirection, List<Node> intersectedNodes)  {
		Vector3 nodeMax = node.position + Vector3.one * (float)node.size;
		sbyte a = 0;
 		if(rayDirection.x < 0) {
			rayOrigin.x = -rayOrigin.x;
			rayDirection.x = -rayDirection.x;
			a |= 4;
		}
		if(rayDirection.y < 0){ 		
			rayOrigin.y = -rayOrigin.y;
			rayDirection.y = -rayDirection.y;
			a |= 2;
		}
		if(rayDirection.z < 0){ 		
			rayOrigin.z =  -rayOrigin.z;
			rayDirection.z = -rayDirection.z;
			a |= 1;
		}

 		double divx = 1 / rayDirection.x; // IEEE stability fix
		double divy = 1 / rayDirection.y;
		double divz = 1 / rayDirection.z;
 		double tx0 = (node.position.x - rayOrigin.x) * divx;
		double tx1 = (nodeMax.x - rayOrigin.x) * divx;
		double ty0 = (node.position.y - rayOrigin.y) * divy;
		double ty1 = (nodeMax.y - rayOrigin.y) * divy;
		double tz0 = (node.position.z - rayOrigin.z) * divz;
		double tz1 = (nodeMax.z - rayOrigin.z) * divz;

 		if(Mathd.Max(tx0,ty0,tz0) < Mathd.Min(tx1,ty1,tz1)){ 		
			ProcSubtree(rayOrigin, rayDirection, tx0,ty0,tz0,tx1,ty1,tz1,node,intersectedNodes, a);
		}
	}

	/*
		Debug Methods
	 */

	public List<SVONode> GetAllNodes() {
		List<SVONode> nodes = new List<SVONode>();
		GetAllNodesAux(root, nodes);
		return nodes;
	}

	private void GetAllNodesAux(Node node, List<SVONode> nodes) {
		if(node == null) { return; }
		
		nodes.Add(node);

		if(node.Children != null) {
			for(int i = 0; i < 8; i++) {
				GetAllNodesAux(node.Children[i], nodes);
			}
		}
	}
	
	public void DrawGizmos(float scale) {
	}
}
}
