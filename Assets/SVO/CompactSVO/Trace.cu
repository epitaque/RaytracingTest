__device__ void cast_ray(
	int* root, // In: Octree root (pointer to global mem).
	volatile float3& p, // In: Ray origin (shared mem).
	volatile float3& d, // In: Ray direction (shared mem).
	volatile float& ray_size_coef, // In: LOD at ray origin (shared mem).
	float ray_size_bias, // In: LOD increase along ray (register).
	float& hit_t, // Out: Hit t-value (register).
	float3& hit_pos, // Out: Hit position (register).
	int*& hit_parent, // Out: Hit parent voxel (pointer to global mem).
	int& hit_idx, // Out: Hit child slot index (register).
	int& hit_scale) // Out: Hit scale (register).
{
	const int s_max = 23; // Maximum scale (number of float mantissa bits).
	const float epsilon = exp2f(-s_max);
	int2 stack[s_max + 1]; // Stack of parent voxels (local mem).

	// Get rid of small ray direction components to avoid division by zero.
	if (fabsf(d.x) < epsilon) d.x = copysignf(epsilon, d.x);
	if (fabsf(d.y) < epsilon) d.y = copysignf(epsilon, d.y);
	if (fabsf(d.z) < epsilon) d.z = copysignf(epsilon, d.z);

	// Precompute the coefficients of tx(x), ty(y), and tz(z).
	// The octree is assumed to reside at coordinates [1, 2].
	float tx_coef = 1.0f / -fabs(d.x);
	float ty_coef = 1.0f / -fabs(d.y);
	float tz_coef = 1.0f / -fabs(d.z);
	float tx_bias = tx_coef * p.x;
	float ty_bias = ty_coef * p.y;
	float tz_bias = tz_coef * p.z;

	// Select octant mask to mirror the coordinate system so
	// that ray direction is negative along each axis.
	int octant_mask = 7;
	if (d.x > 0.0f) octant_mask ˆ= 1, tx_bias = 3.0f * tx_coef - tx_bias;
	if (d.y > 0.0f) octant_mask ˆ= 2, ty_bias = 3.0f * ty_coef - ty_bias;
	if (d.z > 0.0f) octant_mask ˆ= 4, tz_bias = 3.0f * tz_coef - tz_bias;

	// Initialize the active span of t-values.
	float t_min = fmaxf(fmaxf(2.0f * tx_coef - tx_bias, 2.0f * ty_coef - ty_bias), 2.0f * tz_coef - tz_bias);
	float t_max = fminf(fminf(tx_coef - tx_bias, ty_coef - ty_bias), tz_coef - tz_bias);
	float h = t_max;
	t_min = fmaxf(t_min, 0.0f);
	t_max = fminf(t_max, 1.0f);

	// Initialize the current voxel to the first child of the root.
	int* parent = root;
	int2 child_descriptor = make_int2(0, 0); // invalid until fetched
	int idx = 0;
	float3 pos = make_float3(1.0f, 1.0f, 1.0f);
	int scale = s_max - 1;
	float scale_exp2 = 0.5f; // exp2f(scale - s_max)

	if (1.5f * tx_coef - tx_bias > t_min) idx ˆ= 1, pos.x = 1.5f;
	if (1.5f * ty_coef - ty_bias > t_min) idx ˆ= 2, pos.y = 1.5f;
	if (1.5f * tz_coef - tz_bias > t_min) idx ˆ= 4, pos.z = 1.5f;

	// Traverse voxels along the ray as long as the current voxel
	// stays within the octree.
	while (scale < s_max)
	{
		// Fetch child descriptor unless it is already valid.
		if (child_descriptor.x == 0)
			child_descriptor = *(int2*)parent;

		// Determine maximum t-value of the cube by evaluating
		// tx(), ty(), and tz() at its corner.
		float tx_corner = pos.x * tx_coef - tx_bias;
		float ty_corner = pos.y * ty_coef - ty_bias;
		float tz_corner = pos.z * tz_coef - tz_bias;
		float tc_max = fminf(fminf(tx_corner, ty_corner), tz_corner);

		// Process voxel if the corresponding bit in valid mask is set
		// and the active t-span is non-empty.
		int child_shift = idx ˆ octant_mask; // permute child slots based on the mirroring
		int child_masks = child_descriptor.x << child_shift;

		if ((child_masks & 0x8000) != 0 && t_min <= t_max)
		{
			// Terminate if the voxel is small enough.
			if (tc_max * ray_size_coef + ray_size_bias >= scale_exp2)
				break; // at t_min

			// INTERSECT
			// Intersect active t-span with the cube and evaluate
			// tx(), ty(), and tz() at the center of the voxel.
			float tv_max = fminf(t_max, tc_max);
			float half = scale_exp2 * 0.5f;
			float tx_center = half * tx_coef + tx_corner;
			float ty_center = half * ty_coef + ty_corner;
			float tz_center = half * tz_coef + tz_corner;

			// Descend to the first child if the resulting t-span is non-empty.
			if (t_min <= tv_max)
			{
				// Terminate if the corresponding bit in the non-leaf mask is not set.
				if ((child_masks & 0x0080) == 0)
					break; // at t_min (overridden with tv_min).
					
				// PUSH
				// Write current parent to the stack.
				if (tc_max < h)
					stack[scale] = make_int2((int)parent, __float_as_int(t_max));
				h = tc_max;

				// Find child descriptor corresponding to the current voxel.
				int ofs = (unsigned int)child_descriptor.x >> 17; // child pointer
				if ((child_descriptor.x & 0x10000) != 0) // far
					ofs = parent[ofs * 2]; // far pointer
				ofs += popc8(child_masks & 0x7F);
				parent += ofs * 2;

				// Select child voxel that the ray enters first.
				idx = 0;
				scale--;
				scale_exp2 = half;
				if (tx_center > t_min) idx ˆ= 1, pos.x += scale_exp2;
				if (ty_center > t_min) idx ˆ= 2, pos.y += scale_exp2;
				if (tz_center > t_min) idx ˆ= 4, pos.z += scale_exp2;

				// Update active t-span and invalidate cached child descriptor.
				t_max = tv_max;
				child_descriptor.x = 0;
				continue;
			}
		}
		
		// ADVANCE
		// Step along the ray.
		int step_mask = 0;
		if (tx_corner <= tc_max) step_mask ˆ= 1, pos.x -= scale_exp2;
		if (ty_corner <= tc_max) step_mask ˆ= 2, pos.y -= scale_exp2;
		if (tz_corner <= tc_max) step_mask ˆ= 4, pos.z -= scale_exp2;

		// Update active t-span and flip bits of the child slot index.
		t_min = tc_max;
		idx ˆ= step_mask;

		// Proceed with pop if the bit flips disagree with the ray direction.
		if ((idx & step_mask) != 0)
		{
			// POP
			// Find the highest differing bit between the two positions.
			unsigned int differing_bits = 0;
			if ((step_mask & 1) != 0) differing_bits |= __float_as_int(pos.x) ˆ __float_as_int(pos.x + scale_exp2);
			if ((step_mask & 2) != 0) differing_bits |= __float_as_int(pos.y) ˆ __float_as_int(pos.y + scale_exp2);
			if ((step_mask & 4) != 0) differing_bits |= __float_as_int(pos.z) ˆ __float_as_int(pos.z + scale_exp2);
			scale = (__float_as_int((float)differing_bits) >> 23) - 127; // position of the highest bit
			scale_exp2 = __int_as_float((scale - s_max + 127) << 23); // exp2f(scale - s_max)

			// Restore parent voxel from the stack.
			int2 stackEntry = stack[scale];
			parent = (int*)stackEntry.x;
			t_max = __int_as_float(stackEntry.y);

			// Round cube position and extract child slot index.
			int shx = __float_as_int(pos.x) >> scale;
			int shy = __float_as_int(pos.y) >> scale;
			int shz = __float_as_int(pos.z) >> scale;
			pos.x = __int_as_float(shx << scale);
			pos.y = __int_as_float(shy << scale);
			pos.z = __int_as_float(shz << scale);
			idx = (shx & 1) | ((shy & 1) << 1) | ((shz & 1) << 2);

			// Prevent same parent from being stored again and invalidate cached child descriptor.
			h = 0.0f;
			child_descriptor.x = 0;
		}
	}

	// Indicate miss if we are outside the octree.
	if (scale >= s_max)
		t_min = 2.0f;

	// Undo mirroring of the coordinate system.
	if ((octant_mask & 1) == 0) pos.x = 3.0f - scale_exp2 - pos.x;
	if ((octant_mask & 2) == 0) pos.y = 3.0f - scale_exp2 - pos.y;
	if ((octant_mask & 4) == 0) pos.z = 3.0f - scale_exp2 - pos.z;

	// Output results.
	hit_t = t_min;
	hit_pos.x = fminf(fmaxf(p.x + t_min * d.x, pos.x + epsilon), pos.x + scale_exp2 - epsilon);
	hit_pos.y = fminf(fmaxf(p.y + t_min * d.y, pos.y + epsilon), pos.y + scale_exp2 - epsilon);
	hit_pos.z = fminf(fmaxf(p.z + t_min * d.z, pos.z + epsilon), pos.z + scale_exp2 - epsilon);
	hit_parent = parent;
	hit_idx = idx ˆ octant_mask ˆ 7;
	hit_scale = scale;
}
