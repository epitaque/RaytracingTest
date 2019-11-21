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
	public float size;
	public bool leaf;
	public int level;
	public Vector3 position;

	public SVONode() {}
	public SVONode(Vector3 position, float size, bool leaf, int level) {
		this.size = size;
		this.leaf = leaf;
		this.level = level;
		this.position = position;
	}

	public virtual ColoredBox GetColoredBox() {
		ColoredBox box = new ColoredBox();
		box.Center = GetCenter();
		box.Color = UtilFuncs.SinColor(level * 2f);
		box.Color.a = 0.07f;
		box.Size = Vector3.one * (float)size;
		return box;		
	}

	public Vector3 GetCenter() {
		return position + Vector3.one * ((float)size / 2);
	}

	public SVONode GetChild(int childNum, bool leaf) {
			Debug.Assert(!this.leaf);
		return new SVONode(position + Constants.vfoffsets[childNum] * (float)size/2, (float)size/2, leaf, level + 1);
	}

	public override string ToString() {
		return "[Node, Position " + position.ToString("F4") + ", Size: " + size + ", Leaf: " + leaf + ", Level: " + level + "]";
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
