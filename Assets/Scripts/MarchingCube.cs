using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class MarchingCube : MonoBehaviour
{
    private const int NUM_CUBE_CORNERS = 8;

    [SerializeField] private float _heightTreshold = 0.5f;
    [SerializeField] private float _heightDisparity = 0.5f;

    private List<Vector3> _particlePositions;
    private Dictionary<Vector3Int, List<int>> _gridPositions;
 
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

        foreach (Vector3Int particlePosition in _gridPositions.Keys)
        {
            float[] cubeCorners = new float[NUM_CUBE_CORNERS];

            for (int i = 0; i < NUM_CUBE_CORNERS; i++)
            {
                float density = 0.0f;
                Vector3Int corner = new Vector3Int((int)particlePosition.x, (int)particlePosition.y, (int)particlePosition.z) + MarchingTable.Corners[i];

                if (!_gridPositions.ContainsKey(corner))
                {
                    cubeCorners[i] = 0.0f;
                    continue;
                }

                foreach (int j in _gridPositions[corner])
                {
                    density += _simulator.GetParticleDensity(j);
                }

                cubeCorners[i] = density / _gridPositions[corner].Count;
            }
            foreach (int j in _gridPositions[particlePosition])
            {
                MarchCubePosition(_simulator.GetParticlePosition(j), CalculateConfigurationIndex(cubeCorners));
            }
            
            //MarchCubePosition(particlePosition, CalculateConfigurationIndex(cubeCorners));
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

    public void SetParticleHeight(/*Vector3 value*/Dictionary<Vector3Int, List<int>> grid)//Try to convert 1D arrays to 3D
    {
        //float currentHeight = value.y;
        //float distToSufrace;

        //if (value.y <= currentHeight - _heightDisparity)
        //    distToSufrace = 0f + _heightDisparity * Random.value;
        //else if (value.y > currentHeight + _heightDisparity)
        //    distToSufrace = 1f + _heightDisparity * Random.value;
        //else if (value.y > currentHeight)
        //    distToSufrace = (value.y - currentHeight + _heightDisparity) * Random.value;
        //else
        //    distToSufrace = (currentHeight - value.y + _heightDisparity) * Random.value;

        //value.y = distToSufrace;
        //_particlePositions.Add(value);


        //if (!_gridPositions.ContainsKey(cell))
        //{
        //    _gridPositions[cell] = new List<int>();
        //}
        //_gridPositions[cell].Add(index);

        _gridPositions = grid;
    }

    public void ResetParticlePositions()
    {
        _particlePositions ??= new List<Vector3>();
        _particlePositions.Clear();
    }
}
