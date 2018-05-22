using UnityEngine;
using System.Collections.Generic;
using Util;

public static class UtilFuncs {
    public static SE.OpenSimplexNoise s = new SE.OpenSimplexNoise(7);

	public static FastNoise myNoise; // Create a FastNoise object

	static UtilFuncs() {
		myNoise = new FastNoise();
		myNoise.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
		myNoise.SetFractalOctaves(2);
		

	}

    public delegate float Sampler(float x, float y, float z);

	public static float max = float.MinValue;
	public static float min = float.MaxValue;

	public static float Noise3DSample(float x, float y, float z) {
        float r = 0.05f;
        float ground = -1.5f + y; 
		float noise = (float)s.Evaluate((double)x * r, (double)y * r, (double)z * r) * 15;

		float result = 0f;
		result += Mathf.Min(ground, noise);
		//result += ground;
		return result;
	}

    public static float FlatGround(float x, float y, float z) {
        return -0.5f + y;
    }

    public static float Sample(float x, float y, float z) {
		//Debug.Log("Sampling at " + x + ", " + y + ", " + z);

        float r = 8f;
		float r2 = 0.05f;
		float result = 0f;
        float ground = -5.5f + y; 

		result += ground;

		//result += (UnityEngine.Mathf.PerlinNoise(x * r2, z * r2) - 0.5f) * 15;

		//result += myNoise.GetNoise(x * r, y * r, z * r) * 50;
        //float noise = myNoise.GetNoise(x * r, z * r) * 50;
        //result -= noise;

		//float cube = RotatedCuboid(new Vector3(x, y - 6, z), 4f);
		//Debug.Log("Cuboid result: " + res);


		//result = ground;

		//float r2 = 1f/50f;
		//result += Sphere(x - 32, y - 32, z - 32, 30);
        //result += (float)s.Evaluate((double)x * r, (double)y * r, (double)z * r) * 15;
		//result = Mathf.Min(ground, cube);

		

		/*if(result > max) {
			max = result;
			Debug.Log("New max result: " + result);
		}
		if(result < min) {
			min = result;
			Debug.Log("New min result: " + result);
		}*/



		//result += res;
        return result;
    }
	public static int mod(int x, int m) {
		return (x%m + m)%m;
	}
	public static float RotatedCuboid(Vector3 q, float radius)
	{
		q = Matrix4x4.Rotate(Quaternion.Euler(45, 45, 45)).MultiplyVector(q);
		return Cuboid(q, radius);
	}

	public static float Cuboid(Vector3 p, float radius)
	{
		Vector3 local = new Vector3(p.x, p.y, p.z);
		Vector3 d = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z)) - new Vector3(radius, radius, radius);
		float m = Mathf.Max(d.x, Mathf.Max(d.y, d.z));
		Vector3 max = d;
		return Mathf.Min(m, Vector3.Magnitude(max));
	}


	public static Vector3 abs(Vector3 p) {
		return new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));
	}

	public static float Sphere(float x, float y, float z, float r) {
		return x * x + y * y + z * z - r * r;
	}

    public static Vector3 Lerp(float isolevel, Point point1, Point point2) {
        if (Mathf.Abs(isolevel-point1.density) < 0.00001)
            return(point1.position);
        if (Mathf.Abs(isolevel-point2.density) < 0.00001)
            return(point2.position);
        if (Mathf.Abs(point1.density-point2.density) < 0.00001)
            return(point2.position);
        float mu = (isolevel - point1.density) / (point2.density - point1.density); 
        return point1.position + mu * (point2.position - point1.position); 
    }
    
    public static Color SinColor(float value) {
        float frequency = 0.3f;
        float red   = Mathf.Sin(frequency*value + 0) * 0.5f + 0.5f;
        float green = Mathf.Sin(frequency*value + 2) * 0.5f + 0.5f;
        float blue  = Mathf.Sin(frequency*value + 4) * 0.5f + 0.5f;
        return new Color(red, green, blue);
    }
}

namespace Util {
    public struct Vector3i {
        public int x;
        public int y;
        public int z;

        public Vector3i(int x, int y, int z) { 
            this.x = x; this.y = y; this.z = z; 
        }
        public int getDimensionSigned(int dim) {
            switch(dim) {
                case 0: return -x;
                case 1: return x;
                case 2: return -y;
                case 3: return y;
                case 4: return -z;
                case 5: return z;
            }
            return -1;
        }
        public int getDimension(int dim) {
            switch(dim) {
                case 0: return x;
                case 1: return y;
                case 2: return z;
            }
            return -1;
        }
        public void setDimension(int dim, int val) {
            switch(dim) {
                case 0: x = val; break;
                case 1: y = val; break;
                case 2: z = val; break;
            }
        }
    }


    public struct GridCell {
        public Point[] points;
        public GridCell Clone() {
            GridCell c = new GridCell();
            c.points = new Point[points.Length];
            for(int i = 0; i < points.Length; i++) {
                c.points[i] = points[i];
            }
            return c;
        }
    }

    public struct Point {
        public Vector3 position;
        public float density;    
    }

    public class ExtractionResult {
        public Mesh mesh;
        public long time;
        public Vector3 offset;
    }

	public class BoundsInt {
		public Vector3Int minExtents;
		public Vector3Int maxExtents;

		public BoundsInt(Vector3Int min, Vector3Int max) {
			minExtents = min;
			maxExtents = max;
		}

		public bool Contains(Vector3 point) {
			if(point.x > minExtents.x && point.x < maxExtents.x &&
				point.y > minExtents.y && point.y < maxExtents.y &&
				point.z > minExtents.z && point.z < maxExtents.z)
			{
				return true;
			}

			//If not, then return false
			return false;
		}

		public bool ContainsInclusive(Vector3 point) {
			if(point.x >= minExtents.x && point.x <= maxExtents.x &&
				point.y >= minExtents.y && point.y <= maxExtents.y &&
				point.z >= minExtents.z && point.z <= maxExtents.z)
			{
				return true;
			}

			//If not, then return false
			return false;
		}

		public bool ContainsPartiallyInclusive(Vector3 point) {
			if(point.x >= minExtents.x && point.x < maxExtents.x &&
				point.y >= minExtents.y && point.y < maxExtents.y &&
				point.z >= minExtents.z && point.z < maxExtents.z)
			{
				return true;
			}

			//If not, then return false
			return false;
		}
	}
}

namespace UnityEngine {
	public struct Vector4Int {
		public int x;
		public int y;
		public int z;
		public int w;
		public Vector4Int(int x, int y, int z, int w) {
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
	}

}