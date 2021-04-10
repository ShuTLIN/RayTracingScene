TriangleMesh _TriangleMesh(float3 v0, float3 v1,float3 v2, float3 normal,Material m)
{
	TriangleMesh T;

	T.v0 = v0;
	T.v1 = v1;
	T.v2 = v2;
	T.normal=normal;
    T.Material=m;

	return T;
}

bool Triangle_Hit(TriangleMesh Tri, Ray r, float t_min, float t_max, inout HitRecord rec)
{
    

	float NdotRayDirection = dot(Tri.normal,r.Dir); 
    if (sqrt(pow(NdotRayDirection,2.0)) < 0.01){
        return false; // they are parallel so they don't intersect ! 
    }

    
    float d = dot(Tri.normal,Tri.v0); 
    float t = (-dot(Tri.normal,r.Orig) + d) / NdotRayDirection; 


    // check if the triangle is in behind the ray
    if (t < 0){
        return false; // the triangle is behind 
    } 
    float3 P = r.Orig + t * r.Dir; 
 
    // Step 2: inside-outside test
    float3 C; // vector perpendicular to triangle's plane 
 
    // edge 0
    float3 edge0 = Tri.v1 - Tri.v0; 
    float3 vp0 = normalize(P - Tri.v0); 
    C = cross(edge0,vp0); 
    if (dot(Tri.normal,C) < 0){
        return false; // P is on the right side 
    } 
 
    // edge 1
    float3 edge1 = Tri.v2 - Tri.v1; 
    float3 vp1 = normalize(P - Tri.v1); 
    C = cross(edge1,vp1); 
    if (dot(Tri.normal,C) < 0){
        return false; // P is on the right side
    }   
 
    // edge 2
    float3 edge2 = Tri.v0 - Tri.v2; 
    float3 vp2 = normalize(P - Tri.v2); 
    C = cross(edge2,vp2); 
    if (dot(Tri.normal,C) < 0){
        return false; // P is on the right side; 
    } 
 
    if (t < t_min || t_max < t)
	{
		return false;
	}
    rec.t = t;
	rec.P = Ray_At(r, rec.t);
	HitRecord_SetFaceNormal(rec, r, Tri.normal);
    rec.Material = Tri.Material;
    return true; // this ray hits the triangle 
}
