using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace RT {
	public interface SVO {
		void Create(UtilFuncs.Sampler sample, int maxDepth);
		List<SVONode> Trace(UnityEngine.Ray ray);
		List<SVONode> GetAllNodes();

		/*
			Draw any implementation-specific gizmos
		 */
		void DrawGizmos(float scale);
	}
}