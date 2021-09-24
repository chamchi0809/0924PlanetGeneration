using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] //인스펙터창에 표시됨
public class NoiseLayer
{
    Noise noise = new Noise();
    public bool enabled;    
    public int octaveCount;
    public float roughness;
    public float strength;
    public float lacunarity;
    public float persistence;
    public float seaLevel;
    public Vector3 offset;
    
    public float Evaluate(Vector3 point)
    {
        float noiseValue = 0;
        float r = roughness;
        float s = strength;
        for(int i = 0; i < octaveCount; i++)
        {
            noiseValue += (noise.Evaluate(point * r + offset) + 1) * .5f * s;
            r *= lacunarity;
            s *= persistence;
        }
        return Mathf.Max(0, noiseValue - seaLevel); //둘중에 큰값을 반환
    }
}

public class PlanetGenerator : MonoBehaviour
{
    public NoiseLayer[] layers;
    public MeshFilter[] meshFilters;
    public int resolution; //그리드 칸 개수 : r * r

    private void OnValidate() //인스펙터창에서 뭐 수정할때마다 실힝됨
    {
        meshFilters = new MeshFilter[6];
        meshFilters[0] = GenerateOneFace(Vector3.forward, 0);
        meshFilters[1] = GenerateOneFace(Vector3.back, 1);
        meshFilters[2] = GenerateOneFace(Vector3.up, 2);
        meshFilters[3] = GenerateOneFace(Vector3.down, 3);
        meshFilters[4] = GenerateOneFace(Vector3.right, 4);
        meshFilters[5] = GenerateOneFace(Vector3.left, 5);
    }

    MeshFilter GenerateOneFace(Vector3 direction /*면이 바라보는 방향*/,int idx /*몇번째 생성하는 면인지 정의*/)
    {
        Vector3 axisA = new Vector3(direction.y, direction.z, direction.x);
        Vector3 axisB = Vector3.Cross(direction, axisA);

        GameObject g;
        MeshFilter mf;
        MeshRenderer mr;

        if(transform.childCount < idx + 1)
        {
            g = new GameObject("g");
            g.transform.parent = this.transform;
            mf = g.AddComponent<MeshFilter>();
            mr = g.AddComponent<MeshRenderer>();
        }
        else
        {
            g = transform.GetChild(idx).gameObject;
            mf = g.GetComponent<MeshFilter>();
            mr = g.GetComponent<MeshRenderer>();
        }

        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
        int[] triangles = new int[6 * resolution * resolution];

        for(int y = 0,i=0,triIdx=0; y <= resolution; y++)
        {
            for(int x = 0; x <= resolution; x++)
            {
                Vector2 percent = new Vector2(x, y) / resolution;
                Vector3 pointOnCube = direction + axisA * (percent.x - .5f) * 2 + axisB * (percent.y - .5f) * 2;
                Vector3 pointOnSphere = pointOnCube.normalized; //모든 벡터길이 1로 고정

                float elevation = 0;

                for(int j = 0; j < layers.Length; j++)
                {
                    if (!layers[j].enabled)
                        continue;
                    elevation += layers[j].Evaluate(pointOnSphere);                    
                }

                vertices[i] = pointOnSphere * (elevation + 1);
                if(x != resolution && y != resolution)
                {
                    triangles[triIdx + 0] = i;
                    triangles[triIdx + 1] = i + resolution + 2;
                    triangles[triIdx + 2] = i + resolution + 1;
                    triangles[triIdx + 3] = i;
                    triangles[triIdx + 4] = i + 1;
                    triangles[triIdx + 5] = i + resolution + 2;

                    triIdx += 6;
                }
                i++;
            }            
        }

        Mesh mesh = mf.sharedMesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mr.material = new Material(Shader.Find("Standard"));
        mesh.RecalculateNormals(); //빛 초기화
        mesh.RecalculateBounds(); //메쉬 부드럽게
        return mf;
    }
}
