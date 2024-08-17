using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class MarchingCube : MonoBehaviour
{
    private const int NUM_CUBE_CORNERS = 8;

    [SerializeField] private float _heightTreshold = 0.5f;

    private List<Vector3> _particlePositions;
 
    private MeshFilter _meshFilter;

    private List<Vector3> _vertices = new List<Vector3>();
    private List<int> _triangles = new List<int>();

    [SerializeField] ParticleSimulator _simulator;

    private void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
    }

    public void SetMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = _vertices.ToArray();
        mesh.triangles = _triangles.ToArray();
        mesh.RecalculateNormals();

        _meshFilter.mesh = mesh;
    }

    public void MarchCubesPosition()
    {
        _triangles.Clear();
        _vertices.Clear();

        foreach (Vector3 particlePosition in _particlePositions)
        {
            float[] cubeCorners = new float[NUM_CUBE_CORNERS];

            for (int i = 0; i < NUM_CUBE_CORNERS; i++)
            {
                //Vector3Int corner = new Vector3Int((int)particlePosition.x, (int)particlePosition.y, (int)particlePosition.z) + MarchingTable.Corners[i];

                cubeCorners[i] = Random.value;
            }

            MarchCubePosition(particlePosition, CalculateConfigurationIndex(cubeCorners));
        }
    }

    private void MarchCubePosition(Vector3 position, int configurationIndex)
    {

        if (configurationIndex == 0 || configurationIndex == 255)
        {
            return;
        }

        int edgeIndex = 0;

        for (int t = 0; t < 5; t++)
        {
            for (int v = 0; v < 3; v++)
            {
                int triTableValue = MarchingTable.Triangles[configurationIndex, edgeIndex];

                if (triTableValue == -1)
                {
                    return;
                }

                Vector3 edgeStart = position + MarchingTable.Edges[triTableValue, 0];
                Vector3 edgeEnd = position + MarchingTable.Edges[triTableValue, 1];

                Vector3 vertex = (edgeStart + edgeEnd) / 2;

                _vertices.Add(vertex);
                _triangles.Add(_vertices.Count - 1);


                edgeIndex++;
            }
        }
    }

    private int CalculateConfigurationIndex(float[] cubeCorners)
    {
        int index = 0;

        for (int i = 0; i < NUM_CUBE_CORNERS; i++)
        {
            if (cubeCorners[i] > _heightTreshold)
            {
                index |= 1 << i;
            }
        }

        return index;
    }

    public void SetParticleHeight(Vector3 value)//Try to convert 1D arrays to 3D
    {
        _particlePositions.Add(value);
    }

    public void ResetParticlePositions()
    {
        _particlePositions ??= new List<Vector3>();
        _particlePositions.Clear();
    }
}
