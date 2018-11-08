using System.Collections.Generic;
using UnityEngine;

namespace RT.CS {
public class DescriptorTracer : CompactSVO.CompactSVOTracer {
	public List<SVONode> Trace(Ray ray, List<uint> svo) {
		return null;
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
		if(node.Leaf){
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
		Vector3 nodeMax = node.Position + Vector3.one * (float)node.Size;
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
 		double tx0 = (node.Position.x - rayOrigin.x) * divx;
		double tx1 = (nodeMax.x - rayOrigin.x) * divx;
		double ty0 = (node.Position.y - rayOrigin.y) * divy;
		double ty1 = (nodeMax.y - rayOrigin.y) * divy;
		double tz0 = (node.Position.z - rayOrigin.z) * divz;
		double tz1 = (nodeMax.z - rayOrigin.z) * divz;

 		if(Mathd.Max(tx0,ty0,tz0) < Mathd.Min(tx1,ty1,tz1)){ 		
			ProcSubtree(rayOrigin, rayDirection, tx0,ty0,tz0,tx1,ty1,tz1,node,intersectedNodes, a);
		}
	}

	public List<SVONode> GetAllNodes(List<uint> svo) {
		return null;
	}
}
}