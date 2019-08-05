using UnityEngine;

namespace RT {
public static class Constants {
	public static int blockSize = 8192; // 8192 * 4 bytes (32kb)


	public static Vector3[] vdirections = {
		new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, -1, 0),
		new Vector3(0, 0, 1), new Vector3(0, 0, -1)
	};

	public static Vector3[] vfoffsets2 = {
		new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0), new Vector3(0, 1, 1),
		new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(1, 1, 0), new Vector3(1, 1, 1)
	};

	public static Vector3[] vfoffsets = {
		new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0),
		new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 1)
	};

	public static Vector3[] vfoffsets3 = {
		new Vector3(1, 1, 1), new Vector3(0, 1, 1), new Vector3(1, 0, 1), new Vector3(0, 0, 1),  
		new Vector3(1, 1, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 0),  
	};

}
}

namespace QT {
public static class Constants {
	public static Vector3[] qoffsets = { // Quadtree node offsets
		new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0)
	};
}
}

namespace OT {
public static class Constants {
	public static Vector3[] qoffsets = { // Quadtree node offsets
		new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0), new Vector3(0, 1, 1),
		new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(1, 1, 0), new Vector3(1, 1, 1)
	};
}
}

namespace ST {
public static class Constants {
	public static Vector3[] vfoffsets = {
		new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0),
		new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 1)
	};

}
}