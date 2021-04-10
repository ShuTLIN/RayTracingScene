using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RayTracingInOneWeekend : MonoBehaviour
{
    public enum MaterialType
    {
        MAT_LAMBERTIAN  = 0,
        MAT_METAL       = 1,
        MAT_DIELECTRIC  = 2,
    }

    public struct SphereData
    {
        public Vector3 Center;
        public float Radius;
        public int MaterialType;
        public Vector3 MaterialAlbedo;
        public Vector4 MaterialData;
    }

    public struct FaceAttribute
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
        public Vector3 normal;
        public int MaterialType;
        public Vector3 MaterialAlbedo;
        public Vector4 MaterialData;
    }

    public struct AABB
    {
        public Vector2 X_axis;
        public Vector2 Y_axis;
        public Vector2 Z_axis;
    }

    public Mesh m_Mesh;
    public Material m_QuadMaterial;
    public Texture m_Skybox; 
    public ComputeShader m_ComputeShader;
    public Vector2Int m_RTSize;
    private Texture2D noiseTex;
    private Color[] pix;

    RenderTexture m_RTTarget;
    ComputeBuffer m_SimpleAccelerationStructureDataBuffer;
    ComputeBuffer m_VertexDataBuffer;
    ComputeBuffer m_AABBDataBuffer;

    int m_NumSpheres = 0;
    
    SphereData[] m_SphereArray = new SphereData[512];
    float[] m_SphereTimeOffset = new float[512];


    void CalcNoise()
    {
        // For each pixel in the texture...
        float y = 0.0F;

        while (y < noiseTex.height)
        {
            float x = 0.0F;
            while (x < noiseTex.width)
            {
                float xCoord = 0.0f + x / noiseTex.width * 30.0f;
                float yCoord = 0.0f + y / noiseTex.height * 30.0f;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                pix[(int)y * noiseTex.width + (int)x] = new Color(sample, sample, sample);
                x++;
            }
            y++;
        }

        // Copy the pixel data to the texture and load it into the GPU.
        noiseTex.SetPixels(pix);
        noiseTex.Apply();
    }
    void OneVertexCompare(Vector3 Point,ref AABB aabb)
    {
        //find X-axis min-max
        if (Point.x < aabb.X_axis.x)
        {
            aabb.X_axis.x = Point.x;
        }
        if (Point.x > aabb.X_axis.y)
        {
            aabb.X_axis.y = Point.x;
        }

        //find Y-axis min-max
        if (Point.y < aabb.Y_axis.x)
        {
            aabb.Y_axis.x = Point.y;
        }
        if (Point.y > aabb.Y_axis.y)
        {
            aabb.Y_axis.y = Point.y;
        }


        //find Z-axis min-max
        if (Point.z < aabb.Z_axis.x)
        {
            aabb.Z_axis.x = Point.z;
        }
        if (Point.z > aabb.Z_axis.y)
        {
            aabb.Z_axis.y = Point.z;
        }
    }
    AABB FindAABB(ref FaceAttribute[] Mesh)
    {
        //Debug.Log(Mesh.Length);
        AABB tmp = new AABB();
        Vector2 offset = new Vector2(-0.1f,0.1f);
        
        for (int i = 0; i < Mesh.Length; i++)
        {
            if (i == 0)
            {
                tmp.X_axis = new Vector2(Mesh[i].v0.x, Mesh[i].v0.x);
                tmp.Y_axis = new Vector2(Mesh[i].v0.y, Mesh[i].v0.y);
                tmp.Z_axis = new Vector2(Mesh[i].v0.z, Mesh[i].v0.z);
            }
            for (int j = 0; j < 3; j++)
            {
                if (j == 0) { OneVertexCompare(Mesh[i].v0, ref tmp); }
                if (j == 1) { OneVertexCompare(Mesh[i].v1, ref tmp); }
                if (j == 2) { OneVertexCompare(Mesh[i].v2, ref tmp); }
            }
        }

        tmp.X_axis += offset;
        tmp.Y_axis += offset;
        tmp.Z_axis += offset;
        return tmp;
    }

    // Start is called before the first frame update
    void Start()
    {
        
        //render出來的結果會被寫在m_RTTarget中
        //m_RTTarget = new RenderTexture(m_RTSize.x, m_RTSize.y, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
    
        //開出材質空間
        m_RTTarget = new RenderTexture(m_RTSize.x, m_RTSize.y, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
        m_RTTarget.enableRandomWrite = true;
        m_RTTarget.Create();

        m_QuadMaterial.SetTexture("_MainTex", m_RTTarget);

        //init a buffer in cpu menory
        m_SimpleAccelerationStructureDataBuffer = new ComputeBuffer(512, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SphereData)));
        SphereData Data = new SphereData();


        //lambert sphere
        Data.Center = new Vector3(-2.0f, -1.0f, 0.0f);
        Data.Radius = 0.8f;
        Data.MaterialType = (int)MaterialType.MAT_METAL;
        Data.MaterialAlbedo = new Vector3(0.9f, 0.9f, 0.9f);
        Data.MaterialData = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        m_SphereArray[m_NumSpheres] = Data;
        m_SphereTimeOffset[m_NumSpheres] = UnityEngine.Random.Range(0, 100.0f);
        m_NumSpheres++;

        //set data in cpu  memory
        m_SimpleAccelerationStructureDataBuffer.SetData(m_SphereArray);

        //load obj
        m_VertexDataBuffer = new ComputeBuffer(m_Mesh.GetIndices(0).Length / 3, System.Runtime.InteropServices.Marshal.SizeOf(typeof(FaceAttribute)));
        FaceAttribute[] m_VertexData = new FaceAttribute[m_Mesh.GetIndices(0).Length / 3];
        //Debug.Log(m_Mesh.GetIndices(0).Length);
        for (int i=0;i< m_Mesh.GetIndices(0).Length / 3; i++)
        {
            m_VertexData[i].v0 = m_Mesh.vertices[m_Mesh.GetIndices(0)[3*i]];
            m_VertexData[i].v1 = m_Mesh.vertices[m_Mesh.GetIndices(0)[3*i+1]];
            m_VertexData[i].v2 = m_Mesh.vertices[m_Mesh.GetIndices(0)[3*i+2]];

            //m_VertexData[i].v0.y += 1.0f;
            //m_VertexData[i].v1.y += 1.0f;
            //m_VertexData[i].v2.y += 1.0f;

            Vector3 v0v1 = m_VertexData[i].v1 - m_VertexData[i].v0;
            Vector3 v0v2 = m_VertexData[i].v2 - m_VertexData[i].v0;
            Vector3 N = Vector3.Normalize(Vector3.Cross(v0v1, v0v2)); // N
            m_VertexData[i].normal = N;

       
            m_VertexData[i].MaterialType= (int)MaterialType.MAT_METAL;
            m_VertexData[i].MaterialAlbedo = new Vector3(0.9f, 0.9f, 0.9f);
            m_VertexData[i].MaterialData = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);


            //Debug.Log((m_VertexData[i].v0.x, m_VertexData[i].v0.y, m_VertexData[i].v0.z));
            //Debug.Log((m_VertexData[i].v1.x, m_VertexData[i].v1.y, m_VertexData[i].v1.z));
            //Debug.Log((m_VertexData[i].v2.x, m_VertexData[i].v2.y, m_VertexData[i].v2.z));
            //Debug.Log((m_VertexData[i].normal.x, m_VertexData[i].normal.y, m_VertexData[i].normal.z));
            //Debug.Log("---------------------------------------------------------------------------------");

           
        }


        AABB[] mainOBJ_aabb = new AABB[1];
        mainOBJ_aabb[0]=FindAABB(ref m_VertexData);
        Debug.Log( ((mainOBJ_aabb[0].X_axis),(mainOBJ_aabb[0].Y_axis),(mainOBJ_aabb[0].Z_axis)) );
        m_AABBDataBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AABB)));
        m_AABBDataBuffer.SetData(mainOBJ_aabb);

        m_VertexDataBuffer.SetData(m_VertexData);
        float m_TriangleNum = m_Mesh.GetIndices(0).Length / 3;
        int KernelHandle = m_ComputeShader.FindKernel("CSMain");
        m_ComputeShader.SetBuffer(KernelHandle, "FaceAttrib", m_VertexDataBuffer);
        m_ComputeShader.SetBuffer(KernelHandle, "objAABB", m_AABBDataBuffer);
        m_ComputeShader.SetTexture(KernelHandle, "_SkyboxTexture", m_Skybox);
        m_ComputeShader.SetFloat("TriangleNum", m_TriangleNum);

        //procedural texture
        noiseTex = new Texture2D(100, 100);
        pix = new Color[noiseTex.width * noiseTex.height];
        CalcNoise();
        m_ComputeShader.SetTexture(KernelHandle, "_ProceduralTexture", noiseTex);

    }

    // Update is called once per frame
    void Update()
    {
        //because 0~3 is default sphere ,so i=4 
        for (int i = 0; i < m_NumSpheres; i++)
        {
            float dis = Vector3.Distance(new Vector3(0.0f, m_SphereArray[i].Center.y, 0.0f), m_SphereArray[i].Center);
            m_SphereArray[i].Center.x = dis * (UnityEngine.Mathf.Cos(m_SphereTimeOffset[i] + (Time.time)));
            m_SphereArray[i].Center.z = dis * (UnityEngine.Mathf.Sin(m_SphereTimeOffset[i] + (Time.time)));
        }
            

        int KernelHandle = m_ComputeShader.FindKernel("CSMain");
        m_ComputeShader.SetVector("TargetSize", new Vector4(m_RTSize.x, m_RTSize.y, UnityEngine.Mathf.Sin(Time.time * 10.0f), m_NumSpheres));
        
        //Result is vec4(width,height,depth,Color_Format)
        m_ComputeShader.SetTexture(KernelHandle, "Result", m_RTTarget);

        //set new spheres center in cpu buffer
        m_SimpleAccelerationStructureDataBuffer.SetData(m_SphereArray);

        //pass cpu memory buffer data to gpu buffer
        m_ComputeShader.SetBuffer(KernelHandle, "SimpleAccelerationStructureData", m_SimpleAccelerationStructureDataBuffer);
        
        m_ComputeShader.Dispatch(KernelHandle, m_RTSize.x / 8, m_RTSize.y / 8, 1);
    }
}
