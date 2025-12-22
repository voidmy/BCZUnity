using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawGrass : MonoBehaviour
{
    
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    
    void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        CreateMesh();

        SetupSingleGrassBezier();
    }

    private void CreateMesh()
    {
        var mesh = new Mesh();
        mesh.name = "Grass";
        mesh.MarkDynamic();

        // 草片尺寸（XY 平面上）
        float width = 1f;
        float height = 8f;

        // 2x2 小矩形，一共 4 个 quad
        int gridX = 1; // 水平方向分成 2 份
        int gridY = 8; // 垂直方向分成 2 份

        int vertCountX = gridX + 1; // 顶点列数
        int vertCountY = gridY + 1; // 顶点行数

        var vertices = new List<Vector3>(vertCountX * vertCountY);
        var normals = new List<Vector3>(vertCountX * vertCountY);
        var uvs = new List<Vector2>(vertCountX * vertCountY);
        var colors = new List<Color>(vertCountX * vertCountY);
        
        // 顶点分布在 XY 平面，根部在 y=0，顶端在 y=height
        for (int z = 0; z < vertCountY; z++)
        {
            float vy = (float)z / gridY * height;
            for (int x = 0; x < vertCountX; x++)
            {
                float vx = ((float)x / gridX - 0.5f) * width;

                vertices.Add(new Vector3(vx, vy, 0));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2((float)x / gridX, (float)z / gridY));
                colors.Add(Color.white);
            }
        }

        // 每个小格子两个三角形
        var triangles = new List<int>(gridX * gridY * 6);
        for (int z = 0; z < gridY; z++)
        {
            for (int x = 0; x < gridX; x++)
            {
                int i0 = z * vertCountX + x;
                int i1 = z * vertCountX + (x + 1);
                int i2 = (z + 1) * vertCountX + x;
                int i3 = (z + 1) * vertCountX + (x + 1);

                // 三角形 1
                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i1);

                // 三角形 2
                triangles.Add(i1);
                triangles.Add(i2);
                triangles.Add(i3);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);

        mesh.RecalculateBounds();

        _meshFilter.sharedMesh = mesh;
    }

    private void SetupSingleGrassBezier()
    {
        if (_meshRenderer == null)
        {
            return;
        }

        var mat = _meshRenderer.sharedMaterial;
        if (mat == null)
        {
            return;
        }

        // 这里和 CreateMesh 里的高度保持一致
        float height = 4f;

        // 草根和草尖的世界坐标（以物体的 transform 为基础）
        Vector3 rootWS = transform.position;
        Vector3 tipWS = rootWS + transform.up * height;

        // 简单设置一个弯曲方向和强度
        Vector3 windDir = transform.right.normalized;
        float bend = 0.25f;

        Vector3 p0 = rootWS;
        Vector3 p3 = tipWS + windDir * bend * height;
        Vector3 p1 = Vector3.Lerp(rootWS, tipWS, 1f / 3f) + windDir * bend * 0.3f * height;
        Vector3 p2 = Vector3.Lerp(rootWS, tipWS, 2f / 3f) + windDir * bend * 0.6f * height;

        if (mat.HasProperty("_P0_WS")) mat.SetVector("_P0_WS", p0);
        if (mat.HasProperty("_P1_WS")) mat.SetVector("_P1_WS", p1);
        if (mat.HasProperty("_P2_WS")) mat.SetVector("_P2_WS", p2);
        if (mat.HasProperty("_P3_WS")) mat.SetVector("_P3_WS", p3);
        if (mat.HasProperty("_GrassHeight")) mat.SetFloat("_GrassHeight", height);
    }

    // Update is called once per frame
    void Update()
    {
    }
}