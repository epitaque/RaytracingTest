using UnityEngine;

namespace RT {
	public interface SVO {
		void Create(UtilFuncs.Sampler sampler, int depth);
		Vector4[] Trace(UnityEngine.Ray ray);
		ColoredBox[] GenerateDebugBoxes(bool onlyShowLeaves);
		ColoredBox[] GenerateDebugBoxesAlongRay(Ray ray, bool onlyShowLeaves);
	}
}