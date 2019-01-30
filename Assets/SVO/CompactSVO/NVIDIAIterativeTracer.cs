using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using RT.CS;

namespace RT.CS {
public class NVIDIAIterativeNaiveTracer : CompactSVO.CompactSVOTracer {
	private Node ExpandSVO(List<int> svo, List<Node> svoNodes) {
		for(int i = 0; i < svo.Count; i++) {
			svoNodes.Add(null);
		}
		Node root = new Node(new Vector3(1, 1, 1), 1, 1, false);
		ExpandSVOAux(root, 0, 1, svo, svoNodes);
		return root;
	}

	/*
	    child pointer | valid mask | nonleaf mask
            16			   8			8
	 */
	private void ExpandSVOAux(Node node, int nodeIndex, int level, List<int> svo, List<Node> svoNodes) { 
		ChildDescriptor descriptor = new ChildDescriptor(svo[nodeIndex]); 
 
		node.Children = new Node[8];
		int pointer = descriptor.childPointer;
		float half = node.Size/2f;

		if(svoNodes[nodeIndex] == null) {
			svoNodes[nodeIndex] = node;
		}

		for(int childNum = 0; childNum < 8; childNum++) { 
			if(descriptor.Valid(childNum)) {
				bool leaf = descriptor.Leaf(childNum);

				Node child = new Node(node.Position + Constants.vfoffsets[childNum] * (float)(half), half, level + 1, leaf);
				node.Children[childNum] = child;

				

				if(!leaf) {
					ExpandSVOAux(node.Children[childNum], pointer++, level + 1, svo, svoNodes);
				}
			}
		}
	}

	/*
		Ray Tracing methods
		Returns a list of nodes that intersect a ray (in sorted order)
	 */
	public List<SVONode> Trace(UnityEngine.Ray ray, List<int> svo) {
		List<Node> allSvoNodes = new List<Node>();
		ExpandSVO(svo, allSvoNodes);

		List<Node> intersectedNodes = new List<Node>();
		if(ray.direction.x != 0) {
			RayStep(svo, ray.origin, ray.direction, intersectedNodes, allSvoNodes);
		}
		return intersectedNodes.ConvertAll(node => (SVONode)node).ToList();
	}

	class StackData {
		public int x;
		public float y;

		public StackData(int x, float y) {
			this.x = x;
			this.y = y;
		}
	}

	private void RayStep(List<int> svo, Vector3 rayOrigin, Vector3 rayDirection, List<Node> intersectedNodes, List<Node> allSvoNodes)  {
		//Debug.Log("Tracing svo hierarchy\n" + allSvoNodes[])
		string debugStr = "";
		debugStr += string.Join("\n", svo.ConvertAll(code => new ChildDescriptor(code)));
		debugStr += "\nNeatly Printed Hierarchy:\n";
		debugStr += allSvoNodes[0].StringifyHierarchy();

		GUIUtility.systemCopyBuffer = debugStr;


		int s_max = 23;
		float epsilon = Mathf.Epsilon;
		StackData[] stack = new StackData[s_max + 1];

		float tx_coef = 1.0f / -Mathf.Abs(rayDirection.x);
		float ty_coef = 1.0f / -Mathf.Abs(rayDirection.y);
		float tz_coef = 1.0f / -Mathf.Abs(rayDirection.z);

		float tx_bias = tx_coef * rayOrigin.x;
		float ty_bias = ty_coef * rayOrigin.y;
		float tz_bias = tz_coef * rayOrigin.z;

		int octant_mask = 7;
		if (rayDirection.x > 0.0f) {
			octant_mask ^= 1;
			tx_bias = 3.0f * tx_coef - tx_bias;
		}
		if (rayDirection.y > 0.0f) {
			octant_mask ^= 2; 
			ty_bias = 3.0f * ty_coef - ty_bias;
		}
		if (rayDirection.z > 0.0f) {
			octant_mask ^= 4; 
			tz_bias = 3.0f * tz_coef - tz_bias;
		}

		float t_min = Mathf.Max(2.0f * tx_coef - tx_bias, 2.0f * ty_coef - ty_bias, 2.0f * tz_coef - tz_bias);
		float t_max = Mathf.Min(tx_coef - tx_bias, ty_coef - ty_bias, tz_coef - tz_bias);
		float h = t_max;
		t_min = Mathf.Max(t_min, 0.0f);
		t_max = Mathf.Min(t_max, 1.0f);


		int parent = 0; // pointer to parent
		int child_descriptor = 0; // invalid until fetched

		int idx = 0;
		Vector3 pos = new Vector3(1.0f, 1.0f, 1.0f); // position of first child of root
		int scale = s_max - 1; // scale of "child" (22 at root)
		float scale_exp2 = 0.5f; // exp2f(scale - s_max)

		if (1.5f * tx_coef - tx_bias > t_min) { 
			idx ^= 1; pos.x = 1.5f; 
		}
		if (1.5f * ty_coef - ty_bias > t_min) { 
			idx ^= 2; pos.y = 1.5f; 
		}
		if (1.5f * tz_coef - tz_bias > t_min) { 
			idx ^= 4; pos.z = 1.5f; 
		}

		List<int> parentsVisited = new List<int>();

		while (scale < s_max)
		{
			if(parent < allSvoNodes.Count) {
				//intersectedNodes.Add(allSvoNodes[parent]);
				Vector3 nodePos = pos;

				if ((octant_mask & 1) == 0) nodePos.x = 3.0f - scale_exp2 - nodePos.x;
				if ((octant_mask & 2) == 0) nodePos.y = 3.0f - scale_exp2 - nodePos.y;
				if ((octant_mask & 4) == 0) nodePos.z = 3.0f - scale_exp2 - nodePos.z;

				parentsVisited.Add(parent);
				//intersectedNodes.Add(new Node(nodePos, scale_exp2, 24 - scale, false));
			}
			// Fetch child descriptor unless it is already valid.
			if (child_descriptor == 0) {
				child_descriptor = svo[parent];
			}

			ChildDescriptor neatcd = new ChildDescriptor(child_descriptor);

			// Determine maximum t-value of the cube by evaluating
			// tx(), ty(), and tz() at its corner.
			float tx_corner = pos.x * tx_coef - tx_bias;
			float ty_corner = pos.y * ty_coef - ty_bias;
			float tz_corner = pos.z * tz_coef - tz_bias;
			float tc_max = Mathf.Min(tx_corner, ty_corner, tz_corner);

			int child_shift = idx ^ octant_mask; // permute child slots based on the mirroring
			int child_num = child_shift ^ 7; // Purely for seeing the index of the child in the debugger. Unused variable
			int child_masks = child_descriptor << child_shift;

			if ((child_masks & 0x8000) != 0 && t_min <= t_max)
			{
				// INTERSECT
				// Intersect active t-span with the cube and evaluate
				// tx(), ty(), and tz() at the center of the voxel.
				float tv_max = Mathf.Min(t_max, tc_max);
				float half = scale_exp2 * 0.5f;
				float tx_center = half * tx_coef + tx_corner;
				float ty_center = half * ty_coef + ty_corner;
				float tz_center = half * tz_coef + tz_corner;

				// Descend to the first child if the resulting t-span is non-empty.
				if (t_min <= tv_max)
				{
					// Terminate if the corresponding bit in the non-leaf mask is not set.
					if ((child_masks & 0x0080) == 0) {
						break; // at t_min (overridden with tv_min).
					}
						
					// PUSH
					// Write current parent to the stack.
					if (tc_max < h)
						stack[scale] = new StackData(parent, t_max);
					h = tc_max;

					// Find child descriptor corresponding to the current voxel.
					// child_descriptor format: first 16 bits is child pointer
					// next 8 is valid, next 8 is leaf

					int ofs = (child_descriptor >> 16); // child pointer
					ofs += popc8(child_masks & 0x7F); // finds (bits - 1) of shifted non-leaf mask
					parent = ofs;

					// Select child voxel that the ray enters first.
					idx = 0;
					scale--;
					scale_exp2 = half;
					if (tx_center > t_min) { idx ^= 1; pos.x += scale_exp2; }
					if (ty_center > t_min) { idx ^= 2; pos.y += scale_exp2; }
					if (tz_center > t_min) { idx ^= 4; pos.z += scale_exp2; }

					// Update active t-span and invalidate cached child descriptor.
					t_max = tv_max;
					child_descriptor = 0;
					continue;
				}
			}

			// ADVANCE
			// Step along the ray.
			int step_mask = 0;
			if (tx_corner <= tc_max) { step_mask ^= 1; pos.x -= scale_exp2; }
			if (ty_corner <= tc_max) { step_mask ^= 2; pos.y -= scale_exp2; }
			if (tz_corner <= tc_max) { step_mask ^= 4; pos.z -= scale_exp2; }

			// Update active t-span and flip bits of the child slot index.
			t_min = tc_max;
			idx ^= step_mask;

			// Proceed with pop if the bit flips disagree with the ray direction.
			if ((idx & step_mask) != 0)
			{
				// POP
				// Find the highest differing bit between the two positions.
				int differing_bits = 0;
				if ((step_mask & 1) != 0) differing_bits |= __float_as_int(pos.x) ^ __float_as_int(pos.x + scale_exp2);
				if ((step_mask & 2) != 0) differing_bits |= __float_as_int(pos.y) ^ __float_as_int(pos.y + scale_exp2);
				if ((step_mask & 4) != 0) differing_bits |= __float_as_int(pos.z) ^ __float_as_int(pos.z + scale_exp2);
				scale = (__float_as_int((float)differing_bits) >> 23) - 127; // position of the highest bit
				scale_exp2 = __int_as_float((scale - s_max + 127) << 23); // exp2f(scale - s_max)

				// Restore parent voxel from the stack.
				StackData stackEntry = stack[scale];
				if(stackEntry  != null) {
					parent = stackEntry.x;
					t_max = stackEntry.y;
				}

				// Round cube position and extract child slot index.
				int shx = __float_as_int(pos.x) >> scale;
				int shy = __float_as_int(pos.y) >> scale;
				int shz = __float_as_int(pos.z) >> scale;
				pos.x = __int_as_float(shx << scale);
				pos.y = __int_as_float(shy << scale);
				pos.z = __int_as_float(shz << scale);
				idx = (shx & 1) | ((shy & 1) << 1) | ((shz & 1) << 2);

				// Prevent same parent from being stored again and invalidate cached child descriptor.
				h = 0.0f;
				child_descriptor = 0;
			}

		}
		// Indicate miss if we are outside the octree.
		if (scale >= s_max)
			t_min = 2.0f;

		// Undo mirroring of the coordinate system.
		if ((octant_mask & 1) == 0) pos.x = 3.0f - scale_exp2 - pos.x;
		if ((octant_mask & 2) == 0) pos.y = 3.0f - scale_exp2 - pos.y;
		if ((octant_mask & 4) == 0) pos.z = 3.0f - scale_exp2 - pos.z;

		// Output results.
		/* hit_t = t_min;
		hit_pos.x = fminf(fmaxf(p.x + t_min * d.x, pos.x + epsilon), pos.x + scale_exp2 - epsilon);
		hit_pos.y = fminf(fmaxf(p.y + t_min * d.y, pos.y + epsilon), pos.y + scale_exp2 - epsilon);
		hit_pos.z = fminf(fmaxf(p.z + t_min * d.z, pos.z + epsilon), pos.z + scale_exp2 - epsilon);
		hit_parent = parent;
		hit_idx = idx ˆ octant_mask ˆ 7;
		hit_scale = scale;*/

		//Vector3 hit_pos = new Vector3()

		if(scale >= s_max) {

		}
		else {
			Debug.Log("Hit_t: " + t_min);
			Debug.Log("Hit parents: " + string.Join(", ", parentsVisited));
			Debug.Log("Hit idx: " + (idx ^ octant_mask ^ 7));
			intersectedNodes.Add(allSvoNodes[parent].Children[idx ^ octant_mask ^ 7]);
		}

	
	}
	/*
		Debug Methods
	 */

	public List<SVONode> GetAllNodes(List<int> svo) {
		List<Node> allNodes = new List<Node>();
		List<SVONode> nodes = new List<SVONode>();
		//ExpandSVO(svo, allNodes);
		//testRoot = ExpandSVO(svo, allNodes);
		GetAllNodesAux(ExpandSVO(svo, allNodes), nodes);
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

	private int[] c_popc8LUT =   // CUDA CONST
	{
		0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4,
		1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
		1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
		2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
		1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
		2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
		2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
		3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
		1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
		2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
		2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
		3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
		2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
		3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
		3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
		4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 7, 6, 7, 7, 8,
	};

	private int popc8(int mask)
	{
		return c_popc8LUT[mask & 0xFFu];
	}

	private static int __float_as_int(float x) {
		float[] pos_f = new float[] { x };
		int[] pos_i = new int[1];

		Buffer.BlockCopy(pos_f, 0, pos_i, 0, 1 * 4);
		return pos_i[0];
	}

	private static float __int_as_float(int x) {
		int[] pos_i = new int[1] { x };
		float[] pos_f = new float[1];

		Buffer.BlockCopy(pos_i, 0, pos_f, 0, 1 * 4);
		return pos_f[0];
	}

	private static int __int_as_int(int x) {
		int[] pos_ui = new int[1] { x };
		int[] pos_i = new int[1];

		Buffer.BlockCopy(pos_ui, 0, pos_i, 0, 1 * 4);
		return pos_i[0];
	}

	private static int __uint_as_int(int x) {
		int[] pos_i = new int[1] { x };
		int[] pos_ui = new int[1];

		Buffer.BlockCopy(pos_i, 0, pos_ui, 0, 1 * 4);
		return pos_ui[0];
	}


	static NVIDIAIterativeNaiveTracer() {
		//int test = 2; 
		//Debug.Log("test " + test + ": " + __float_as_int(test));

	}
}
}