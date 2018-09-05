using UnityEngine;

namespace RT {
public class SVOCreatorNaive : SVOCreator {
	private class Node {
		public Vector3 Position;
		public int Size;

	}

	public SVO Create(UtilFuncs.Sampler sampler, int depth) {
		SVO svo = new SVO();

		return svo;
	}


}
}
