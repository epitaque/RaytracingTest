using UnityEngine;

namespace RT {
	public interface SVO {
		void Create(UtilFuncs.Sampler sampler, int depth);
		SVONode[] Trace(UnityEngine.Ray ray);

		/*
			General debugging methods that apply to all SVOs
		 */
		ColoredBox[] GenerateDebugBoxes(bool onlyShowLeaves);
		ColoredBox[] GenerateDebugBoxesAlongRay(Ray ray, bool onlyShowLeaves);

		/*
			Draw any implementation-specific gizmos
		 */
		void DrawGizmos(float scale);
	}
}