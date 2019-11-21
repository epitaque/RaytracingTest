using UnityEngine;
using System;

namespace RT.CS {
public class Node : SVONode {
	public Boolean errorNode;
	public Node[] children;
	public Vector3 normal;
	public Color color;

	public Node(Vector3 position, float size, int level, bool leaf) {
		base.position = position;
		base.size = size;
		base.level = level;
		base.leaf = leaf;
		errorNode = false;
		color = Color.red;
		normal = Vector3.up;
	}

	public Node(Vector3 position, float size, int level, bool leaf, Vector3 normal, Color color) {
		base.position = position;
		base.size = size;
		base.level = level;
		base.leaf = leaf;
		errorNode = false;
		this.normal = normal;
		this.color = color;
	}

	public override ColoredBox GetColoredBox() {
		ColoredBox box = new ColoredBox();
		box.Center = GetCenter();
		if(!errorNode) {
			box.Color = color; //UtilFuncs.SinColor(Level * 2f);
		}
		else {
			box.Color = Color.red;
		}
		box.Color.a = 0.07f;
		box.Size = Vector3.one * (float)size;
		return box;		
	}

	public string StringifyHierarchy() {
		string result = "";
		result += ToString() + "\n";
		if(children != null) {
			for(int i = 0; i < 8; i++) {
				Node n = children[i];
				if(n != null) {
					result += n.StringifyHierarchy();
				}
			}
		}
		return result;
	}
}

public class ChildDescriptor {
	public ushort childPointer;
	public byte validMask;
	public byte nonLeafMask;

	public ChildDescriptor(int code) {
		this.childPointer = (ushort)(code >> 16);
		this.validMask = (byte)((code >> 8) & 255);
		this.nonLeafMask = (byte)((code >> 0) & 255);
	}

	public ChildDescriptor(ushort childPointer, byte validMask, byte nonLeafMask) {
		this.childPointer = childPointer;
		this.validMask = validMask;
		this.nonLeafMask = nonLeafMask;
	}

	public bool Valid(int childNum) { return (validMask & (1 << childNum)) != 0; }
	public bool Leaf(int childNum) { return (nonLeafMask & (1 << childNum)) == 0; }

	public static int ToCode(int childPointer, int validMask, int nonLeafMask) {
		return (int)(childPointer | (validMask << 16) | (nonLeafMask << 24));
	}

	public override string ToString() {
		return "[ChildDescriptor childPointer: " + childPointer + ", validMask: " + Convert.ToString(validMask, 2).PadLeft(8, '0') + ", nonLeafMask: " + Convert.ToString(nonLeafMask, 2).PadLeft(8, '0') + "]";
	}
}
}