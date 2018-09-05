using UnityEngine;

namespace RT {
	public interface SVOCreator {
		SVO Create(UtilFuncs.Sampler sampler, int depth);
	}

	public interface SVOTracer {
		Vector4[] TraceNodes(UnityEngine.Ray ray);
	}
}