using UnityEngine;
using System.Collections.Generic;
using RT.CS;

/*
    Class that stores the SVO in the following format
    child pointer | valid mask | leaf mask
        16			   8			8

    Child pointer:
     - Points to the first child

    Status of child in given slot:
     - Neither bit is set: the slot is not intersected by a surface, and is therefore empty.
     - The bit in valid mask is set: the slot contains a non-leaf voxel that is subdivided further.
     - Both bits are set: the slot contains a leaf voxel.

 */
namespace RT {
public class CompactSVO : SVO {
    private List<int> svo;
	private UtilFuncs.Sampler sample;
    private int maxLevel;

	private CompactSVOCreator creator;
	private CompactSVOTracer tracer;

	public interface CompactSVOCreator {
		List<int> Create(UtilFuncs.Sampler sample, int maxLevel);
	}
	public interface CompactSVOTracer {
		List<SVONode> Trace(Ray ray, List<int> svo);
		List<SVONode> GetAllNodes(List<int> svo);
	}

	public CompactSVO(UtilFuncs.Sampler sample, int maxLevel) {
		this.creator = new NaiveCreator();
		this.tracer = new NVIDIAIterativeNaiveTracer();

		Create(sample, maxLevel);
	}

	public CompactSVO(UtilFuncs.Sampler sample, int maxLevel, CompactSVOCreator creator, CompactSVOTracer tracer) {
		this.creator = creator;
		this.tracer = tracer;

		Create(sample, maxLevel);
	}

	public void Create(UtilFuncs.Sampler sample, int maxLevel) {
		this.maxLevel = maxLevel;
		this.sample = sample;

		BuildSVO();
	}

	public void BuildSVO() {
		svo = creator.Create(sample, maxLevel);
	}

    public List<SVONode> Trace(UnityEngine.Ray ray) {
        return tracer.Trace(ray, svo);
    }
    public List<SVONode> GetAllNodes() {
        return tracer.GetAllNodes(svo);
    }

    public void DrawGizmos(float scale) {
        
    }
}
}
