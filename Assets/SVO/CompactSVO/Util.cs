using UnityEngine;
using System;

namespace RT.CS {
public class Node : SVONode {
	public Node(Vector3 position, double size, int level, bool leaf) {
		Position = position;
		Size = size;
		Level = level;
		Leaf = leaf;
	}

	public Node[] Children;

	public string StringifyHierarchy() {
		string result = "";
		result += ToString();
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

	public bool Valid(int childNum) { return (validMask & (1 << childNum)) != 0; }
	public bool Leaf(int childNum) { return (leafMask & (1 << childNum)) != 0; }

	public static uint ToCode(uint childPointer, uint validMask, uint leafMask) {
		return (uint)(childPointer | (validMask << 16) | (leafMask << 24));
	}

	public override string ToString() {
		return "[ChildDescriptor childPointer: " + childPointer + ", validMask: " + Convert.ToString(validMask, 2).PadLeft(8, '0') + ", leafMask: " + Convert.ToString(leafMask, 2).PadLeft(8, '0') + "]";
	}

	static ChildDescriptor() {
		string result = "Child Descriptor Test";
		uint childPointer_ = 5, validMask_ = 1, leafMask_ = 1;
		result += "Child Pointer: " + childPointer_ + ", Valid Mask: " + validMask_ + ", Leaf Mask: " + leafMask_ + "\n";
		uint code = ToCode(childPointer_, validMask_, leafMask_);
		result += "Code: " + code + " or " + Convert.ToString(code, 2) + "\n";
		result += "Converted back: " + new ChildDescriptor(code).ToString() + "\n"; 
		Debug.Log(result); 
	}
}
}