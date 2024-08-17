using UnityEngine;

public class Converstions : MonoBehaviour
{
    public bool _visualizeBoundary;
    public bool _visualizeBoundaryPosition;
    public bool _visualizeCubeInWorld;
    public bool _visualizeCubeInGrid;
    public bool _visualizeParticle;

    public float _cubeLength;
    public float _particleSize;

    public Vector3 _boundsSize;
    public Vector3 _boundsCenter;
    public Vector3 _boundsPosition;
    public Vector3 _particlePosition;
    public Vector3 _cubeWorldPosition;
    public Vector3Int _cubeGridPosition;

    private void Update()
    {
        _boundsPosition = new Vector3(
            _boundsCenter.x - (_boundsSize.x / 2),
            _boundsCenter.y - (_boundsSize.y / 2),
            _boundsCenter.z - (_boundsSize.z / 2)
            );
        _cubeGridPosition = GetCellInBoundary(_particlePosition);
        _cubeWorldPosition = GetCellInWorld(_cubeGridPosition);
    }
    Vector3Int GetCellInBoundary(Vector3 position)
    {
        float x = (position.x - _boundsPosition.x) / _cubeLength;
        float y = (position.y - _boundsPosition.y) / _cubeLength;
        float z = (position.z - _boundsPosition.z) / _cubeLength;

        int gridX = Mathf.FloorToInt(x);
        int gridY = Mathf.FloorToInt(y);
        int gridZ = Mathf.FloorToInt(z);

        return new Vector3Int(gridX, gridY, gridZ);
    }

    Vector3 GetCellInWorld(Vector3Int position)
    {
        float x = (position.x * _cubeLength) + _boundsPosition.x;
        float y = (position.y * _cubeLength) + _boundsPosition.y;
        float z = (position.z * _cubeLength) + _boundsPosition.z;

        return new Vector3(x, y, z);
    }

    private void OnDrawGizmos()
    {
        if (_visualizeBoundary)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(_boundsCenter, _boundsSize);
        }
        if (_visualizeCubeInWorld)
        {
            Gizmos.color = Color.red;
            Vector3 cubePos = new Vector3(
            _cubeWorldPosition.x + (_cubeLength / 2),
            _cubeWorldPosition.y + (_cubeLength / 2),
            _cubeWorldPosition.z + (_cubeLength / 2)
            );
            Gizmos.DrawWireCube(cubePos, Vector3.one * _cubeLength);
        }
        if (_visualizeCubeInGrid)
        {
            Gizmos.color = Color.yellow;
            Vector3 newPos = _boundsPosition + _cubeGridPosition;
            Vector3 cubePos = new Vector3(
            newPos.x + (_cubeLength / 2),
            newPos.y + (_cubeLength / 2),
            newPos.z + (_cubeLength / 2)
            );
            Gizmos.DrawWireCube(cubePos, Vector3.one * _cubeLength);
        }
        if (_visualizeCubeInGrid)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_particlePosition, _particleSize);
        }
        if (_visualizeBoundaryPosition)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_boundsPosition, 1f);
        }
    }
}
