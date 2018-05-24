using System.Collections.Generic;
using UnityEngine;

namespace OT {
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

public class OctreeTest : MonoBehaviour {
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
	Ray reflectedRay = new Ray(new Vector3(0, 0, 0), new Vector3(16, 0, 0));

    public GameObject RayPoint1;
    public GameObject RayPoint2;

	public void Start() {
		root = GenerateOctree((float x, float y, float z) => UtilFuncs.Sphere(x, y, z, 0.8f), MaxLOD);
		Debug.Log("Generated Octree. ContainsSurface: " + root.ContainsSurface);
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
		node.Color.a = 0.8f;

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
				node.Children[i].Min = node.Min + Constants.qoffsets[i] * (node.Size / 2f);
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
		Gizmos.DrawRay(currentRay.origin * OctreeScale, currentRay.direction * 100);
		Gizmos.color = Color.blue;
		Gizmos.DrawRay(reflectedRay.origin * OctreeScale, reflectedRay.direction * 100);
		

		DrawNodeRecursive(root, OctreeScale);
	}

	public void Update() {
        Ray newRay = new Ray(RayPoint1.transform.position, RayPoint2.transform.position - RayPoint1.transform.position);
        if(Vector3.Distance(newRay.origin, currentRay.origin) > 0.001 || Vector3.Distance(newRay.direction, currentRay.direction) > 0.001) {
            currentRay = newRay;
            //Debug.Log("Adjusting ray. New ray o: " + newRay.origin + ", dir: " + newRay.direction);
            CastRay(currentRay.origin, currentRay.direction);
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
			node.Color.a = 0.8f;
		}
		currentlyRayedNodes.Clear();
	}

	public void CastRay(Vector3 origin, Vector3 direction) {
		//ray_Octree_traversal(root, origin, direction);
		ClearLastRayNodeList();
		//currentRay = new Ray(origin, direction * 64); //  * OctreeScale * 1.5f
		ray_step(root, origin, direction, currentlyRayedNodes);
		for(int i = 0; i < currentlyRayedNodes.Count; i++) {
			currentlyRayedNodes[i].Color = new Color(0, 1, 0, 1f ); //- ((float)i / (float)currentlyRayedNodes.Count)
		}
	}

	int find_firstNode(double tx0, double ty0, double tz0, double txm, double tym, double tzm){
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

	int next_Node(double txm, double tym, double tzm, int x, int y, int z){
		if(txm < tym){
			if(txm < tzm){return x;}  // YZ plane
		}
		else{
			if(tym < tzm){return y;} // XZ plane
		}
		return z; // XY plane;
	}


	void ray_step(Node node, Vector3 rayOrigin, Vector3 rayDirection, List<Node> intersectedNodes) 
	{ 
		Vector3 nodeMax = node.Min + Vector3.one * node.Size;
        Vector3 octreeCenter = new Vector3(0, 0, 0);
		sbyte a = 0; 

		if (rayDirection.x < 0) 
		{ 
			rayOrigin.x = -rayOrigin.x; 
			rayDirection.x = -(rayDirection.x); 
			a |= 4; 
		} 
		if (rayDirection.y < 0) 
		{ 
			rayOrigin.y = -rayOrigin.y; 
			rayDirection.y = -(rayDirection.y); 
			a |= 2; 
		} 
		if (rayDirection.z < 0) 
		{ 
			rayOrigin.z = -rayOrigin.z; 
			rayDirection.z = -(rayDirection.z); 
			a |= 1; 
		}

		reflectedRay = new Ray(rayOrigin, rayDirection);

		float  tx0 = (node.Min.x - rayOrigin.x) / rayDirection.x; 
		float  tx1 = (nodeMax.x - rayOrigin.x) / rayDirection.x;  
		float  ty0 = (node.Min.y - rayOrigin.y) / rayDirection.y; 
		float  ty1 = (nodeMax.y - rayOrigin.y) / rayDirection.y;  
		float  tz0 = (node.Min.z - rayOrigin.z) / rayDirection.z; 
		float  tz1 = (nodeMax.z - rayOrigin.z) / rayDirection.z;  

		proc_subtree(tx0,ty0,tz0, tx1,ty1, tz1, node, intersectedNodes, a); 
	}

	void proc_subtree(float tx0, float ty0, float tz0,
					  float tx1, float ty1, float tz1,
					  Node n, List<Node> intersectedNodes , sbyte a) 
	{ 
		if (!(Mathf.Max(tx0,ty0,tz0) < Mathf.Min(tx1,ty1,tz1)) ) 
			return;

		if (n.IsLeaf) 
		{ 
			intersectedNodes.Add(n);
			return; 
		}

		float txM = 0.5f * (tx0 + tx1); 
		float tyM = 0.5f * (ty0 + ty1); 
		float tzM = 0.5f * (tz0 + tz1); 

		//int currNode = find_firstNode(tx0,ty0,tz0,txM,tyM,tzM);


		// Note, this is based on the assumption that the children are ordered in a particular 
		// manner.  Different Octree libraries will have to adjust. 

		proc_subtree(tx0,ty0,tz0,txM,tyM,tzM,n.Children[a], intersectedNodes, a); 
		proc_subtree(tx0,ty0,tzM,txM,tyM,tz1,n.Children[1^a], intersectedNodes, a); 
		proc_subtree(tx0,tyM,tz0,txM,ty1,tzM,n.Children[2^a], intersectedNodes, a); 
		proc_subtree(tx0,tyM,tzM,txM,ty1,tz1,n.Children[3^a], intersectedNodes, a); 
		proc_subtree(txM,ty0,tz0,tx1,tyM,tzM,n.Children[4^a], intersectedNodes, a); 
		proc_subtree(txM,ty0,tzM,tx1,tyM,tz1,n.Children[5^a], intersectedNodes, a); 
		proc_subtree(txM,tyM,tz0,tx1,ty1,tzM,n.Children[6^a], intersectedNodes, a); 
		proc_subtree(txM,txM,tzM,tx1,ty1,tz1,n.Children[7], intersectedNodes, a); 
		/*do {
			switch(currNode) { 
			case 0 : proc_subtree(tx0,ty0,tz0,txM,tyM,tzM,n.Children[a], intersectedNodes, a); 
				currNode = next_Node(txM,tyM,tzM,4,2,1); 
				break; 
			case 1 : proc_subtree(tx0,ty0,tzM,txM,tyM,tz1,n.Children[1^a], intersectedNodes, a); 
				currNode = next_Node(txM,tyM,tz1,5,3,8); 
				break; 
			case 2 : proc_subtree(tx0,tyM,tz0,txM,ty1,tzM,n.Children[2^a], intersectedNodes, a); 
				currNode = next_Node(txM,ty1,tzM,6,8,3); 
				break; 
			case 3 : proc_subtree(tx0,tyM,tzM,txM,ty1,tz1,n.Children[3^a], intersectedNodes, a); 
				currNode = next_Node(txM,ty1,tz1,7,8,8); 
				break; 
			case 4 : proc_subtree(txM,ty0,tz0,tx1,tyM,tzM,n.Children[4^a], intersectedNodes, a); 
				currNode = next_Node(tx1,tyM,tzM,8,6,5); 
				break; 
			case 5 : proc_subtree(txM,ty0,tzM,tx1,tyM,tz1,n.Children[5^a], intersectedNodes, a); 
				currNode = next_Node(tx1,tyM,tz1,8,7,8); 
				break; 
			case 6 : proc_subtree(txM,tyM,tz0,tx1,ty1,tzM,n.Children[6^a], intersectedNodes, a); 
				currNode = next_Node(tx1,ty1,tzM,8,8,7); 
				break; 
			case 7 : proc_subtree(txM,txM,tzM,tx1,ty1,tz1,n.Children[7], intersectedNodes, a); 
				currNode = 8; 
				break; 
			} 
		} while(currNode < 8);*/
	}


}
}


