using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

//Struct to represent the particle
[System.Serializable]
[StructLayout(LayoutKind.Sequential, Size = 44)]
public struct Particle
{
    public float pressure;
    public float density;
    public Vector3 velocity;
    public Vector3 currentForce;
    public Vector3 position;
}

public class SPH : MonoBehaviour
{
    [Header("General")]
    public bool showParticles = true; //toggle to show particle mesh
    public Vector3Int numToSpawn = new Vector3Int(10, 10, 10);

    private int _totalParticles
    {
        get
        {
            return numToSpawn.x * numToSpawn.y * numToSpawn.z;
        }
    }

    public Vector3 boundarySize = new Vector3(4, 10, 3);
    public Vector3 spawnPosition;
    public float particleRadius = .1f;

    [Header("Particle Rendering")]
    public Mesh particleMesh;
    public float particleRenderSize = 8f;
    public Material material;

    [Header("Compute Shader")]
    public ComputeShader shader;
    public Particle[] particles;

    private ComputeBuffer _argsBuffer;
    private ComputeBuffer _particleBuffer;
}
