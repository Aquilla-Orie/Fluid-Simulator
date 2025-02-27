﻿using System;
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

    // Grid properties
    private float cellSize;
    private Dictionary<Vector3Int, List<int>> grid;
    public Dictionary<Vector3Int, List<int>> gridPositions;

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
        marchingCube.ResetParticlePositions();

        cellSize = smoothingLength;

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
            //marchingCube.SetParticleHeight(new Vector3(cell.x * cellSize, cell.y * cellSize, cell.z * cellSize));
            marchingCube.SetParticleHeight(grid);
        }

        float[,,] field = GenerateScalarField(positions, new Vector3Int((int)(boundsSize.x / cellSize), (int)(boundsSize.y / cellSize), (int)(boundsSize.z / cellSize)), 0, smoothingLength);
        marchingCube.SetScalarField(field);
        marchingCube.MarchCubesPosition();
        marchingCube.SetMesh();
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

            if (box != null)
            {
                boxMin = box.bounds.min;
                boxMax = box.bounds.max;

                ResolveBoxCollision(ref positions[i], ref velocities[i]);
            }
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

    public Vector3 GetBoundsPosition()
    {
        return new Vector3(
            boundsPosition.x - (boundsSize.x / 2),
            boundsPosition.y - (boundsSize.y / 2),
            boundsPosition.z - (boundsSize.z / 2)
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

    private void ResolveBoxCollision(ref Vector3 position, ref Vector3 velocity)
    {
        if (Vector3.Distance(position, box.transform.position) <= box.transform.localScale.magnitude)
        {
            // Check each axis and correct the particle's position and velocity
            if (position.x >= boxMin.x + particleSize && position.x < box.transform.position.x)
            {
                position.x = boxMin.x - particleSize ;//- boundaryOffset;  // Move particle just outside the box
                if (velocity.x < 0) velocity.x *= -collisionDamping;  // Reverse velocity if moving toward the box
            }
            else if (position.x <= boxMax.x - particleSize && position.x > box.transform.position.x)
            {
                position.x = boxMax.x + particleSize ;//+ boundaryOffset;  // Move particle just outside the box
                if (velocity.x > 0) velocity.x *= -collisionDamping;  // Reverse velocity if moving toward the box
            }

            if (position.y >= boxMin.y + particleSize && position.y < box.transform.position.y)
            {
                position.y = boxMin.y - particleSize ;//- boundaryOffset;
                if (velocity.y < 0) velocity.y *= -collisionDamping;
            }
            else if (position.y <= boxMax.y - particleSize && position.y > box.transform.position.y)
            {
                position.y = boxMax.y + particleSize ;//+ boundaryOffset;
                if (velocity.y > 0) velocity.y *= -collisionDamping;
            }

            if (position.z >= boxMin.z + particleSize && position.z < box.transform.position.z)
            {
                position.z = boxMin.z - particleSize ;//- boundaryOffset;
                if (velocity.z < 0) velocity.z *= -collisionDamping;
            }
            else if (position.z <= boxMax.z - particleSize && position.z > box.transform.position.z)
            {
                position.z = boxMax.z + particleSize ;//+ boundaryOffset;
                if (velocity.z > 0) velocity.z *= -collisionDamping;
            }
        }
        
    }

    private bool Intersects(Vector3 pointA, Vector3 pointBMin, Vector3 pointBMax)
    {
        return (
            pointA.x <= pointBMax.x &&
            pointA.x >= pointBMin.x &&
            pointA.y <= pointBMax.y &&
            pointA.y >= pointBMin.y &&
            pointA.z <= pointBMax.z &&
            pointA.z >= pointBMin.z
            );
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
                if(visualizeColorOnVelocity)
                    Gizmos.color = new Color(velocities[i].x, velocities[i].y, velocities[i].z, 1);
                Gizmos.DrawSphere(positions[i], particleSize);
            }
        }

        if (visualizeGrid)
        {
            if (grid != null && grid.Count > 0)
            {
                foreach (Vector3Int position in grid.Keys)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(new Vector3(position.x * cellSize, position.y * cellSize, position.z * cellSize), new Vector3(cellSize, cellSize, cellSize));
                }
            }
        }
       

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(boundsPosition, boundsSize);
    }

    public Vector3 GetParticlePosition(int index)
    {
        return positions[index];
    }

    public float GetParticleDensity(int index)
    {
        return densities[index];
    }

    public Vector3Int GetCellInBoundary(Vector3 position)
    {
        Vector3 _boundsPosition = new Vector3(
            boundsPosition.x - (boundsSize.x / 2),
            boundsPosition.y - (boundsSize.y / 2),
            boundsPosition.z - (boundsSize.z / 2)
            );
        float x = (position.x - _boundsPosition.x) / cellSize;
        float y = (position.y - _boundsPosition.y) / cellSize;
        float z = (position.z - _boundsPosition.z) / cellSize;

        int gridX = Mathf.FloorToInt(x);
        int gridY = Mathf.FloorToInt(y);
        int gridZ = Mathf.FloorToInt(z);

        return new Vector3Int(gridX, gridY, gridZ);
    }

    public Vector3 GetCellInWorld(Vector3Int position)
    {
        Vector3 _boundsPosition = new Vector3(
            boundsPosition.x - (boundsSize.x / 2),
            boundsPosition.y - (boundsSize.y / 2),
            boundsPosition.z - (boundsSize.z / 2)
            );

        float x = (position.x * cellSize) + _boundsPosition.x;
        float y = (position.y * cellSize) + _boundsPosition.y;
        float z = (position.z * cellSize) + _boundsPosition.z;

        return new Vector3(x, y, z);
    }

    private float[,,] GenerateScalarField(Vector3[] positions, Vector3Int gridResolution, float gridSize, float particleRadius)
    {
        float[,,] scalarField = new float[gridResolution.x + 1, gridResolution.y + 1, gridResolution.z + 1];
        float halfGridSize = gridSize / 2;

        foreach (var position in positions)
        {
            Vector3Int gridPos = GetCellInBoundary(position);
            int x = Mathf.Clamp((int)gridPos.x, 0, gridResolution.x - 1);
            int y = Mathf.Clamp((int)gridPos.y, 0, gridResolution.y - 1);
            int z = Mathf.Clamp((int)gridPos.z, 0, gridResolution.z - 1);

            // Apply a kernel function to spread density contributions
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    for (int k = -1; k <= 1; k++)
                    {
                        int nx = gridPos.x + i;
                        int ny = gridPos.y + j;
                        int nz = gridPos.z + k;

                        if (nx >= 0 && nx < gridResolution.x && ny >= 0 && ny < gridResolution.y && nz >= 0 && nz < gridResolution.z)
                        {
                            float distance = Vector3.Distance(position, new Vector3(nx, ny, nz)) /* ((gridResolution.magnitude - 1) * gridSize - halfGridSize))*/;
                            scalarField[nx, ny, nz] += Mathf.Exp(-distance * distance / (2 * particleRadius * particleRadius));
                        }
                    }
                }
            }
        }
        return scalarField;
    }
}
