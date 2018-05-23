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

	[Range (0, 64f)]
	[SerializeField]
	public float OctreeScale = 16;

	Node root;
	List<Node> currentlyRayedNodes = new List<Node>();
	Ray currentRay = new Ray(new Vector3(0, 0, 0), new Vector3(16, 0, 0));
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
		Gizmos.color = Color.green;
		//Debug.Log("Drawing ray at " + currentRay.origin + ", with direction " + currentRay.direction);
		Gizmos.DrawRay(currentRay.origin * OctreeScale, currentRay.direction * 64);
		DrawNodeRecursive(root, OctreeScale);
	}

	public void Update() {
		if(UnityEngine.Input.GetKeyDown(KeyCode.R)) {
			CastRay(Camera.main.transform.position / OctreeScale, Camera.main.transform.forward);
		}
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

	public void ClearLastRayNodeList() {
		foreach(Node node in currentlyRayedNodes) {
			//Debug.Log("Clearing node...");
			node.Color = UtilFuncs.SinColor(node.Depth);
			node.Color.a = 0.2f;
		}
		currentlyRayedNodes.Clear();
	}

	public void CastRay(Vector3 origin, Vector3 direction) {
		//ray_octree_traversal(root, origin, direction);
		ClearLastRayNodeList();
		currentRay = new Ray(origin, direction * 64); //  * OctreeScale * 1.5f
		ray_step(root, origin, direction, currentlyRayedNodes);
		for(int i = 0; i < currentlyRayedNodes.Count; i++) {
			currentlyRayedNodes[i].Color = new Color(0, 1, 0, 1);
		}
	}
	/*sbyte a; // because an unsigned char is 8 bits
	int first_node(double tx0, double ty0, double tz0, double txm, double tym, double tzm){
		sbyte answer = 0;   // initialize to 00000000
		// select the entry plane and set bits
		if(tx0 > ty0){
			if(tx0 > tz0){ // PLANE YZ
				if(tym < tx0) answer|=2;    // set bit at position 1
				if(tzm < tx0) answer|=1;    // set bit at position 0
				return (int) answer;
			}
		}
		else {
			if(ty0 > tz0){ // PLANE XZ
				if(txm < ty0) answer|=4;    // set bit at position 2
				if(tzm < ty0) answer|=1;    // set bit at position 0
				return (int) answer;
			}
		}
		// PLANE XY
		if(txm < tz0) answer|=4;    // set bit at position 2
		if(tym < tz0) answer|=2;    // set bit at position 1
		return (int) answer;
	}
	int new_node(double txm, int x, double tym, int y, double tzm, int z){
		if(txm < tym){
			if(txm < tzm){return x;}  // YZ plane
		}
		else{
			if(tym < tzm){return y;} // XZ plane
		}
		return z; // XY plane;
	}
	void proc_subtree (double tx0, double ty0, double tz0, double tx1, double ty1, double tz1, Node node){
		float txm, tym, tzm;
		int currNode;

		if(tx1 < 0 || ty1 < 0 || tz1 < 0) return;
		if(node.IsLeaf || node.Children == null){
			//cout << "Reached leaf node " << node->debug_ID << endl;
			node.Color = Color.black;
		}
		else{ 
			//cout << "Reached node " << node->debug_ID << endl;
		}

		txm = (float)(0.5*(tx0 + tx1));
		tym = (float)(0.5*(ty0 + ty1));
		tzm = (float)(0.5*(tz0 + tz1));

		currNode = first_node(tx0,ty0,tz0,txm,tym,tzm);
		do{
			switch (currNode)
			{
			case 0: { 
				proc_subtree(tx0,ty0,tz0,txm,tym,tzm,node.Children[a]);
				currNode = new_node(txm,4,tym,2,tzm,1);
				break;}
			case 1: { 
				proc_subtree(tx0,ty0,tzm,txm,tym,tz1,node.Children[1^a]);
				currNode = new_node(txm,5,tym,3,tz1,8);
				break;}
			case 2: { 
				proc_subtree(tx0,tym,tz0,txm,ty1,tzm,node.Children[2^a]);
				currNode = new_node(txm,6,ty1,8,tzm,3);
				break;}
			case 3: { 
				proc_subtree(tx0,tym,tzm,txm,ty1,tz1,node.Children[3^a]);
				currNode = new_node(txm,7,ty1,8,tz1,8);
				break;}
			case 4: { 
				proc_subtree(txm,ty0,tz0,tx1,tym,tzm,node.Children[4^a]);
				currNode = new_node(tx1,8,tym,6,tzm,5);
				break;}
			case 5: { 
				proc_subtree(txm,ty0,tzm,tx1,tym,tz1,node.Children[5^a]);
				currNode = new_node(tx1,8,tym,7,tz1,8);
				break;}
			case 6: { 
				proc_subtree(txm,tym,tz0,tx1,ty1,tzm,node.Children[6^a]);
				currNode = new_node(tx1,8,ty1,8,tzm,7);
				break;}
			case 7: { 
				proc_subtree(txm,tym,tzm,tx1,ty1,tz1,node.Children[7^a]);
				currNode = 8;
				break;}
			}
		} while (currNode<8);
	}
	void ray_octree_traversal(Node octree, Vector3 rayOrigin, Vector3 rayDirection){
		a = 0;
		Vector3 octreeCenter = octree.Min + Vector3.one * octree.Size * 0.5f;

		// fixes for rays with negative direction
		if(rayDirection[0] < 0){
			rayOrigin[0] = octreeCenter[0] * 2 - rayOrigin[0];
			rayDirection[0] = - rayDirection[0];
			a |= 4 ; //bitwise OR (latest bits are XYZ)
		}
		if(rayDirection[1] < 0){
			rayOrigin[1] = octreeCenter[1] * 2 - rayOrigin[1];
			rayDirection[1] = - rayDirection[1];
			a |= 2 ; 
		}
		if(rayDirection[2] < 0){
			rayOrigin[2] = octreeCenter[2] * 2 - rayOrigin[2];
			rayDirection[2] = - rayDirection[2];
			a |= 1 ; 
		}

		double divx = 1 / rayDirection[0]; // IEEE stability fix
		double divy = 1 / rayDirection[1];
		double divz = 1 / rayDirection[2];

		Vector3 omax = octree.Min + Vector3.one * octree.Size;

		double tx0 = (octree.Min[0] - rayOrigin[0]) * divx;
		double tx1 = (omax[0] - rayOrigin[0]) * divx;
		double ty0 = (octree.Min[1] - rayOrigin[1]) * divy;
		double ty1 = (omax[1] - rayOrigin[1]) * divy;
		double tz0 = (octree.Min[2] - rayOrigin[2]) * divz;
		double tz1 = (omax[2] - rayOrigin[2]) * divz;

		if(Mathd.Max(Mathd.Max(tx0,ty0),tz0) < Mathd.Min(Mathd.Min(tx1,ty1),tz1) ){
			proc_subtree(tx0,ty0,tz0,tx1,ty1,tz1, octree);
		}
	}*/

	void ray_step(Node node, Vector3 rayOrigin, Vector3 rayDirection, List<Node> intersectedNodes) 
	{ 
		Vector3 nodeMax = node.Min + Vector3.one * node.Size;
		float  tx0 = (node.Min.x - rayOrigin.x) / rayDirection.x; 
		float  tx1 = (nodeMax.x - rayOrigin.x) / rayDirection.x;  
		float  ty0 = (node.Min.y - rayOrigin.y) / rayDirection.y; 
		float  ty1 = (nodeMax.y - rayOrigin.y) / rayDirection.y;  
		float  tz0 = (node.Min.z - rayOrigin.z) / rayDirection.z; 
		float  tz1 = (nodeMax.z - rayOrigin.z) / rayDirection.z;

		proc_subtree(tx0,ty0,tz0,tx1,ty1,tz1, node, intersectedNodes); 
	}

	void proc_subtree( float tx0, float ty0, float tz0, 
								float tx1, float ty1, float tz1, 
								Node n, List<Node> intersectedNodes ) 
	{ 
		if ( !(Mathf.Max(tx0,ty0,tz0) < Mathf.Min(tx1,ty1,tz1)) ) 
			return;

		if (n.IsLeaf) 
		{ 
			intersectedNodes.Add(n);
			return; 
		}

		float txM = 0.5f * (tx0 + tx1); 
		float tyM = 0.5f * (ty0 + ty1); 
		float tzM = 0.5f * (tz0 + tz1);

		// Note, this is based on the assumption that the children are ordered in a particular 
		// manner.  Different octree libraries will have to adjust. 
		proc_subtree(tx0,ty0,tz0,txM,tyM,tzM, n.Children[0], intersectedNodes); // x0y0z0
		proc_subtree(tx0,ty0,tzM,txM,tyM,tz1, n.Children[1], intersectedNodes); //x0y0z1
		proc_subtree(tx0,tyM,tz0,txM,ty1,tzM, n.Children[2], intersectedNodes); //x0y1z0
		proc_subtree(tx0,tyM,tzM,txM,ty1,tz1, n.Children[3], intersectedNodes); //x0y1z1
		proc_subtree(txM,ty0,tz0,tx1,tyM,tzM, n.Children[4], intersectedNodes); //x1y0z0
		proc_subtree(txM,ty0,tzM,tx1,tyM,tz1, n.Children[5], intersectedNodes); //x1y0z1
		proc_subtree(txM,tyM,tz0,tx1,ty1,tzM, n.Children[6], intersectedNodes); //x1y1z0
		proc_subtree(txM,txM,tzM,tx1,ty1,tz1, n.Children[7], intersectedNodes); //x1y1z1
	}


}
}


