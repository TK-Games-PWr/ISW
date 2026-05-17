using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavGridSystem : MonoBehaviour
{
    public static NavGridSystem Instance { get; private set; }

    [Header("Generation Settings")] [Tooltip("Distance between generated points on edges.")]
    public float pointSpacing = 2.0f;

    [Header("Grid Settings")] public float cellSize = 10f;
    public float cellHeight = 4f;

    [Header("Debug")] public bool drawGizmos = true;

    Dictionary<Vector3Int, List<Vector3>> grid = new();

    List<Vector3> debugPoints = new();

    struct Edge : System.IEquatable<Edge>
    {
        public int v1, v2;

        public Edge(int vertex1, int vertex2)
        {
            v1 = Mathf.Min(vertex1, vertex2);
            v2 = Mathf.Max(vertex1, vertex2);
        }

        public bool Equals(Edge other) => v1 == other.v1 && v2 == other.v2;
        public override int GetHashCode() => (v1 * 397) ^ v2;
    }

    void Awake()
    {
        Instance = this;
        GenerateGridFromNavMesh();
    }

    public void GenerateGridFromNavMesh()
    {
        grid.Clear();
        debugPoints.Clear();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        int[] indices = triangulation.indices;
        Vector3[] vertices = triangulation.vertices;

        Dictionary<Edge, int> edgeUsage = new Dictionary<Edge, int>();
        for (int i = 0; i < indices.Length; i += 3)
        {
            AddEdge(edgeUsage, indices[i], indices[i + 1]);
            AddEdge(edgeUsage, indices[i + 1], indices[i + 2]);
            AddEdge(edgeUsage, indices[i + 2], indices[i]);
        }

        // Generate candidate points
        List<Vector3> candidatePoints = new List<Vector3>();

        foreach (KeyValuePair<Edge, int> kvp in edgeUsage)
        {
            if (kvp.Value == 1)
            {
                Vector3 startPos = vertices[kvp.Key.v1];
                Vector3 endPos = vertices[kvp.Key.v2];
                float edgeLength = Vector3.Distance(startPos, endPos);

                int pointCount = Mathf.RoundToInt(edgeLength / pointSpacing);
                if (pointCount == 0) continue;

                for (int i = 0; i < pointCount; i++)
                {
                    float t = (i + 0.5f) / pointCount;
                    Vector3 point = Vector3.Lerp(startPos, endPos, t);

                    if (NavMesh.SamplePosition(point, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                    {
                        candidatePoints.Add(hit.position);
                    }
                }
            }
        }

        // Remove clusters with Spatial Culling Pass
        float minAllowedDistance = pointSpacing * 0.75f;
        float minSqrDist = minAllowedDistance * minAllowedDistance;

        List<Vector3> filteredPoints = new List<Vector3>();

        foreach (Vector3 candidate in candidatePoints)
        {
            bool isClustered = false;

            foreach (Vector3 validPoint in filteredPoints)
            {
                if ((candidate - validPoint).sqrMagnitude < minSqrDist)
                {
                    // If points are separated by a wall, we keep them both
                    if (!NavMesh.Raycast(candidate, validPoint, out NavMeshHit hit, NavMesh.AllAreas))
                    {
                        isClustered = true;
                        break;
                    }
                }
            }

            if (!isClustered)
            {
                filteredPoints.Add(candidate);
                AddPointToGrid(candidate);
            }
        }
    }

    void AddPointToGrid(Vector3 point)
    {
        int x = Mathf.FloorToInt(point.x / cellSize);
        int y = Mathf.FloorToInt(point.y / cellHeight);
        int z = Mathf.FloorToInt(point.z / cellSize);
        Vector3Int cell = new(x, y, z);

        if (!grid.ContainsKey(cell))
        {
            grid[cell] = new List<Vector3>();
        }

        grid[cell].Add(point);

#if UNITY_EDITOR
        debugPoints.Add(point);
#endif
    }

    void AddEdge(Dictionary<Edge, int> dict, int v1, int v2)
    {
        Edge edge = new(v1, v2);
        if (dict.ContainsKey(edge)) dict[edge]++;
        else dict[edge] = 1;
    }

    // Access
    public Vector3 GetBestCover(NavMeshAgent agent, LayerMask checkCoverMask, Vector3 playerPos)
    {
        Vector3 agentPos = agent.transform.position;

        // Get base cell agent is in
        int baseX = Mathf.FloorToInt(agentPos.x / cellSize);
        int baseY = Mathf.FloorToInt(agentPos.y / cellHeight);
        int baseZ = Mathf.FloorToInt(agentPos.z / cellSize);

        Vector3 bestPoint = agentPos;
        float shortestPathDistance = float.MaxValue;
        
        void EvaluateCell(Vector3Int cell)
        {
            if (!grid.TryGetValue(cell, out List<Vector3> localPoints)) return;

            // Iterate ONLY through points in this specific 3D chunk
            foreach (Vector3 point in localPoints)
            {
                if (Physics.Linecast(point + new Vector3(0, 1, 0), playerPos, out RaycastHit hit, checkCoverMask))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        Debug.DrawLine(point + new Vector3(0, 1, 0), playerPos, Color.red, .1f);
                        continue;
                    }

                    Debug.DrawLine(point + new Vector3(0, 1, 0), playerPos, Color.green, .1f);
                }

                float pathDist = GetPathDistance(agent, point);
                if (pathDist < shortestPathDistance)
                {
                    shortestPathDistance = pathDist;
                    bestPoint = point;
                }
            }
        }
        
        EvaluateCell(new Vector3Int(baseX, baseY, baseZ));
        
        if (Mathf.Approximately(shortestPathDistance, float.MaxValue))
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;

                        EvaluateCell(new Vector3Int(baseX + x, baseY + y, baseZ + z));
                    }
                }
            }
        }

        return bestPoint;
    }

    // Helpers
    float GetPathDistance(NavMeshAgent agent, Vector3 target)
    {
        NavMeshPath path = new();
        if (NavMesh.CalculatePath(agent.transform.position, target, agent.areaMask, path))
        {
            if (path.status != NavMeshPathStatus.PathComplete) return float.MaxValue;

            float distance = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }

            return distance;
        }

        return float.MaxValue;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos || debugPoints.Count == 0) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        foreach (Vector3 point in debugPoints)
        {
            Gizmos.DrawSphere(point, 0.2f);
        }
    }
#endif
}