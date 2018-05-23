using UnityEngine;

namespace RT {
public static class Constants {
	public static Vector3[] vfoffsets = {
		new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0), new Vector3(0, 1, 1),
		new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(1, 1, 0), new Vector3(1, 1, 1)
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