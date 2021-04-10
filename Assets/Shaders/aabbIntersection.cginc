
float isHitAABB(StructuredBuffer<AABB> box, Ray r)
{
	float3 ray=normalize(r.Dir);
	float in_Xaxis_time=0.0;
	float out_Xaxis_time=0.0;
	float in_Yaxis_time=0.0;
	float out_Yaxis_time=0.0;
	float in_Zaxis_time=0.0;
	float out_Zaxis_time=0.0;
	

	if( abs(box[0].X_axis.x - r.Orig.x) > abs(box[0].X_axis.y - r.Orig.x) ){
		in_Xaxis_time=(box[0].X_axis.y - r.Orig.x)/ray.x;
		out_Xaxis_time=(box[0].X_axis.x - r.Orig.x)/ray.x;
	}
	else{
		in_Xaxis_time=(box[0].X_axis.x - r.Orig.x)/ray.x;
		out_Xaxis_time=(box[0].X_axis.y - r.Orig.x)/ray.x;
	}

	if( abs(box[0].Y_axis.x - r.Orig.y) > abs(box[0].Y_axis.y - r.Orig.y) ){
		in_Yaxis_time=(box[0].Y_axis.y - r.Orig.y)/ray.y;
		out_Yaxis_time=(box[0].Y_axis.x - r.Orig.y)/ray.y;
	}
	else{
		in_Yaxis_time=(box[0].Y_axis.x - r.Orig.y)/ray.y;
		out_Yaxis_time=(box[0].Y_axis.y - r.Orig.y)/ray.y;
	}

	if( abs(box[0].Z_axis.x - r.Orig.z) > abs(box[0].Z_axis.y - r.Orig.z) ){
		in_Zaxis_time=(box[0].Z_axis.y - r.Orig.z)/ray.z;
		out_Zaxis_time=(box[0].Z_axis.x - r.Orig.z)/ray.z;
	}
	else{
		in_Zaxis_time=(box[0].Z_axis.x - r.Orig.z)/ray.z;
		out_Zaxis_time=(box[0].Z_axis.y - r.Orig.z)/ray.z;
	}


	float max_in  = in_Xaxis_time;
	float min_out = out_Xaxis_time;

	if(max_in < in_Yaxis_time){
		max_in=in_Yaxis_time;
		if(max_in<in_Zaxis_time){
			max_in=in_Zaxis_time;
		}
	}
	else if(max_in < in_Zaxis_time){
		max_in=in_Zaxis_time;
	}


	if(min_out > out_Yaxis_time){
		min_out=out_Yaxis_time;
		if(min_out>out_Zaxis_time){
			min_out=out_Zaxis_time;
		}
	}
	else if(min_out > out_Zaxis_time){
		min_out=out_Zaxis_time;
	}

	if ((max_in<min_out) && min_out>=0.0){
		return 1.0;
	}
	

	return -1.0;
}