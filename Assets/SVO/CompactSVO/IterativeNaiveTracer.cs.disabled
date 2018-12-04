using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using RT.CS;

namespace RT.CS {
public class IterativeNaiveTracer : CompactSVO.CompactSVOTracer {
	private Node ExpandSVO(List<uint> svo) {
		Node root = new Node(new Vector3(-1, -1, -1), 2, 1, false);
		ExpandSVOAux(root, 0, 1, svo);
		return root;
	}

	/*
	    child pointer | valid mask | leaf mask
            16			   8			8
	 */
	private void ExpandSVOAux(Node node, int nodeIndex, int level, List<uint> svo) { 
		ChildDescriptor descriptor = new ChildDescriptor(svo[nodeIndex]); 
 
		node.Children = new Node[8]; 
		int pointer = descriptor.childPointer;
		double half = node.Size/2d;

		for(int childNum = 0; childNum < 8; childNum++) { 
			if(descriptor.Valid(childNum)) {
				bool leaf = descriptor.Leaf(childNum);

				Node child = new Node(node.Position + Constants.vfoffsets[childNum] * (float)(half), half, level + 1, leaf);
				node.Children[childNum] = child;

				if(!leaf) {
					ExpandSVOAux(node.Children[childNum], pointer++, level + 1, svo);
				}
			}
		}
	}

	/*
		Ray Tracing methods
		Returns a list of nodes that intersect a ray (in sorted order)
	 */
	public List<SVONode> Trace(UnityEngine.Ray ray, List<uint> svo) {
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

 	private static int NewNode(double txm, int x, double tym, int y, double tzm, int z){
		if(txm < tym){
			if(txm < tzm){return x;}  // YZ plane
		}
		else{
			if(tym < tzm){return y;} // XZ plane
		}
		return z; // XY plane;
	}

	private class ParameterData {
		public Vector3 t0;
		public Vector3 t1;
		public Node node;
		public int currNode;

		public ParameterData(Vector3 t0, Vector3 t1, Node node, int currNode) {
			this.t0 = t0;
			this.t1 = t1;
			this.node = node;
			this.currNode = currNode;
		}
	}

 	private void RayStep(Node root, Vector3 rayOrigin, Vector3 rayDirection, List<Node> intersectedNodes)  {

		// 'a' is used to flip the bits that correspond with a negative ray direction
		// when picking a child node
		sbyte a = 0;
 		if(rayDirection.x < 0) {
			rayOrigin.x = -rayOrigin.x;
			rayDirection.x = -rayDirection.x;
			a |= 4;
		}
		if(rayDirection.y < 0) { 		
			rayOrigin.y = -rayOrigin.y;
			rayDirection.y = -rayDirection.y;
			a |= 2;
		}
		if(rayDirection.z < 0) { 		
			rayOrigin.z =  -rayOrigin.z;
			rayDirection.z = -rayDirection.z;
			a |= 1;
		}

		if(rayDirection.x == 0) rayDirection.x += float.Epsilon;
		if(rayDirection.y == 0) rayDirection.y += float.Epsilon;
		if(rayDirection.z == 0) rayDirection.z += float.Epsilon;

		Vector3 nodeMax = root.Position + Vector3.one * (float)root.Size;
 		double divx = 1 / rayDirection.x;
		double divy = 1 / rayDirection.y;
		double divz = 1 / rayDirection.z;

		double tx_bias = rayOrigin.x * divx;
		double ty_bias = rayOrigin.y * divy;
		double tz_bias = rayOrigin.z * divz;

 		double tx0 = root.Position.x * divx - tx_bias;
		double ty0 = root.Position.y * divy - ty_bias;
		double tz0 = root.Position.z * divz - tz_bias;
		double tx1 = nodeMax.x * divx - tx_bias;
		double ty1 = nodeMax.y * divy - ty_bias;
		double tz1 = nodeMax.z * divz - tz_bias;

		Vector3 t0 = new Vector3((float)tx0, (float)ty0, (float)tz0);
		Vector3 t1 = new Vector3((float)tx1, (float)ty1, (float)tz1);

		ParameterData[] stack = new ParameterData[30];
		int sf = 0;
		stack[sf] = new ParameterData(t0, t1, root, -1);

 		if(Mathd.Max(tx0,ty0,tz0) < Mathd.Min(tx1,ty1,tz1)){ 	
			while(sf >= 0) {
				ParameterData data = stack[sf];
				Node node = data.node;

				t1 = data.t1;

				if(node == null || data.currNode > 7 || t1.x <= 0 || t1.y <= 0 || t1.z <= 0) { 
					sf--;
					continue;
				}

				tx0 = Mathf.Pow(-1, ((a & 4) >> 2)) * node.Position.x * divx - tx_bias;
				ty0 = Mathf.Pow(-1, ((a & 2) >> 1)) * node.Position.y * divy - ty_bias;
				tz0 = Mathf.Pow(-1, ((a & 1))) * node.Position.z * divz - tz_bias;


				t0 = new Vector3((float)tx0, (float)ty0, (float)tz0);
				//t0 = data.t0;

				if(node.Leaf){
					intersectedNodes.Add(node);
					sf--;
					continue;
				}

				Vector3 tm = 0.5f*(t0 + t1);
				data.currNode = data.currNode == -1 ? FirstNode(t0.x,t0.y,t0.z,tm.x,tm.y,tm.z) : data.currNode;

				Vector3 childT0 = getT0(t0, tm, data.currNode);
				Vector3 childT1 = getT1(tm, t1, data.currNode);
				ParameterData nextFrame = new ParameterData(childT0, childT1, data.node.Children[data.currNode^a], -1);
				data.currNode = getNewNode(tm, t1, data.currNode);				
				stack[++sf] = nextFrame;
			}
		}
	}

	private static Vector3 getT0(Vector3 t0, Vector3 tm, int currNode) {
		float[] arr = new float[] {t0.x, t0.y, t0.z, tm.x, tm.y, tm.z};
		return new Vector3(arr[((currNode & 4) >> 2) * 3],  arr[1 + ((currNode & 2) >> 1) * 3], arr[2 + (currNode & 1) * 3]);
	}
	private static Vector3 getT1(Vector3 tm, Vector3 t1, int currNode) {
		float[] arr = new float[] {tm.x, tm.y, tm.z, t1.x, t1.y, t1.z};
		return new Vector3(arr[((currNode & 4) >> 2) * 3],  arr[1 + ((currNode & 2) >> 1) * 3], arr[2 + (currNode & 1) * 3]);
	}
	private static int getNewNode(Vector3 tm, Vector3 t1, int currNode) {
		int[] arr = new int[] {4,2,1, 5,3,8, 6,8,3, 7,8,8, 8,6,5, 8,7,8, 8,8,7, 8,8,8};
		Vector3 t = getT1(tm, t1, currNode);
		return NewNode(t.x, arr[3 * currNode], t.y, arr[1 + 3*currNode], t.z, arr[2 + 3*currNode]);
	}

	/*
		Debug Methods
	 */

	public List<SVONode> GetAllNodes(List<uint> svo) {
		List<SVONode> nodes = new List<SVONode>();
		testRoot = ExpandSVO(svo);
		GetAllNodesAux(ExpandSVO(svo), nodes);
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

	// Test the tracing functionality

	public static Node testRoot;
	static IterativeNaiveTracer() {
		Debug.Log("Attempting testGetTTest");

		Vector3 t0 = new Vector3(0, 0, 0);
		Vector3 tm = new Vector3(0.5f, 0.5f, 0.5f);
		Vector3 t1 = new Vector3(1, 1, 1);

		string result = "GetTTest Results\n\n";
		for(int currNode = 0; currNode < 8; currNode++) {
			Vector3 ct0 = getT0(t0, tm, currNode);
			Vector3 ct1 = getT1(tm, t1, currNode); 


			int[] arr = new int[] {4,2,1, 5,3,8, 6,8,3, 7,8,8, 8,6,5, 8,7,8, 8,8,7, 8,8,8};
			Vector3 t = getT1(tm, t1, currNode);

			result += "currNode " + currNode + ": ct0 " + ct0 + ", ct1 " + ct1 + "\n";
			result += "newNode params: (" + t.x + ", " + arr[3 * currNode] + ", " + t.y + ", " + arr[1 + 3*currNode] + ", " + t.z + ", " + arr[2 + 3*currNode] + ")\n";
		}

		Debug.Log(result); 

	}

}
}