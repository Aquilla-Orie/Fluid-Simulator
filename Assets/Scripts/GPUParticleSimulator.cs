using System;
using UnityEngine;

public class GPUParticleSimulator : MonoBehaviour
{
    public const int NUM_OF_THREADS = 16;
    [Header("World Values")]
    public float gravitationalConstant = 9.8f;
    public Vector3 center;

    [Header("Particle Properties")]
    public int particleCount;
    public float[] pressures;
    public float[] densities;
    public Vector3[] velocities;
    public Vector3[] currentForces;
    public Vector3[] positions;
    public bool visualizeParticles;
    public bool visualizeGrid;
    public float particleSize;
    public float dispersionAmount;
    public Vector3 startVelocity;
    public float smoothingLength;
    public float restingDensity; // Ideal density for the fluid
    public float stiffnessConstant;
    public float viscosityCoefficient;

    public float volume = 0.000000299f; // Volume of water molecule 2.99x10^-23
    public float molarMass = 0.018f; // Molar mass of water 18g -> 0.018kg

    [Header("Bounds")]
    public Vector3 boundsSize;
    public Vector3 boundsPosition;
    public float collisionDamping = 1;

    [Header("Graphics")]
    [SerializeField] private MarchingCube marchingCube;

    [Header("Compute Shader")]
    public int kernelCount;
    public ComputeShader computeShader;

    ComputeBuffer positionsBuffer;
    ComputeBuffer velocitiesBuffer;
    ComputeBuffer pressuresBuffer;
    ComputeBuffer densitiesBuffer;
    ComputeBuffer forcesBuffer;

    void Start()
    {
        // Create particles properties arrays
        positions = new Vector3[particleCount];
        velocities = new Vector3[particleCount];
        pressures = new float[particleCount];
        densities = new float[particleCount];
        currentForces = new Vector3[particleCount];

        int particlesPerAxis = (int)Math.Cbrt(particleCount);
        int i = 0;
        for (int x = 0; x < particlesPerAxis; x++)
        {
            for (int y = 0; y < particlesPerAxis; y++)
            {
                for (int z = 0; z < particlesPerAxis; z++)
                {
                    float tx = x / (particlesPerAxis - 1f);
                    float ty = y / (particlesPerAxis - 1f);
                    float tz = z / (particlesPerAxis - 1f);

                    float px = (tx - 0.5f) * particleSize + center.x;
                    float py = (ty - 0.5f) * particleSize + center.y;
                    float pz = (tz - 0.5f) * particleSize + center.z;
                    Vector3 jitter = UnityEngine.Random.insideUnitSphere * dispersionAmount;
                    positions[i] = new Vector3(px, py, pz) + jitter;
                    velocities[i] = startVelocity;
                    i++;
                }
            }
        }
        

        // Initialize compute buffers
        positionsBuffer = new ComputeBuffer(particleCount, sizeof(float) * 3);
        velocitiesBuffer = new ComputeBuffer(particleCount, sizeof(float) * 3);
        pressuresBuffer = new ComputeBuffer(particleCount, sizeof(float));
        densitiesBuffer = new ComputeBuffer(particleCount, sizeof(float));
        forcesBuffer = new ComputeBuffer(particleCount, sizeof(float) * 3);

        // Set initial data
        positionsBuffer.SetData(positions);
        velocitiesBuffer.SetData(velocities);
        pressuresBuffer.SetData(pressures);
        densitiesBuffer.SetData(densities);
        forcesBuffer.SetData(currentForces);

        computeShader.SetFloats("down", new float[] { 0, -1, 0 });


        // Set buffers in compute shader
        for (int k = 0; k < kernelCount; k++)
        {
            computeShader.SetBuffer(k, "positions", positionsBuffer);
            computeShader.SetBuffer(k, "velocities", velocitiesBuffer);
            computeShader.SetBuffer(k, "pressures", pressuresBuffer);
            computeShader.SetBuffer(k, "densities", densitiesBuffer);
            computeShader.SetBuffer(k, "forces", forcesBuffer);
        }
        
    }

    void Update()
    {
        computeShader.SetInt("numOfParticles", particleCount);
        computeShader.SetFloat("smoothingLength", smoothingLength);
        computeShader.SetFloat("molarMass", molarMass);
        computeShader.SetFloat("particleSize", particleSize);
        computeShader.SetFloat("gravitationalConstant", gravitationalConstant);
        computeShader.SetFloat("collisionDamping", collisionDamping);
        computeShader.SetFloat("stiffnessConstant", stiffnessConstant);
        computeShader.SetFloat("restingDensity", restingDensity);
        computeShader.SetFloat("viscosityCoefficient", viscosityCoefficient);
        computeShader.SetFloats("boundsPosition", new float[] { boundsPosition.x, boundsPosition.y, boundsPosition.z });
        computeShader.SetFloats("boundsSize", new float[] { boundsSize.x, boundsSize.y, boundsSize.z });
        computeShader.SetFloat("deltaTime", Time.deltaTime);

        //computeShader.Dispatch(computeShader.FindKernel("HashParticles"), particleCount / NUM_OF_THREADS, 1, 1);
        //SortParticles();
        //computeShader.Dispatch(computeShader.FindKernel("CalculateCellOffsets"), particleCount / NUM_OF_THREADS, 1, 1);

        // Dispatch the compute shader
        computeShader.Dispatch(computeShader.FindKernel("UpdateDensity"), particleCount/NUM_OF_THREADS, 1, 1);
        computeShader.Dispatch(computeShader.FindKernel("UpdatePressure"), particleCount / NUM_OF_THREADS, 1, 1);
        computeShader.Dispatch(computeShader.FindKernel("ComputeForces"), particleCount/NUM_OF_THREADS, 1, 1);
        computeShader.Dispatch(computeShader.FindKernel("MoveParticles"), particleCount/NUM_OF_THREADS, 1, 1);

        // Optionally, read back data from GPU
        positionsBuffer.GetData(positions);
        velocitiesBuffer.GetData(velocities);
        forcesBuffer.GetData(currentForces);
        densitiesBuffer.GetData(densities);
    }

    private void OnDrawGizmos()
    {
        if (visualizeParticles)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(positions[i], particleSize);
            }
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(boundsPosition, boundsSize);
    }
}
