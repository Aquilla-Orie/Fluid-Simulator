using System;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSimulator : MonoBehaviour
{
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

    // Grid properties
    private float cellSize;
    private Dictionary<Vector3Int, List<int>> grid;

    void Start()
    {
        // Create particles properties arrays
        positions = new Vector3[particleCount];
        velocities = new Vector3[particleCount];
        pressures = new float[particleCount];
        densities = new float[particleCount];
        currentForces = new Vector3[particleCount];

        // Arrange particles in 3D grid
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

        // Initialize grid
        cellSize = smoothingLength;
        grid = new Dictionary<Vector3Int, List<int>>();
    }

    void Update()
    {
        // Clear and update grid
        grid.Clear();
        for (int i = 0; i < particleCount; i++)
        {
            Vector3Int cell = GetCell(positions[i]);
            if (!grid.ContainsKey(cell))
            {
                grid[cell] = new List<int>();
            }
            grid[cell].Add(i);
        }

        UpdateDensity();
        UpdatePressure();
        ComputeForces();

        for (int i = 0; i < positions.Length; i++)
        {
            // Update velocities
            velocities[i] += currentForces[i] * Time.deltaTime;
            velocities[i] += Vector3.down * gravitationalConstant * Time.deltaTime; // Apply gravity

            // Update positions
            positions[i] += velocities[i] * Time.deltaTime;

            ResolveBoundsCollision(ref positions[i], ref velocities[i]);
        }
    }

    Vector3Int GetCell(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    private void ResolveBoundsCollision(ref Vector3 position, ref Vector3 velocity)
    {
        Vector3 halfBoundsSize = boundsSize / 2;

        if (position.x - particleSize < boundsPosition.x - halfBoundsSize.x)
        {
            position.x = boundsPosition.x - halfBoundsSize.x + particleSize;
            velocity.x *= -collisionDamping;
        }
        else if (position.x + particleSize > boundsPosition.x + halfBoundsSize.x)
        {
            position.x = boundsPosition.x + halfBoundsSize.x - particleSize;
            velocity.x *= -collisionDamping;
        }

        if (position.y - particleSize < boundsPosition.y - halfBoundsSize.y)
        {
            position.y = boundsPosition.y - halfBoundsSize.y + particleSize;
            velocity.y *= -collisionDamping;
        }
        else if (position.y + particleSize > boundsPosition.y + halfBoundsSize.y)
        {
            position.y = boundsPosition.y + halfBoundsSize.y - particleSize;
            velocity.y *= -collisionDamping;
        }

        if (position.z - particleSize < boundsPosition.z - halfBoundsSize.z)
        {
            position.z = boundsPosition.z - halfBoundsSize.z + particleSize;
            velocity.z *= -collisionDamping;
        }
        else if (position.z + particleSize > boundsPosition.z + halfBoundsSize.z)
        {
            position.z = boundsPosition.z + halfBoundsSize.z - particleSize;
            velocity.z *= -collisionDamping;
        }
    }


    private void UpdateDensity()
    {
        for (int i = 0; i < particleCount; i++)
        {
            float density = 0.0f;
            Vector3Int cell = GetCell(positions[i]);

            // Check current cell and neighboring cells
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3Int neighborCell = cell + new Vector3Int(x, y, z);
                        if (grid.ContainsKey(neighborCell))
                        {
                            foreach (int j in grid[neighborCell])
                            {
                                float distance = (positions[i] - positions[j]).magnitude;
                                if (distance < smoothingLength)
                                {
                                    density += molarMass * SmoothingKernel(smoothingLength, distance);
                                }
                            }
                        }
                    }
                }
            }
            densities[i] = density;
        }
    }

    private void UpdatePressure()
    {
        for (int i = 0; i < particleCount; i++)
        {
            pressures[i] = PressureEquation(densities[i]);
        }
    }

    private float PressureEquation(float density)
    {
        return stiffnessConstant * (density - restingDensity); //P = k(p - p0)
    }

    private void ComputeForces()
    {
        for (int i = 0; i < particleCount; i++)
        {
            Vector3 force = Vector3.zero;
            Vector3Int cell = GetCell(positions[i]);

            // Check current cell and neighboring cells
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3Int neighborCell = cell + new Vector3Int(x, y, z);
                        if (grid.ContainsKey(neighborCell))
                        {
                            foreach (int j in grid[neighborCell])
                            {
                                if (i != j)
                                {
                                    float distance = (positions[i] - positions[j]).magnitude;
                                    if (distance < smoothingLength)
                                    {
                                        Vector3 direction = (positions[j] - positions[i]).normalized;

                                        // Pressure force
                                        float pressureForce = -(pressures[i] + pressures[j]) / (2.0f * densities[j]) * PressureKernelGradient(smoothingLength, distance);
                                        force += direction * pressureForce;

                                        // Viscosity force
                                        force += ComputeViscosityForce(i, j, distance);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            currentForces[i] = force/* + Vector3.down * gravitationalConstant * densities[i]*/; // Apply gravity
        }
    }

    Vector3 ComputeViscosityForce(int i, int j, float distance)
    {
        Vector3 velocityDiff = velocities[j] - velocities[i];
        float laplacian = ViscosityKernelLaplacian(smoothingLength, distance);
        //return viscosityCoefficient * velocityDiff * laplacian / densities[j];
        return viscosityCoefficient * (velocityDiff / densities[j]) * laplacian;
    }

    private float SmoothingKernel(float smoothingLength, float distance)// Poly6 Kernel
    {
        float q = distance / smoothingLength;
        float sigma = 315f / (64f * Mathf.PI * Mathf.Pow(smoothingLength, 9));

        if (q >= 0 && q <= 1)
        {
            return sigma * Mathf.Pow((smoothingLength * smoothingLength - distance * distance), 3);
        }
        return 0;
    }


    private float PressureKernelGradient(float smoothingLength, float distance)// Spiky Kernel Gradient
    {
        float q = distance / smoothingLength;
        if (q >= 0 && q <= 1)
        {
            return -45.0f / (Mathf.PI * Mathf.Pow(smoothingLength, 6)) * Mathf.Pow(1 - q, 2);
        }
        return 0;
    }

    private float ViscosityKernelLaplacian(float smoothingLength, float distance)
    {
        float q = distance / smoothingLength;
        if (q >= 0 && q <= 1)
        {
            return 45.0f / (Mathf.PI * Mathf.Pow(smoothingLength, 6)) * (1 - q);
        }
        return 0;
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
