using UnityEngine;

public static class SampleFunctions {
	public enum Type {
		FlatGround,
		Sphere,
		Simplex,
		RotatedCuboid,
		Custom1,
		Custom2
	}
	
	public static UtilFuncs.Sampler[] functions;
    public static SE.OpenSimplexNoise simplex;

	static SampleFunctions() {
		functions = new UtilFuncs.Sampler[6];
		simplex = new SE.OpenSimplexNoise(7);
		// Flat ground
		functions[0] = (float x, float y, float z) => {
			return y - 1.5f;
		};
		// Sphere
		functions[1] = (float x, float y, float z) => {
			Vector3 p = new Vector3((x-1.5f)*2f, (y-1.5f)*2f, (z-1.5f)*2f);
			return Sphere(p.x, p.y, p.z, 1f);
		};
		// Simplex
		functions[2] = (float x, float y, float z) => {
			float r = 2f;
			return (float)simplex.Evaluate(x * r, y * r, z * r);
		};
		// Rotated Cuboid
		functions[3] = (float x, float y, float z) => {
			Vector3 p = new Vector3((x-1.5f)*2f, (y-1.5f)*2f, (z-1.5f)*2f);
			return RotatedCuboid(p, 0.6f);
		};
	}

	public static float Sphere(float x, float y, float z, float r) {
		return x * x + y * y + z * z - r * r;
	}

	public static float RotatedCuboid(Vector3 p, float radius)
	{
		p = Matrix4x4.Rotate(Quaternion.Euler(45, 45, 45)).MultiplyVector(p);
		return Cuboid(p, radius);
	}


	public static float Cuboid(Vector3 p, float radius)
	{
		Vector3 local = new Vector3(p.x, p.y, p.z);
		Vector3 d = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z)) - new Vector3(radius, radius, radius);
		float m = Mathf.Max(d.x, Mathf.Max(d.y, d.z));
		Vector3 max = d;
		return Mathf.Min(m, Vector3.Magnitude(max));
	}


}