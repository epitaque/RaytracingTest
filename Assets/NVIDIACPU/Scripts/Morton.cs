public static class Morton {
	public static ulong mortonEncode(int x, int y, int z)
	{
		ulong answer = 0;
		for (ulong i = 0; i < (sizeof(int) * 8); ++i)
		{
			answer |= (ulong)(((x & (int)((ulong)1 << (int)i)) << (int)(2 * i)) | ((y & (int)((ulong)1 << (int)i)) << (int)(2 * i + 1)) | ((z & (int)((ulong)1 << (int)i)) << (int)(2 * i + 2)));
		}
		return answer;
	}

	public static void mortonDecode(ulong morton, ref int x, ref int y, ref int z)
	{
		x = 0;
		y = 0;
		z = 0;
		for (ulong i = 0; i < (sizeof(ulong) * 8) / 3; ++i)
		{
			x |= (int)((morton & (1ul << (int)((3ul * i) + 0ul))) >> (int)(((3ul * i) + 0ul) - i));
			y |= (int)((morton & (1ul << (int)((3ul * i) + 1ul))) >> (int)(((3ul * i) + 1ul) - i));
			z |= (int)((morton & (1ul << (int)((3ul * i) + 2ul))) >> (int)(((3ul * i) + 2ul) - i));
		}
	}

}