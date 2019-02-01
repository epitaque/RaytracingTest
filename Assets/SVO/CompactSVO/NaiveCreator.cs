using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RT.CS {
public class NaiveCreator : CompactSVO.CompactSVOCreator {


	public SVOData Create(UtilFuncs.Sampler sample, int maxLevel) {
		SVOData result = new SVOData();
		Node root = new Node(new Vector3(1, 1, 1), 1, 1, false);
		BuildTree(root, 1, sample, maxLevel);

		List<int> nodes = new List<int>();
		List<uint> attachments = new List<uint>();

		CompressSVO(root, nodes, attachments);

		result.childDescriptors = nodes;
		result.attachments = attachments;

		return result;
	}
	/*
		Tree building methods

		Constructs subtree from node.
		Will return null if node does not contain surface.
		Will return node if node contains surface.
	 */
	private Node BuildTree(Node node, int level, UtilFuncs.Sampler sample, int maxLevel) {
		/*
		
		Grid[cx,cy,cz].Normal.X := (Grid[cx-1, cy, cz].Value-Grid[cx+1, cy, cz].Value)
		Grid[cx,cy,cz].Normal.Y := (Grid[cx, cy-1, cz].Value-Grid[cx, cy+1, cz].Value)
		Grid[cx,cy,cz].Normal.Z := (Grid[cx, cy, cz-1].Value-Grid[cx, cy, cz+1].Value)


		 */

		// Node is leaf. Determine if within surface. If so, return node.
		if(node.Leaf) {
			Vector3 p = node.Position + Vector3.one * (float)node.Size / 2;
			if(sample(p.x, p.y, p.z) <= 0 && IsEdge(node, sample)) {
				// Node is on an edge, calculate normal and color
				Vector3 normal = Vector3.zero;
				float h = 0.001f;
				normal.x = sample(p.x - h, p.y, p.z) - sample(p.x, p.y, p.z);
				normal.y = sample(p.x, p.y - h, p.z) - sample(p.x, p.y, p.z);
				normal.z = sample(p.x, p.y, p.z - h) - sample(p.x, p.y, p.z);
				normal = Vector3.Normalize(normal);
				node.Normal = normal;
				node.Color = new Color(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z), 1.0f);
				//node.Color = new Color(node.Position.x - 1, node.Position.y - 1, node.Position.z - 1, 1.0f);

				return node;
			}
		}


		// Node is not leaf. Construct 8 children. If any of them intersect surface, return node.
		else {
			bool childExists = false;
			int numLeaves = 0;
			node.Children = new Node[8];
			float half = node.Size/2;

			for(int i = 0; i < 8; i++) {
				Node child = new Node(node.Position + Constants.vfoffsets[i] * (float)(half), half, level + 1, level + 1 == maxLevel);
				node.Children[i] = BuildTree(child, level + 1, sample, maxLevel);

				if(node.Children[i] != null) {
					childExists = true;
					if(node.Children[i].Leaf) {
						numLeaves++;
					}
				}
			}

			if(childExists) {
				// Compute average color and normal
				int numChildren = 0;
				Vector3 color = Vector3.zero;
				Vector3 normal = Vector3.zero;
				for(int i = 0; i < 8; i++) {

					if(node.Children[i] != null) {
						numChildren++;
						color.x += node.Children[i].Color.r;
						normal.x += node.Children[i].Normal.x;
					}
				}

				color = color * (1 / numChildren);
				normal = normal * (1 / numChildren);

				node.Color = new Color(color.x, color.y, color.z);
				node.Normal = normal;
				return node;
			}
		}
		return null;
	}

	// Given that node resides inside the surface, detects if it's an edge voxel (has air next to it)
	private bool IsEdge(Node node, UtilFuncs.Sampler sample) {
		foreach(Vector3 direction in Constants.vdirections) {
			Vector3 pos = node.GetCenter() + direction * (float)(node.Size);
			float s = sample(pos.x, pos.y, pos.z);
			if(s > 0) {
				return true;
			}
		}
		return false;
	}

    private void CompressSVO(Node root, List<int> compressedNodes, List<uint> attachments) {
		compressedNodes.Add(0);
		attachments.Add(0); attachments.Add(0);
        CompressSVOAux(root, 0, compressedNodes, attachments);
    } 

    private void CompressSVOAux(Node node, int nodeIndex, List<int> compressedNodes, List<uint> attachments) {
        if(node == null || node.Leaf) { return; }

		int childPointer = 0;
        int validMask = 0;
        int leafMask = 0;

		int numHitChild = 0;

		List<Color> childColors = new List<Color>();

        for(int childNum = 0; childNum < 8; childNum++) { 
            int bit = (int)1 << childNum;

            if(node.Children[childNum] != null) {
                validMask |= bit;

                if(node.Children[childNum].Leaf) {
                    leafMask |= bit;
                }
				else {
					if(childPointer == 0) {
						childPointer = (int)compressedNodes.Count;
					}
					compressedNodes.Add(0); 
					attachments.Add(0); attachments.Add(0);
				}
            }
        }

		int childPointerClone = (int)childPointer;
		for(int childNum = 0; childNum < 8; childNum++) {
			if(node.Children[childNum] != null && !node.Children[childNum].Leaf) {
				CompressSVOAux(node.Children[childNum], childPointerClone++, compressedNodes, attachments);
			}
		}

		int nonLeafMask = leafMask ^ 255;
		nonLeafMask &= validMask;

        int result = (childPointer << 16) | (validMask << 8) | (nonLeafMask << 0);

		long attachment = GetAttachment(node);
		attachments[nodeIndex * 2] = ((uint)attachment);
		attachments[nodeIndex * 2 + 1] = ((uint)(attachment >> 32));
		compressedNodes[nodeIndex] = result;
    }

	public static long GetAttachment(Node node) {
		long attachment = 0;
		int inodeAColor = 0; // 16 bits to encode node A color (5 6 5)
		int inodeBColor = 0; // 16 bits to encode node B color (5 6 5)
		int icolorChoices = 0; // 16 bits node color choices, chosen from {A, B, .66A + .33A, .33A + .67B}
		int inormal = 0; // 1 bit sign, 2 bits axis, 7 bits u coordinate on unit cube, 6 bits v coordinate 

		Vector3 normal = Vector3.zero;
		Vector3 colorSum = Vector3.zero; 

		Vector3 nodeAColor = Vector3.zero;
		Vector3 nodeBColor = Vector3.zero;



		int numChildren = 0;
		float bdist = 0;

		for(int i = 0; i < 8; i++) {
			Node child = node.Children[i];
			if(child == null) continue;
			numChildren++;
			normal += child.Normal;
			Vector3 vColor = new Vector3(child.Color.r, child.Color.g, child.Color.b);
			colorSum += vColor;

			if(numChildren == 1) {
				nodeAColor = vColor;
			} else {
				float dist = Vector3.Distance(nodeAColor, vColor);
				if(dist > bdist) {
					nodeBColor = vColor;
				}
			}
		}

		inodeAColor = CompressColor(nodeAColor);
		inodeBColor = CompressColor(nodeBColor);
		normal = Vector3.Normalize(normal * (1f / numChildren));
		inormal = CompressNormal(normal);

		Vector3[] candidateColors = new Vector3[] { nodeAColor, nodeBColor, .667f * nodeAColor + .333f * nodeBColor, .333f * nodeAColor + .667f * nodeBColor };

		for(int i = 0; i < 8; i++) {
			Node child = node.Children[i];
			if(child == null) continue;
			Vector3 vColor = new Vector3(child.Color.r, child.Color.g, child.Color.b);
			float shortestDistance = 100;
			int choice = 0;

			for(int j = 0; j < 4; j++) {
				float dist = Vector3.Distance(vColor, candidateColors[j]);
				if(dist < shortestDistance) {
					shortestDistance = dist;
					choice = j;
				}

			}
			icolorChoices |= choice << (i * 2);

		}

		attachment = (long)inodeAColor | ((long)inodeBColor << 16) | ((long)icolorChoices << 32) | ((long)inormal << 48);

		return attachment;
	}


	/*
		Original Child Node colors and normals
		C0 is null
		C1 color: RGBA(0.900, 0.500, 0.300, 1.000), normal: (0.0, 1.0, 0.0)
		C2 color: RGBA(0.300, 0.100, 0.200, 1.000), normal: (0.0, 1.0, 0.0)
		C3 color: RGBA(0.100, 0.800, 0.400, 1.000), normal: (0.8, 0.8, 0.0)
		C4 is null
		C5 is null
		C6 is null
		C7 is null

		Raw attachment: 1111111111111111111111111111111111100110111010110100110001111100
		NodeAColor: 0100110001111100
		NodeBColor: 1110011011101011
		ColorChoices: 1111111111111111
		Normal: 11111111111111111111111111111111

		Partially Decompressed Attachment
		NodeAColor: (0.9, 0.6, 0.3)
		NodeBColor: (0.4, 0.9, 0.9)
		ColorChoices: 1110011011101011
		Normal: (0.0, 0.0, 0.0)

		Actual colors of children
		C0: (0.5, 0.8, 0.7)
		C1: (0.9, 0.6, 0.3)
		C2: (0.9, 0.6, 0.3)
		C3: (0.9, 0.6, 0.3)
		C4: (0.9, 0.6, 0.3)
		C5: (0.9, 0.6, 0.3)
		C6: (0.9, 0.6, 0.3)
		C7: (0.9, 0.6, 0.3)

*/

	public static void TestDecompressAttachment() {
		Node testNode = new Node(Vector3.zero, 1, 1, false, Vector3.up, Color.gray);
		testNode.Children = new Node[] { null, 
			new Node(Vector3.zero, 1, 1, false, new Vector3(0, 1, 0), new Color(0.9f, 0.5f, 0.3f)), 
			new Node(Vector3.zero, 1, 1, false, new Vector3(0, 1, 0), new Color(0.3f, 0.1f, 0.2f)),
			new Node(Vector3.zero, 1, 1, false, new Vector3(0.77f, 0.77f, 0), new Color(0.1f, 0.8f, 0.4f)),
			null, null, null, null};

		string output = "Original Child Node colors and normals\n";

		for(int i = 0; i < 8; i++) {
			Node n = testNode.Children[i];
			if(n == null) {
				output += "C" + i + " is null\n";
			} else {
				output += "C" + i + " color: " + n.Color + ", normal: " + n.Normal + "\n";
			}
		}

		long attachment = GetAttachment(testNode);
		int inodeAColor = (int)(attachment & 65535); // 16 bits to encode node A color (5 6 5)
		int inodeBColor = (int)((attachment >> 16) & 65535); // 16 bits to encode node B color (5 6 5)
		int icolorChoices = (int)((attachment >> 32) & 65535); // 16 bits node color choices, chosen from {A, B, .66A + .33A, .33A + .67B}
		int inormal = (int)((attachment >> 48)); // 1 bit sign, 2 bits axis, 7 bits u coordinate on unit cube, 6 bits v coordinate

		output += "\nRaw attachment: " + Convert.ToString(attachment, 2).PadLeft(64, '0') + "\n";
		output += "NodeAColor: " + Convert.ToString(inodeAColor, 2).PadLeft(16, '0') + "\n";
		output += "NodeBColor: " + Convert.ToString(inodeBColor, 2).PadLeft(16, '0') + "\n";
		output += "ColorChoices: " + Convert.ToString(icolorChoices, 2).PadLeft(16, '0') + "\n";
		output += "Normal: " + Convert.ToString(inormal, 2).PadLeft(16, '0') + "\n\n";

		Vector3 nodeAColor = DecompressColor(inodeAColor);
		Vector3 nodeBColor = DecompressColor(inodeBColor);

		output += "Partially Decompressed Attachment\n";
		output += "NodeAColor: " + DecompressColor(inodeAColor) + "\n";
		output += "NodeBColor: " + DecompressColor(inodeBColor) + "\n";
		output += "ColorChoices: " + Convert.ToString(icolorChoices, 2).PadLeft(16, '0') + "\n";
		output += "Normal: " + DecompressNormal(inormal) + "\n\n";

		Vector3[] candidateColors = new Vector3[] { nodeAColor, nodeBColor, .667f * nodeAColor + .333f * nodeBColor, .333f * nodeAColor + .667f * nodeBColor };

		output += "Actual colors of children\n";
		for(int i = 0; i < 8; i++) {
			int choice = (icolorChoices >> (i * 2)) & 3;
			Vector3 color = decodeDXTColor((uint)inodeAColor | ((uint)inodeBColor << 16), ((uint)icolorChoices), i); //candidateColors[choice];
			output += "C" + i + ": choice: " + choice + ", color: " + color + "\n";
		}

		Debug.Log(output);
	}

	/*
		Color and Normal Compression Utility Functions
	 */
	// compress a color to 16 bits (R5 G6 B5)
	public static int CompressColor(Vector3 c) {
		int color = (int)(32 * (c.x - 0.00001f));
		color |= (int)(64 * (c.y - 0.00001f)) << 5;
		color |= (int)(32 * (c.z - 0.00001f)) << 11;
		return color;
	}
	public static Vector3 DecompressColor(int color) {
		float r = (color & 31);
		float g = ((color >> 5) & 63);
		float b = (color >> 11);
		return new Vector3(r / 31f, g / 63f, b / 31f);
	}

	// nvidias color decompression functions test
	private static float[] c_dxtColorCoefs =
	{
		1.0f / (float)(1 << 24),
		0.0f,
		2.0f / (float)(3 << 24),
		1.0f / (float)(3 << 24),
	};
 
	private static Vector3 decodeDXTColor(uint block1, uint block2, int texelIdx)
	{
		uint head = block1;
		uint bits = block2;

		float c0 = c_dxtColorCoefs[(bits >> (texelIdx * 2)) & 3];
		float c1 = 1.0f / (float)(1 << 24) - c0;

		return new Vector3(
			c0 * (float)(head << 27) + c1 * (float)(head << 11),
			c0 * (float)(head << 21) + c1 * (float)(head << 5),
			c0 * (float)(head << 16) + c1 * (float)head) * (1f/256f);
	}



	public static float ColorDistance(Color a, Color b) {
		Vector3 pA = new Vector3(a.r, a.g, a.b);
		Vector3 pB = new Vector3(b.r, b.g, b.b);
		return Vector3.Distance(pA, pB);
	}

	// compress a normal to 16 bits
	// 1 bit: sign
	// 2 bits: axis
	// 00 x
	// 01 y
	// 10 z
	// 7 bits: u coord
	// 6 bits: v coord

	public static int CompressNormal(Vector3 n) {  
		int compressed = 0;
		int axis = 0;
		int sign = 0;
		float fsign = 0;

		n.x += 0.0000001f; 
		n.y += 0.0000007f;

		// pos = n * t
		// pos.x = 1
		// 1 = n.x * t
		// 1 / n.x = t

		float t = 0;
		float u = 0;
		float v = 0;

		float anx = Mathf.Abs(n.x);
		float any = Mathf.Abs(n.y);
		float anz = Mathf.Abs(n.z);

		if(anz < anx && any < anx) { // exit plane YZ axisv x
			compressed |= 0; 
			fsign = n.x;
			t = 1 / anx;
			u = (n * t).y; 
			v = (n * t).z;
		}
		else if(anx < any && anz < any) { // exit plane XZ axisv y
			axis |= 1;
			fsign = n.y;
			t = 1 / any;
			u = (n * t).x;
			v = (n * t).z;
		}
		else if(anx < anz && any < anz) { // exit plane XY axisv z (can remove conditional, only plane left)
			axis |= 2;
			fsign = n.z;
			t = 1 / anz; 
			u = (n * t).x;
			v = (n * t).y;
		}

		if(fsign > 0) {
			sign |= 1;
		}

		u += 1;
		v += 1;

		int iu = (int)((u - Mathf.Epsilon) * 64); // 7 bit precision
		int iv = (int)((v - Mathf.Epsilon) * 32); // 6 bit precision

		compressed = sign | (axis << 1) | (iu << 3) | (iv << 10);

		return compressed; 
	}

	public static Vector3 DecompressNormal(int compressedNormal) { 
		Vector3 normal = Vector3.zero;
		Vector3 unitCubePoint = Vector3.zero;
		int sign = compressedNormal & 1;
		int axis = (compressedNormal >> 1) & 3;
		int u = (compressedNormal >> 3) & 127;
		int v = (compressedNormal >> 10) & 63;

		float signMultiplier = ((float)sign - 0.5f) * 2;

		if(axis == 0) { // x axis
			unitCubePoint.x = signMultiplier;
			unitCubePoint.y = ((float)u - 64f) / 64f + (1f / 128f);
			unitCubePoint.z = ((float)v - 32f) / 32f + (1f / 64f); 
		}

		if(axis == 1) { // y axis
			unitCubePoint.y = signMultiplier;
			unitCubePoint.x = ((float)u - 64f) / 64f + (1f / 128f);
			unitCubePoint.z = ((float)v - 32f) / 32f + (1f / 64f); 
		}

		if(axis == 2) { // z axis
			unitCubePoint.z = signMultiplier;
			unitCubePoint.x = ((float)u - 64f) / 64f + (1f / 128f);
			unitCubePoint.y = ((float)v - 32f) / 32f + (1f / 64f); 
		}
		normal = Vector3.Normalize(unitCubePoint);
		return normal;
	}

	float3 decodeRawNormal(int value)
	{
		int sign = (int)value >> 15;
		F32 t = (F32)(sign ^ 0x7fffffff);
		F32 u = (F32)((S32)value << 3);
		F32 v = (F32)((S32)value << 18);

		float3 result = { t, u, v };
		if ((value & 0x20000000) != 0)
			result.x = v, result.y = t, result.z = u;
		else if ((value & 0x40000000) != 0)
			result.x = u, result.y = v, result.z = t;
		return result;
	}

	static NaiveCreator() {
		//TestSVOCompaction();
		//TestNormalCompaction();
		TestDecompressAttachment();
	}

	public static void TestNormalCompaction() {
		Vector3 norm = new Vector3(-35, -35, 356);
		norm = Vector3.Normalize(norm);
		int compressed = CompressNormal(norm);

		string output = "Original Normal: " + norm.ToString("F4") + "\n";
		output += "Compressed Normal: " + Convert.ToString(compressed, 2).PadLeft(16, '0') + "\n";
		Vector3 decompressed = DecompressNormal(compressed);
		output += "Decompressed Normal: " + decompressed.ToString("F4");
		Debug.Log(output);
	}

	public static void TestSVOCompaction() {
		NaiveCreator creator = new NaiveCreator();
		List<int> nodes = new List<int>();
		Node root = new Node(new Vector3(-1, -1, -1), 2, 1, false);
		creator.BuildTree(root, 1, SampleFunctions.functions[(int)SampleFunctions.Type.Sphere], 4);
		//nodes = creator.CompressSVO(root);
		string output = "NaiveCreator SVO Compaction Test\n";
		output += "Original Hierarchy:\n" + root.StringifyHierarchy() + "\n\n";
		output += "Compressed:\n" + string.Join("\n", nodes.ConvertAll(code => new ChildDescriptor(code)));
		Debug.Log(output);
	}
}
}