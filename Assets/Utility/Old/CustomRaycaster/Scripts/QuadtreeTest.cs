using System.Collections.Generic;
using UnityEngine;

namespace QT {
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

public class QuadtreeTest : MonoBehaviour {
	[Range (0, 8)]
	[SerializeField]
	public int DrawDepth;
	
	[Range (0, 8)]
	[SerializeField]
	public int MaxLOD = 4;

	[Range (0, 64f)]
	[SerializeField]
	public float QuadtreeScale = 16;

	Node root;
	List<Node> currentlyRayedNodes = new List<Node>();
	Ray currentRay = new Ray(new Vector3(0, 0, 0), new Vector3(16, 0, 0));
	Ray reflectedRay = new Ray(new Vector3(0, 0, 0), new Vector3(16, 0, 0));

    public GameObject RayPoint1;
    public GameObject RayPoint2;

	public void Start() {
		root = GenerateQuadtree((float x, float y, float z) => UtilFuncs.Sphere(x, y, z, 0.8f), MaxLOD);
		Debug.Log("Generated Quadtree. ContainsSurface: " + root.ContainsSurface);
	}

	public Node GenerateQuadtree(UtilFuncs.Sampler sample, int maxDepth) {
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
			node.ContainsSurface = sample(center.x, center.y, 0) < 0 ? true : false;
			node.CompletelyFilled = node.ContainsSurface;
		}
		else {
			node.IsLeaf = false;
			node.Children = new Node[4];

			node.CompletelyFilled = true;

			for(int i = 0; i < 4; i++) {
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
		Gizmos.DrawRay(currentRay.origin * QuadtreeScale, currentRay.direction * 100);
		Gizmos.color = Color.blue;
		Gizmos.DrawRay(reflectedRay.origin * QuadtreeScale, reflectedRay.direction * 100);
		

		DrawNodeRecursive(root, QuadtreeScale);
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
			for(int i = 0; i < 4; i++) {
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
		//ray_Quadtree_traversal(root, origin, direction);
		ClearLastRayNodeList();
		//currentRay = new Ray(origin, direction * 64); //  * QuadtreeScale * 1.5f
		ray_step(root, origin, direction, currentlyRayedNodes);
		for(int i = 0; i < currentlyRayedNodes.Count; i++) {
			currentlyRayedNodes[i].Color = new Color(0, 1, 0, 1f - ((float)i / (float)currentlyRayedNodes.Count));
		}
	}

	int find_firstNode(double tx0, double ty0, double txm, double tym){
		sbyte answer = 0;   // initialize to 00000000
		// select the entry plane and set bits
		if(tym < tx0) answer|=1;    // set bit at position 1
		if(txm < ty0) answer|=2;    // set bit at position 2
		return (int) answer;
	}
	int next_node(double txm, double tym, int x, int y){
		if(txm > tym){
			return x;
		}
		else {
			return y;
		}
	}


	void ray_step(Node node, Vector3 rayOrigin, Vector3 rayDirection, List<Node> intersectedNodes) 
	{ 
		Vector3 nodeMax = node.Min + Vector3.one * node.Size;
        Vector3 octreeCenter = new Vector3(0, 0, 0);
		sbyte a = 0;

        if(rayDirection.x < 0){
            rayOrigin.x = octreeCenter.x * 2 - rayOrigin.x;
            rayDirection.x = - rayDirection.x;
            a |= 2; //bitwise OR (latest bits are XYZ)
        }
        if(rayDirection.y < 0){
            rayOrigin.y = octreeCenter.x * 2 - rayOrigin.y;
            rayDirection.y = - rayDirection.y;
        	a |= 1; 
        }

		reflectedRay = new Ray(rayOrigin, rayDirection);

		float  tx0 = (node.Min.x - rayOrigin.x) / rayDirection.x; 
		float  tx1 = (nodeMax.x - rayOrigin.x) / rayDirection.x;  
		float  ty0 = (node.Min.y - rayOrigin.y) / rayDirection.y; 
		float  ty1 = (nodeMax.y - rayOrigin.y) / rayDirection.y;  

		proc_subtree(tx0,ty0,tx1,ty1, node, intersectedNodes, a); 
	}

	void proc_subtree( float tx0, float ty0, 
								float tx1, float ty1, 
								Node n, List<Node> intersectedNodes , sbyte a ) 
	{ 
		if (!(Mathf.Max(tx0,ty0) < Mathf.Min(tx1,ty1)) ) 
			return;

		if (n.IsLeaf) 
		{ 
			intersectedNodes.Add(n);
			return; 
		}

		float txM = 0.5f * (tx0 + tx1); 
		float tyM = 0.5f * (ty0 + ty1); 

		int currNode = find_firstNode(tx0,ty0,txM,tyM);


		// Note, this is based on the assumption that the children are ordered in a particular 
		// manner.  Different Quadtree libraries will have to adjust. 
		do {
			switch(currNode) {
			case 0:
				proc_subtree(tx0,ty0,txM,tyM, n.Children[a ^ 0], intersectedNodes, a); //x0y0
				currNode = next_node(txM,tyM,1,2); 
				break;
			case 1:
				proc_subtree(tx0,tyM,txM,ty1, n.Children[a ^ 1], intersectedNodes, a); //x0y1
				currNode = next_node(txM,tyM,3,4); 
				break;
			case 2:
				proc_subtree(txM,ty0,tx1,tyM, n.Children[a ^ 2], intersectedNodes, a); //x1y0
				currNode = next_node(txM,tyM,4,3); 
				break;
			case 3:
				proc_subtree(txM,tyM,tx1,ty1, n.Children[a ^ 3], intersectedNodes, a); //x1y1
				currNode = next_node(txM,tyM,4,4); 
				break;
			} 
		} while(currNode < 4);
	}


}
}


