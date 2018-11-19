using UnityEngine;
using System;

namespace RT.CS {
public class Node : SVONode {
	public Node(Vector3 position, double size, int level, bool leaf) {
		Position = position;
		Size = size;
		Level = level;
		Leaf = leaf;
		ErrorNode = false;
	}

	public Boolean ErrorNode;
	public Node[] Children;

	public override ColoredBox GetColoredBox() {
		ColoredBox box = new ColoredBox();
		box.Center = GetCenter();
		if(!ErrorNode) {
			box.Color = UtilFuncs.SinColor(Level * 2f);
		}
		else {
			box.Color = Color.red;
		}
		box.Color.a = 0.07f;
		box.Size = Vector3.one * (float)Size;
		return box;		
	}

	public string StringifyHierarchy() {
		string result = "";
		result += ToString() + "\n";
		if(Children != null) {
			for(int i = 0; i < 8; i++) {
				Node n = Children[i];
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
	public byte leafMask;

	public ChildDescriptor(uint code) {
		this.childPointer = (ushort)(code & 65535);
		this.validMask = (byte)((code >> 16) & 255);
		this.leafMask = (byte)(code >> 24);
	}

	public ChildDescriptor(ushort childPointer, byte validMask, byte leafMask) {
		this.childPointer = childPointer;
		this.validMask = validMask;
		this.leafMask = leafMask;
	}

	public bool Valid(int childNum) { return (validMask & (1 << childNum)) != 0; }
	public bool Leaf(int childNum) { return (leafMask & (1 << childNum)) != 0; }

	public static uint ToCode(uint childPointer, uint validMask, uint leafMask) {
		return (uint)(childPointer | (validMask << 16) | (leafMask << 24));
	}

	public override string ToString() {
		return "[ChildDescriptor childPointer: " + childPointer + ", validMask: " + Convert.ToString(validMask, 2).PadLeft(8, '0') + ", leafMask: " + Convert.ToString(leafMask, 2).PadLeft(8, '0') + "]";
	}
}
}