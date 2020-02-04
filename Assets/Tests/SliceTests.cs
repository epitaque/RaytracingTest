using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace Tests
{
    public class SliceTests
    {
		[Test]
		public void TestChildDescriptor() {
			var cd = new RT.SL.ChildDescriptor(17, 131, 15);
			var cd2 = new RT.SL.ChildDescriptor(cd.GetCode());
			Assert.AreEqual(17, cd2.childPointer);
			Assert.AreEqual(15, cd2.nonLeafMask);
			Assert.AreEqual(131, cd2.validMask);
		}

        // A Test behaves as an ordinary method
        [Test]
        public void SliceTestsSimplePasses()
        {
			List<int> svo = new List<int>();
			svo.Add(new RT.SL.ChildDescriptor(0, 255, 0).GetCode());
            bool[][,,] slices = RT.SL.SliceGenerator.GetSlices(SampleFunctions.functions[(int)SampleFunctions.Type.Sphere], 6);
			for(int i = 2; i < slices.Length; i++) {
				bool[,,] slice = slices[i];
				RT.SL.SliceBasedSVO.AddSlice(svo, slice);
			}
			System.Text.StringBuilder text = new System.Text.StringBuilder("[SliceTest] Result:\n");
			svo.ForEach(i => text.AppendLine(new RT.SL.ChildDescriptor(i).ToString()));
			Debug.Log(text);
			string path = System.IO.Directory.GetParent(Application.dataPath).FullName + "\\Logs\\slicetests.log";
			System.IO.File.WriteAllText(path, text.ToString());
        }

    }
}
