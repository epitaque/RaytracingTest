using UnityEngine;
using System.Collections.Generic;

namespace RT {
public class SVO {
	// structure:
	// child pointer | valid mask | leaf mask
	//    16			   8			8
	public uint[] ChildDescriptors;
	public List<ColoredBox> GizmoBoxes;
}

public struct ColoredBox {
	public Color Color;
	public Vector3 Center;
	public Vector3 Size;
}

public struct ChildDescriptorInfo {
	public ushort ChildPointer;
	public byte ValidMask;
	public byte LeafMask;
}

// uncompressed, naive way to store nodes
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

// expanded form of child descriptor
public class Voxel {
	public bool PartiallyFull;
	public bool CompletelyFull;

	public byte LeafMask;
	public byte ValidMask;
	public ushort ChildPointer;

	public int Level;
}

}
