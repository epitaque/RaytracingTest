using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using RT.CS;

namespace RT.CS {
public class NaiveTracer : CompactSVO.CompactSVOTracer {
	private Node ExpandSVO(List<int> svo) {
		Node root = new Node(new Vector3(-1, -1, -1), 2, 1, false);
		ExpandSVOAux(root, 0, 1, svo);
		return root;
	}

	/*
	    child pointer | valid mask | leaf mask
            16			   8			8
	 */
	private void ExpandSVOAux(Node node, int nodeIndex, int level, List<int> svo) { 
		ChildDescriptor descriptor = new ChildDescriptor(svo[nodeIndex]); 
 
		node.children = new Node[8]; 
		int pointer = descriptor.childPointer;
		float half = node.size/2;

		for(int childNum = 0; childNum < 8; childNum++) { 
			if(descriptor.Valid(childNum)) {
				bool leaf = descriptor.Leaf(childNum);

				Node child = new Node(node.position + Constants.vfoffsets[childNum] * half, half, level + 1, leaf);
				node.children[childNum] = child;

				if(!leaf) {
					ExpandSVOAux(node.children[childNum], pointer++, level + 1, svo);
				}
			}
		}
	}

	/*
		Ray Tracing methods
		Returns a list of nodes that intersect a ray (in sorted order)
	 */
	public List<SVONode> Trace(UnityEngine.Ray ray, List<int> svo) {
		List<Node> intersectedNodes = new List<Node>();
		RayStep(ExpandSVO(svo), ray.origin, ray.direction, intersectedNodes);
		return intersectedNodes.ConvertAll(node => (SVONode)node).ToList();
	}
	
	private int FirstNode(double tx0, double ty0, double tz0, double txm, double tym, double tzm){
		sbyte answer = 0;

		if(tx0 > ty0) {
			if(tz0 > tx0) { // tz0 max. entry xy
				if(txm < tz0) answer |= 4;
				if(tym < tz0) answer |= 2;
			}
			else { //tx0 max. entry yz
				if(tym < tx0) answer |= 2;
				if(tzm < tx0) answer |= 1;
			}
		} else {
			if(ty0 > tz0) { // ty0 max. entry xz
				if(txm < ty0) answer |= 4;
				if(tzm < ty0) answer |= 1;
			} else { // tz0 max. entry XY
				if(txm < tz0) answer |= 4;
				if(tym < tz0) answer |= 2;
			}
		}
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

		if(node == null || tx1 <= 0 || ty1 <= 0 || tz1 <= 0) { 
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

	public List<SVONode> GetAllNodes(List<int> svo) {
		List<SVONode> nodes = new List<SVONode>();
		testRoot = ExpandSVO(svo);
		GetAllNodesAux(ExpandSVO(svo), nodes);
		return nodes;
	}

	private void GetAllNodesAux(Node node, List<SVONode> nodes) {
		if(node == null) { return; }
		
		nodes.Add(node);

		if(node.children != null) {
			for(int i = 0; i < 8; i++) {
				GetAllNodesAux(node.children[i], nodes);
			}
		}
	}
	
	public void DrawGizmos(float scale) {
	}

	// Test the tracing functionality

	public static Node testRoot;
	static NaiveTracer() {
		/*Debug.Log("Attempting NaiveTracer test");
		NaiveCreator c = new NaiveCreator();
		NaiveTracer t = new NaiveTracer();
		List<int> nodes = new List<int>();
		nodes = c.Create(SampleFunctions.functions[(int)SampleFunctions.Type.Sphere], 4);

		testRoot = new Node(new Vector3(-1, -1, -1), 2, 1, false);
		t.ExpandSVOAux(testRoot, 0, 1, nodes);

		string result = "NaiveTracer Expand SVO test: \n";
		result += "Compressed:\n" + string.Join("\n", nodes.ConvertAll(code => new ChildDescriptor(code))) + "\n\n";
		result += "Uncompressed: \n" + testRoot.StringifyHierarchy();
		Debug.Log(result);*/
	}

}
}