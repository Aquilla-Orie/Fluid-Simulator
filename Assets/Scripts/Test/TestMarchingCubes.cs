using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMarchingCubes : MonoBehaviour
{
    //public static Mesh GenerateMesh(float[,,] scalarField, float isoLevel)
    //{
    //    List<Vector3> vertices = new List<Vector3>();
    //    List<int> triangles = new List<int>();

    //    int gridResolution = scalarField.GetLength(0);
    //    float gridSize = 1.0f / gridResolution;

    //    for (int x = 0; x < gridResolution - 1; x++)
    //    {
    //        for (int y = 0; y < gridResolution - 1; y++)
    //        {
    //            for (int z = 0; z < gridResolution - 1; z++)
    //            {
    //                float[] cube = new float[8];
    //                for (int i = 0; i < 8; i++)
    //                {
    //                    int nx = x + vertexOffset[i, 0];
    //                    int ny = y + vertexOffset[i, 1];
    //                    int nz = z + vertexOffset[i, 2];
    //                    cube[i] = scalarField[nx, ny, nz];
    //                }

    //                int cubeIndex = 0;
    //                if (cube[0] < isoLevel) cubeIndex |= 1;
    //                if (cube[1] < isoLevel) cubeIndex |= 2;
    //                if (cube[2] < isoLevel) cubeIndex |= 4;
    //                if (cube[3] < isoLevel) cubeIndex |= 8;
    //                if (cube[4] < isoLevel) cubeIndex |= 16;
    //                if (cube[5] < isoLevel) cubeIndex |= 32;
    //                if (cube[6] < isoLevel) cubeIndex |= 64;
    //                if (cube[7] < isoLevel) cubeIndex |= 128;

    //                if (MeedgeTable[cubeIndex] == 0) continue;

    //                Vector3[] vertList = new Vector3[12];
    //                if ((edgeTable[cubeIndex] & 1) != 0)
    //                    vertList[0] = VertexInterp(isoLevel, cube[0], cube[1], vertexPositions[0], vertexPositions[1]);
    //                if ((edgeTable[cubeIndex] & 2) != 0)
    //                    vertList[1] = VertexInterp(isoLevel, cube[1], cube[2], vertexPositions[1], vertexPositions[2]);
    //                if ((edgeTable[cubeIndex] & 4) != 0)
    //                    vertList[2] = VertexInterp(isoLevel, cube[2], cube[3], vertexPositions[2], vertexPositions[3]);
    //                if ((edgeTable[cubeIndex] & 8) != 0)
    //                    vertList[3] = VertexInterp(isoLevel, cube[3], cube[0], vertexPositions[3], vertexPositions[0]);
    //                if ((edgeTable[cubeIndex] & 16) != 0)
    //                    vertList[4] = VertexInterp(isoLevel, cube[4], cube[5], vertexPositions[4], vertexPositions[5]);
    //                if ((edgeTable[cubeIndex] & 32) != 0)
    //                    vertList[5] = VertexInterp(isoLevel, cube[5], cube[6], vertexPositions[5], vertexPositions[6]);
    //                if ((edgeTable[cubeIndex] & 64) != 0)
    //                    vertList[6] = VertexInterp(isoLevel, cube[6], cube[7], vertexPositions[6], vertexPositions[7]);
    //                if ((edgeTable[cubeIndex] & 128) != 0)
    //                    vertList[7] = VertexInterp(isoLevel, cube[7], cube[4], vertexPositions[7], vertexPositions[4]);
    //                if ((edgeTable[cubeIndex] & 256) != 0)
    //                    vertList[8] = VertexInterp(isoLevel, cube[0], cube[4], vertexPositions[0], vertexPositions[4]);
    //                if ((edgeTable[cubeIndex] & 512) != 0)
    //                    vertList[9] = VertexInterp(isoLevel, cube[1], cube[5], vertexPositions[1], vertexPositions[5]);
    //                if ((edgeTable[cubeIndex] & 1024) != 0)
    //                    vertList[10] = VertexInterp(isoLevel, cube[2], cube[6], vertexPositions[2], vertexPositions[6]);
    //                if ((edgeTable[cubeIndex] & 2048) != 0)
    //                    vertList[11] = VertexInterp(isoLevel, cube[3], cube[7], vertexPositions[3], vertexPositions[7]);

    //                for (int i = 0; triTable[cubeIndex, i] != -1; i += 3)
    //                {
    //                    triangles.Add(vertices.Count);
    //                    vertices.Add(vertList[triTable[cubeIndex, i]]);
    //                    triangles.Add(vertices.Count);
    //                    vertices.Add(vertList[triTable[cubeIndex, i + 1]]);
    //                    triangles.Add(vertices.Count);
    //                    vertices.Add(vertList[triTable[cubeIndex, i + 2]]);
    //                }
    //            }
    //        }
    //    }

    //    Mesh mesh = new Mesh();
    //    mesh.vertices = vertices.ToArray();
    //    mesh.triangles = triangles.ToArray();
    //    mesh.RecalculateNormals();

    //    return mesh;
    //}

    //private static Vector3 VertexInterp(float isoLevel, float val1, float val2, Vector3 p1, Vector3 p2)
    //{
    //    if (Mathf.Abs(isoLevel - val1) < 0.00001)
    //        return p1;
    //    if (Mathf.Abs(isoLevel - val2) < 0.00001)
    //        return p2;
    //    if (Mathf.Abs(val1 - val2) < 0.00001)
    //        return p1;

    //    float mu = (isoLevel - val1) / (val2 - val1);
    //    return p1 + mu * (p2 - p1);
    //}

    //private static readonly Vector3[] vertexPositions = new Vector3[8]
    //{
    //    new Vector3(0, 0, 0),
    //    new Vector3(1, 0, 0),
    //    new Vector3(1, 1, 0),
    //    new Vector3(0, 1, 0),
    //    new Vector3(0, 0, 1),
    //    new Vector3(1, 0, 1),
    //    new Vector3(1, 1, 1),
    //    new Vector3(0, 1, 1)
    //};

    //private static readonly int[,] vertexOffset = new int[8, 3]
    //{
    //    { 0, 0, 0 },
    //    { 1, 0, 0 },
    //    { 1, 1, 0 },
    //    { 0, 1, 0 },
    //    { 0, 0, 1 },
    //    { 1, 0, 1 },
    //    { 1, 1, 1 },
    //    { 0, 1, 1 }
    //};
}
