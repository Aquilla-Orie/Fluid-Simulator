using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class MarchingCube : MonoBehaviour
{
    private const int NUM_CUBE_CORNERS = 8;

    [SerializeField] private float _heightTreshold = 0.5f;
    [SerializeField] private float _heightDisparity = 0.5f;

    private float[,,] _scalarField;

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

        Mesh backFace = InvertMesh(mesh);
        backFace.RecalculateNormals();

        Mesh combied = CombineMeshes(mesh, backFace);
        combied.RecalculateNormals();

        _meshFilter.mesh = combied;
    }

    public Mesh InvertMesh(Mesh mesh)
    {
        Vector3[] normals = mesh.normals;
        Vector3[] invertedNormals = new Vector3[normals.Length];
        for (int i = 0; i < invertedNormals.Length; i++)
        {
            invertedNormals[i] = -normals[i];
        }
        Vector4[] tangents = mesh.tangents;
        Vector4[] invertedTangents = new Vector4[tangents.Length];
        for (int i = 0; i < invertedTangents.Length; i++)
        {
            invertedTangents[i] = tangents[i];
            invertedTangents[i].w = -invertedTangents[i].w;
        }
        return new Mesh
        {
            vertices = mesh.vertices,
            uv = mesh.uv,
            normals = invertedNormals,
            tangents = invertedTangents,
            triangles = mesh.triangles.Reverse().ToArray()
        };
    }

    private Mesh CombineMeshes(Mesh mesh, Mesh invertedMesh)
    {
        CombineInstance[] combineInstancies = new CombineInstance[2]
        {
            new CombineInstance(){mesh = invertedMesh, transform = Matrix4x4.identity},
            new CombineInstance(){mesh = mesh, transform = Matrix4x4.identity}
        };
        //if (_combineOrder == CombineOrder.OriginalThenInverted)
        //{
        //    combineInstancies = combineInstancies.Reverse().ToArray();
        //}
        //combineInstancies = combineInstancies.Reverse().ToArray();
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstancies);
        return combinedMesh;
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
                MarchCubePosition(new Vector3(particlePosition.x * 3, particlePosition.y * 3, particlePosition.z * 3), CalculateConfigurationIndex(cubeCorners));
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
                Vector3 edgeEnd = position + Vector3.one * 3 + MarchingTable.Edges[triTableValue, 1] * 3;

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

    public void MarchCubes()
    {
        _vertices.Clear();
        _triangles.Clear();

        for (int x = 0; x < _scalarField.GetLength(0) - 1; x++)
        {
            for (int y = 0; y < _scalarField.GetLength(1) - 1; y++)
            {
                for (int z = 0; z < _scalarField.GetLength(2) - 1; z++)
                {
                    float[] cubeCorners = new float[8];

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
                        cubeCorners[i] = _scalarField[corner.x, corner.y, corner.z];
                    }

                    MarchCube(new Vector3(x, y, z), cubeCorners);
                }
            }
        }
    }

    private void MarchCube(Vector3 position, float[] cubeCorners)
    {
        int configIndex = CalculateConfigurationIndex(cubeCorners);

        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }

        int edgeIndex = 0;
        for (int t = 0; t < 5; t++)
        {
            for (int v = 0; v < 3; v++)
            {
                int triTableValue = MarchingTable.Triangles[configIndex, edgeIndex];

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

    public void SetScalarField(float[,,] scalar)
    {
        _scalarField = scalar;
    }

    public void ResetParticlePositions()
    {
        _particlePositions ??= new List<Vector3>();
        _particlePositions.Clear();
    }
}
