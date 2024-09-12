using System;
using UnityEngine;

public class GPUParticleSimulator : MonoBehaviour
{
    public const int NUM_OF_THREADS = 1024;
    [Header("World Values")]
    public float gravitationalConstant = 9.8f;
    public Vector3 center;

    [Header("Particle Properties")]
    public int particleCount;
    public float[] pressures;
    public Vector3Int[] spatialIndices;
    public int[] spatialOffsets;
    public float[] densities;
    public Vector3[] velocities;
    public Vector3[] currentForces;
    public Vector3[] positions;
    public bool visualizeParticles;
    public bool visualizeColorOnVelocity;
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

    ComputeBuffer spatialIndicesBuffer;
    ComputeBuffer spatialOffsetsBuffer;

    [Header("External Collision")]
    public BoxCollider box;
    // Variables for the box (AABB)
    public Vector3 boxMin;
    public Vector3 boxMax;
    public float boundaryOffset;

    public void StartSim()
    {
        // Create particles properties arrays
        positions = new Vector3[particleCount];
        velocities = new Vector3[particleCount];
        pressures = new float[particleCount];
        densities = new float[particleCount];
        currentForces = new Vector3[particleCount];
        spatialIndices = new Vector3Int[particleCount];
        spatialOffsets = new int[particleCount];

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

        spatialIndicesBuffer = new ComputeBuffer(particleCount, sizeof(int) * 3);
        spatialOffsetsBuffer = new ComputeBuffer(particleCount, sizeof(int));

        // Set initial data
        positionsBuffer.SetData(positions);
        velocitiesBuffer.SetData(velocities);
        pressuresBuffer.SetData(pressures);
        densitiesBuffer.SetData(densities);
        forcesBuffer.SetData(currentForces);
        spatialIndicesBuffer.SetData(spatialIndices);
        spatialOffsetsBuffer.SetData(spatialOffsets);

        computeShader.SetFloats("down", new float[] { 0, -1, 0 });


        // Set buffers in compute shader
        for (int k = 0; k < kernelCount; k++)
        {
            computeShader.SetBuffer(k, "positions", positionsBuffer);
            computeShader.SetBuffer(k, "velocities", velocitiesBuffer);
            computeShader.SetBuffer(k, "pressures", pressuresBuffer);
            computeShader.SetBuffer(k, "densities", densitiesBuffer);
            computeShader.SetBuffer(k, "forces", forcesBuffer);
            computeShader.SetBuffer(k, "spatialIndices", spatialIndicesBuffer);
            computeShader.SetBuffer(k, "spatialOffsets", spatialOffsetsBuffer);
            computeShader.SetBuffer(k, "Entries", spatialIndicesBuffer);
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

        //Collision box
        if (box != null)
        {
            boxMin = box.bounds.min;
            boxMax = box.bounds.max;

            computeShader.SetFloats("boxPos", new float[] { box.transform.position.x, box.transform.position.y, box.transform.position.z });
            computeShader.SetFloats("boxMin", new float[] { boxMin.x, boxMin.y, boxMin.z });
            computeShader.SetFloats("boxMax", new float[] { boxMax.x, boxMax.y, boxMax.z });
            computeShader.SetFloat("boxScale", box.transform.localScale.magnitude);

        }

        computeShader.SetBool("computeBox", box != null);

        computeShader.Dispatch(computeShader.FindKernel("UpdateSpatialHash"),/* particleCount /*/ NUM_OF_THREADS, 1, 1);
        SortAndCalculateOffsets();

        computeShader.Dispatch(computeShader.FindKernel("UpdateDensity"),/* particleCount /*/ NUM_OF_THREADS, 1, 1);
        computeShader.Dispatch(computeShader.FindKernel("UpdatePressure"),/* particleCount /*/ NUM_OF_THREADS, 1, 1);
        computeShader.Dispatch(computeShader.FindKernel("ComputeForces"), /*particleCount /*/ NUM_OF_THREADS, 1, 1);
        computeShader.Dispatch(computeShader.FindKernel("MoveParticles"), /*particleCount /*/ NUM_OF_THREADS, 1, 1);

       

        positionsBuffer.GetData(positions);
        pressuresBuffer.GetData(pressures);
        velocitiesBuffer.GetData(velocities);
        forcesBuffer.GetData(currentForces);
        densitiesBuffer.GetData(densities);
        spatialIndicesBuffer.GetData(spatialIndices);
        spatialOffsetsBuffer.GetData(spatialOffsets);
    }

    public void Sort()
    {
        computeShader.SetInt("numEntries", spatialIndicesBuffer.count);

        // Launch each step of the sorting algorithm (once the previous step is complete)
        // Number of steps = [log2(n) * (log2(n) + 1)] / 2
        // where n = nearest power of 2 that is greater or equal to the number of inputs
        int numStages = (int)Mathf.Log(Mathf.NextPowerOfTwo(spatialIndicesBuffer.count), 2);

        for (int stageIndex = 0; stageIndex < numStages; stageIndex++)
        {
            for (int stepIndex = 0; stepIndex < stageIndex + 1; stepIndex++)
            {
                // Calculate some pattern stuff
                int groupWidth = 1 << (stageIndex - stepIndex);
                int groupHeight = 2 * groupWidth - 1;
                computeShader.SetInt("groupWidth", groupWidth);
                computeShader.SetInt("groupHeight", groupHeight);
                computeShader.SetInt("stepIndex", stepIndex);
                // Run the sorting step on the GPU
                uint x, y, z;
                computeShader.GetKernelThreadGroupSizes(computeShader.FindKernel("Sort"), out x, out y, out z);
                int numGroupsX = Mathf.CeilToInt((Mathf.NextPowerOfTwo(spatialIndicesBuffer.count) / 2) / (float)x);
                computeShader.Dispatch(computeShader.FindKernel("Sort"), numGroupsX, 1, 1);
            }
        }
    }


    public void SortAndCalculateOffsets()
    {
        Sort();

        computeShader.Dispatch(computeShader.FindKernel("CalculateOffsets"), NUM_OF_THREADS, 1, 1);
    }

    private void OnDrawGizmos()
    {
        if (visualizeParticles)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                Gizmos.color = Color.yellow;
                if (visualizeColorOnVelocity)
                    Gizmos.color = new Color(velocities[i].x, velocities[i].y, velocities[i].z, 1);
                Gizmos.DrawSphere(positions[i], particleSize);
            }
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(boundsPosition, boundsSize);
    }
}
