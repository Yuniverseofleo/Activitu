
using System.Collections.Generic;
using UnityEngine;

public class ShapeMesh : MonoBehaviour
{
    public enum ShapeType
    {
        Pyramid,
        Sphere,
        Cube
    }

    [Header("Shape")]
    [SerializeField] private ShapeType shapeType;

    [Header("Appearance")]
    [SerializeField] private Material lineMat;
    [SerializeField] private Vector3 position = Vector3.zero;
    [SerializeField] private float scale = 1f;

    [Header("Sphere Settings")]
    [Range(6, 50)]
    [SerializeField] private int segments = 12;

    [Range(3, 30)]
    [SerializeField] private int rings = 8;

    [Header("Rotation")]
    [SerializeField] private float xRotationSpeed = 30f;
    [SerializeField] private float yRotationSpeed = 45f;

    [Header("Projection")]
    [Range(1, 500)]
    [SerializeField] private float focalLength = 20f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private readonly List<int> edges = new();

    private float xRotation;
    private float yRotation;

    private void Awake()
    {
        mesh = new Mesh();
        mesh.name = "Custom Wireframe Shape";

        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        filter.mesh = mesh;

        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();

        if (lineMat != null)
        {
            renderer.material = lineMat;
        }
        else
        {
            renderer.material = new Material(
                Shader.Find("Sprites/Default")
            );
        }

        GenerateShape();
    }

    private void Update()
    {
        xRotation += xRotationSpeed * Time.deltaTime;
        yRotation += yRotationSpeed * Time.deltaTime;

        UpdateMesh();
    }

    private void GenerateShape()
    {
        List<Vector3> vertices = new();

        edges.Clear();

        switch (shapeType)
        {
            case ShapeType.Cube:
                GenerateCube(vertices);
                break;

            case ShapeType.Pyramid:
                GeneratePyramid(vertices);
                break;

            case ShapeType.Sphere:
                GenerateSphere(vertices);
                break;
        }

        originalVertices = vertices.ToArray();
    }

    private void GenerateCube(List<Vector3> vertices)
    {
        vertices.AddRange(new Vector3[]
        {
            new(-1, -1, -1),
            new(1, -1, -1),
            new(1, 1, -1),
            new(-1, 1, -1),
            new(-1, -1, 1),
            new(1, -1, 1),
            new(1, 1, 1),
            new(-1, 1, 1)
        });

        edges.AddRange(new int[]
        {
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7
        });
    }

    private void GeneratePyramid(List<Vector3> vertices)
    {
        vertices.AddRange(new Vector3[]
        {
            new(-1, -1, -1),
            new(1, -1, -1),
            new(1, -1, 1),
            new(-1, -1, 1),
            new(0, 1.5f, 0)
        });

        edges.AddRange(new int[]
        {
            0, 1,
            1, 2,
            2, 3,
            3, 0,

            0, 4,
            1, 4,
            2, 4,
            3, 4
        });
    }

    private void GenerateSphere(List<Vector3> vertices)
    {
        vertices.Add(new Vector3(0, 1, 0));

        for (int r = 1; r < rings; r++)
        {
            float latitude = Mathf.PI * r / rings;

            for (int s = 0; s < segments; s++)
            {
                float longitude = 2f * Mathf.PI * s / segments;

                float x = Mathf.Sin(latitude) * Mathf.Cos(longitude);
                float y = Mathf.Cos(latitude);
                float z = Mathf.Sin(latitude) * Mathf.Sin(longitude);

                vertices.Add(new Vector3(x, y, z));
            }
        }

        int bottomIndex = vertices.Count;
        vertices.Add(new Vector3(0, -1, 0));

        // Connect the top pole
        for (int s = 0; s < segments; s++)
        {
            edges.Add(0);
            edges.Add(1 + s);
        }

        // Connect sphere rings
        for (int r = 0; r < rings - 1; r++)
        {
            int currentRing = 1 + r * segments;
            int nextRing = currentRing + segments;

            for (int s = 0; s < segments; s++)
            {
                int next = (s + 1) % segments;

                // Horizontal edges
                edges.Add(currentRing + s);
                edges.Add(currentRing + next);

                // Vertical edges
                if (r < rings - 2)
                {
                    edges.Add(currentRing + s);
                    edges.Add(nextRing + s);
                }
                else
                {
                    edges.Add(currentRing + s);
                    edges.Add(bottomIndex);
                }
            }
        }
    }

    private void UpdateMesh()
    {
        Vector3[] projectedVertices = new Vector3[originalVertices.Length];

        for (int i = 0; i < originalVertices.Length; i++)
        {
            projectedVertices[i] = Project(originalVertices[i]);
        }

        mesh.Clear();
        mesh.vertices = projectedVertices;
        mesh.SetIndices(edges.ToArray(), MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
    }

    private Vector3 Project(Vector3 point)
    {
        point *= 0.5f;

        // Rotate around X axis
        float x = point.x;

        float y = point.y * Mathf.Cos(xRotation * Mathf.Deg2Rad)
                - point.z * Mathf.Sin(xRotation * Mathf.Deg2Rad);

        float z = point.y * Mathf.Sin(xRotation * Mathf.Deg2Rad)
                + point.z * Mathf.Cos(xRotation * Mathf.Deg2Rad);

        point = new Vector3(x, y, z);

        // Rotate around Y axis
        float xRot = point.x * Mathf.Cos(yRotation * Mathf.Deg2Rad)
                   - point.z * Mathf.Sin(yRotation * Mathf.Deg2Rad);

        float zRot = point.x * Mathf.Sin(yRotation * Mathf.Deg2Rad)
                   + point.z * Mathf.Cos(yRotation * Mathf.Deg2Rad);

        point = new Vector3(xRot, point.y, zRot);

        point += position;

        // Perspective projection
        float perspective = focalLength / (focalLength + point.z);

        return new Vector3(
            point.x * scale * perspective,
            point.y * scale * perspective,
            0
        );
    }
}
