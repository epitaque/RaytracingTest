using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using RT;

public class SVOTests {
	[Test]
	public void RaytraceMultipleNodes() {
		SVO svo = new NaiveSVO(SampleFunctions.functions[(int)SampleFunctions.Type.Sphere], 5);

		Ray[] rays = new Ray[6];
		rays[0] = new Ray(new Vector3(-0.6538f, 0.0000f, 0.0000f), new Vector3(0.9999f, 0.0091f, 0.0083f));
		rays[1] = new Ray(new Vector3(-1.2281f, -0.0238f, 0.0000f), new Vector3(0.9998f, 0.0174f, 0.0062f));
		rays[2] = new Ray(new Vector3(1.1388f, -0.0238f, 0.0000f), new Vector3(-0.9588f, 0.2678f, 0.0950f));
		rays[3] = new Ray(new Vector3(1.1388f, -0.0238f, -0.3106f), new Vector3(-0.3909f, 0.1092f, 0.9139f));
		rays[4] = new Ray(new Vector3(1.1388f, -0.4569f, -0.3106f), new Vector3(-0.8954f, 0.3670f, 0.2523f));
		rays[5] = new Ray(new Vector3(0.3325f, 0.3350f, 0.4438f), new Vector3(-0.5412f, -0.5020f, -0.6746f));

		string[] strings = new string[6];
		strings[0] = "[Node, Position (0.8750, 0.0000, 0.0000), Size: 0.125, Leaf: True, Level: 4]";
		strings[1] = "[Node, Position (-1.0000, -0.1250, 0.0000), Size: 0.125, Leaf: True, Level: 4][Node, Position (0.8750, 0.0000, 0.0000), Size: 0.125, Leaf: True, Level: 4]";
		strings[2] = "[Node, Position (0.8750, 0.0000, 0.0000), Size: 0.125, Leaf: True, Level: 4][Node, Position (-0.7500, 0.5000, 0.1250), Size: 0.125, Leaf: True, Level: 4]";
		strings[3] = "[Node, Position (0.8750, 0.0000, 0.0000), Size: 0.125, Leaf: True, Level: 4][Node, Position (0.8750, 0.0000, 0.1250), Size: 0.125, Leaf: True, Level: 4][Node, Position (0.8750, 0.0000, 0.2500), Size: 0.125, Leaf: True, Level: 4][Node, Position (0.7500, 0.0000, 0.3750), Size: 0.125, Leaf: True, Level: 4][Node, Position (0.7500, 0.0000, 0.5000), Size: 0.125, Leaf: True, Level: 4][Node, Position (0.6250, 0.0000, 0.6250), Size: 0.125, Leaf: True, Level: 4]";
		strings[4] = "[Node, Position (0.7500, -0.3750, -0.2500), Size: 0.125, Leaf: True, Level: 4][Node, Position (-0.8750, 0.2500, 0.1250), Size: 0.125, Leaf: True, Level: 4][Node, Position (-0.8750, 0.2500, 0.2500), Size: 0.125, Leaf: True, Level: 4]";
		strings[5] = "[Node, Position (-0.6250, -0.5000, -0.7500), Size: 0.125, Leaf: True, Level: 4]";

		for(int i = 0; i < rays.Length; i++) {
			string result = "";
			SVONode[] nodes = svo.Trace(rays[i]);
			foreach(SVONode n in nodes) {
				result += n.ToString();
			}

			Assert.AreEqual(strings[i], result);
		}
	}
}
