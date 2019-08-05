using UnityEngine;

public class Clipmap : MonoBehaviour {
	public Chunk chunk1;
	public Chunk chunk2;

	void Start() {
		chunk1 = new Chunk();
		chunk2 = new Chunk();

		chunk1.svo = new RT.CompactSVO(SampleFunctions.functions[1], 4);
		chunk2.svo = new RT.CompactSVO(SampleFunctions.functions[1], 4);

		chunk1.offset = Vector3.zero;
		chunk2.offset = Vector3.one;
	}
}