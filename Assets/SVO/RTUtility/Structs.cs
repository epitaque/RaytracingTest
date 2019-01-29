using UnityEngine;
using System.Collections.Generic;

namespace RT {
/*public class SVO {
	// structure:
	// child pointer | valid mask | leaf mask
	//    16			   8			8
	public uint[] ChildDescriptors;
	public List<ColoredBox> GizmoBoxes;
}*/

public struct ColoredBox {
	public Color Color;
	public Vector3 Center;
	public Vector3 Size;

	public override string ToString() {
		return "[ColoredBox, Center: " + Center.ToString("F4") + ", Size: " + Size.ToString("F4") + "]";
	}
}

public struct ChildDescriptorInfo {
	public ushort ChildPointer;
	public byte ValidMask;
	public byte LeafMask;
}

// uncompressed, naive way to store nodes
public class SVONode {
	public float Size;
	public bool Leaf;
	public int Level;
	public Vector3 Position;

	public SVONode() {}
	public SVONode(Vector3 position, float size, bool leaf, int level) {
		Size = size;
		Leaf = leaf;
		Level = level;
		Position = position;
	}

	public virtual ColoredBox GetColoredBox() {
		ColoredBox box = new ColoredBox();
		box.Center = GetCenter();
		box.Color = UtilFuncs.SinColor(Level * 2f);
		box.Color.a = 0.07f;
		box.Size = Vector3.one * (float)Size;
		return box;		
	}

	public Vector3 GetCenter() {
		return Position + Vector3.one * ((float)Size / 2);
	}

	public SVONode GetChild(int childNum, bool leaf) {
		Debug.Assert(!Leaf);
		return new SVONode(Position + Constants.vfoffsets[childNum] * (float)Size/2, (float)Size/2, leaf, Level + 1);
	}

	public override string ToString() {
		return "[Node, Position " + Position.ToString("F4") + ", Size: " + Size + ", Leaf: " + Leaf + ", Level: " + Level + "]";
	}
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
