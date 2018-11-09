using System.Collections.Generic;
using UnityEngine;

namespace RT.CS {
public class IterativeDescriptorTracer : CompactSVO.CompactSVOTracer {
    List<SVONode> Trace(Ray ray, List<uint> svo);
    List<SVONode> GetAllNodes(List<uint> svo);  
}
}