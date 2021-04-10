
SimpleAccelerationStructure _SimpleAccelerationStructure(int num, StructuredBuffer<SphereData> data)
{
	SimpleAccelerationStructure SAS;

	SAS.NumObjects = num;
	SAS.ASData = data;
	
	return SAS;
}

SimpleAccelerationTriangle _SimpleAccelerationTriangle(int num, StructuredBuffer<TriangleData> data)
{
	SimpleAccelerationTriangle SAT;

	SAT.TriangleNums = num;
	SAT.ASTData = data;
	
	return SAT;
}

bool SimpleAccelerationStructure_Hit(SimpleAccelerationStructure sas, Ray r, float t_min, float t_max, inout HitRecord rec)
{
	HitRecord TempRec = _HitRecord();
	bool bHitAnything = false;
	float ClosestT = t_max;

	for (int i = 0; i < sas.NumObjects; i++)
	{
		Sphere S = _Sphere(sas.ASData[i].Center, sas.ASData[i].Radius, _Material(sas.ASData[i].MaterialType, sas.ASData[i].MaterialAlbedo, sas.ASData[i].MaterialData.x));
		if (Sphere_Hit(S, r, t_min, ClosestT, TempRec))
		{
			bHitAnything = true;
			ClosestT = TempRec.t;
			rec = TempRec;
		}		
	}
	return bHitAnything;
}

bool SimpleAccelerationTriangle_Hit(SimpleAccelerationTriangle sat, Ray r, float t_min, float t_max, inout HitRecord rec)
{
	HitRecord TempRec = _HitRecord();
	bool bHitAnything = false;
	float ClosestT = t_max;

	for (int i = 0; i < sat.TriangleNums; i++)
	{
		TriangleMesh TriMesh = _TriangleMesh(sat.ASTData[i].v0, sat.ASTData[i].v1, sat.ASTData[i].v2 , sat.ASTData[i].normal, _Material(sat.ASTData[i].MaterialType, sat.ASTData[i].MaterialAlbedo, sat.ASTData[i].MaterialData.x));
		if (Triangle_Hit(TriMesh, r, t_min, ClosestT, TempRec))
		{
			bHitAnything = true;
			ClosestT = TempRec.t;
			rec = TempRec;
		}		
	}
	return bHitAnything;
}
