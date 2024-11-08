
using System.Collections.Generic;
using UnityEngine;
using GK;

public class BiomesGeneration : MonoBehaviour
{
    public int width = 256;
    public int height = 256;

    public float scale = 1f;
    public float borderDelta = 0.1f;

    [Range(1, 100)]
    public float noiseScale = 20f;

    [Range(0, 100)]
    public float noiseMagnitude = 20f;

    [Range(1, 10)]
    public int Segment = 3;

    [Range(1, 100)]
    public int offsetX = 5;
    [Range(1, 100)]
    public int offsetY = 5;

    public GameObject myMesh;
    [SerializeField] ConfigSO Config;

    private Texture2D tex;
    private Color[] colors;
    private MeshRenderer ren;
    private Vector2[][] vertices;
    private int numGrid;

    private void Awake()
    {
        InitReference();
        numGrid = Mathf.CeilToInt(Mathf.Sqrt(Config.numPoint));
    }

    private void InitReference()
    {
        if (ren == null) ren = GetComponent<MeshRenderer>();
        tex = new Texture2D(width, height, TextureFormat.RGB24, true);
    }

    // Generate random sites point on each grid cell
    void GenerateNewRandomPoints()
    {
        numGrid = Mathf.CeilToInt(Mathf.Sqrt(Config.numPoint));

        vertices = new Vector2[numGrid][];
        for (int i = 0; i < numGrid; i++)
        {
            vertices[i] = new Vector2[numGrid];
        }


        int cellHeight = Mathf.CeilToInt((float)height / numGrid);
        int cellWidth = Mathf.CeilToInt((float)width / numGrid);


        for (int r = 0; r < numGrid; r++)
        {
            for (int c = 0; c < numGrid; c++)
            {
                int rowMin = cellHeight * r;
                int rowMax = cellHeight * (r + 1) - 1;
                int colMin = cellWidth * c;
                int colMax = cellWidth * (c + 1) - 1;
                var randPt = new Vector2Int(Random.Range(colMin, colMax), Random.Range(rowMin, rowMax));
                vertices[r][c] = randPt;
            }
        }


    }

    // Generate texture to visualize Perlin noise
    public void GeneratePerlinNoiseTexture()
    {
        InitReference();

        PerlinNoiseTexture();

        tex.Apply();
        ren.material.mainTexture = tex;
    }

    void PerlinNoiseTexture()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float noise = Mathf.PerlinNoise((float)x / width * scale, (float)y / height * scale);
                var color = new Color(noise, noise, noise);
                tex.SetPixel(x, y, color);
            }
        }
    }

    // Generate texture to visualize Voronoi based on the sites generated with GenerateNewRandomPoints
    public void GenerateSimpleVoronoiTexture()
    {
        InitReference();

        GenerateNewRandomPoints();

        UseSimpleVoronoi();

        tex.Apply();
        ren.material.mainTexture = tex;
        // SaveTexture(tex);
    }
    // Simplified Voronoi generation (no hard algo)
    void UseSimpleVoronoi()
    {
        int[,] direction = {
            {1, 0},
            {0, 1},
            {-1, 0},
            {0, -1},
            {1, 1},
            {-1, -1},
            {1, -1},
            {-1, 1},
        };

        Color[][] sites = new Color[numGrid][];
        for (int i = 0; i < numGrid; i++)
        {
            sites[i] = new Color[numGrid];
        }
        float s = 1f;
        float v = 0.7f;

        int cellHeight = Mathf.CeilToInt((float)height / numGrid);
        int cellWidth = Mathf.CeilToInt((float)width / numGrid);

        float h = 0;

        for (int r = 0; r < numGrid; r++)
        {
            for (int c = 0; c < numGrid; c++)
            {
                var color = Color.HSVToRGB((h++ / (numGrid * numGrid) + 0.1f), s, v);
                sites[r][c] = color;
                if (r == 0 || c == 0 || r == numGrid - 1 || c == numGrid - 1)
                {
                    sites[r][c] = Color.gray;
                }

            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int colIndex = Mathf.FloorToInt(x / cellWidth);
                int rowIndex = Mathf.FloorToInt(y / cellHeight);
                var current = new Vector2(x, y);
                float closestDistance = Vector2.Distance(vertices[rowIndex][colIndex], current);

                Color site = sites[rowIndex][colIndex];
                float[] allDis = new float[9];
                int j = 0;
                allDis[j++] = closestDistance;
                for (int i = 0; i < direction.GetLength(0); i++)
                {
                    int dx = direction[i, 0];
                    int dy = direction[i, 1];
                    var nextRow = rowIndex + dy;
                    var nextCol = colIndex + dx;
                    if (nextRow < 0 || nextRow >= numGrid || nextCol < 0 || nextCol >= numGrid)
                    {
                        continue;
                    }
                    var dis = Vector2.Distance(vertices[nextRow][nextCol], current);
                    var sampleX = (float)Random.Range(1f, 10f) + (float)x / width * scale;
                    var sampleY = (float)Random.Range(1f, 10f) + (float)y / height * scale;
                    dis += (float)(Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f);
                    allDis[j++] = dis;
                    if (dis < closestDistance)
                    {
                        closestDistance = dis;
                        site = sites[nextRow][nextCol];
                    }
                }
                foreach (float dis in allDis)
                {
                    float delta = Mathf.Abs(dis - closestDistance);
                    if (delta > 0 && delta < borderDelta)
                    {
                        site = Color.black;
                        break;
                    };
                }
                tex.SetPixel(x, y, site);

            }
        }

        for (int r = 0; r < numGrid; r++)
        {
            for (int c = 0; c < numGrid; c++)
            {
                tex.SetPixel((int)vertices[r][c].x, (int)vertices[r][c].y, Color.black);
                for (int i = 0; i < direction.GetLength(0); i++)
                {
                    int dx = direction[i, 0];
                    int dy = direction[i, 1];
                    tex.SetPixel((int)vertices[r][c].x + dx, (int)vertices[r][c].y + dy, Color.black);
                }
            }
        }
    }
    // Helper function to create mesh from generated vertices and triangles
    public void VerticesToMesh(Vector3[] workingVertices, int[] triangles)
    {
        if (vertices == null)
        {
            return;
        }
        List<Vector3> normals = new List<Vector3>();
        for (int i = 0; i < workingVertices.Length; i++)
        {
            normals.Add(Vector3.up);
        }

        var meshFilter = myMesh.GetComponent<MeshFilter>();

        Mesh mesh = new Mesh();
        mesh.vertices = workingVertices;
        mesh.triangles = triangles;
        mesh.normals = normals.ToArray();
        // mesh.RecalculateBounds();
        // mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
    }
    // Save a PNG texture for debug
    private void SaveTexture(Texture2D texture)
    {
        byte[] bytes = texture.EncodeToPNG();
        var dirPath = Application.dataPath + "/RenderOutput";
        if (!System.IO.Directory.Exists(dirPath))
        {
            System.IO.Directory.CreateDirectory(dirPath);
        }
        System.IO.File.WriteAllBytes(dirPath + "/R_" + Random.Range(0, 100000) + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + dirPath);
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
    // Voronoi generation with delaunay triangle and Algo from external Lib
    public void UseVoronoiLib()
    {
        if (vertices == null)
        {
            GenerateNewRandomPoints();
        }

        var calc = new VoronoiCalculator();
        Dictionary<int, List<VoronoiDiagram.Edge>> SiteToEdgeMap = new Dictionary<int, List<VoronoiDiagram.Edge>>();

        Vector2[] OneDVertices = new Vector2[numGrid * numGrid];
        int vi = 0;
        for (int y = 0; y < numGrid; y++)
        {
            for (int x = 0; x < numGrid; x++)
            {
                OneDVertices[vi++] = vertices[y][x];
            }
        }

        VoronoiDiagram diagram = calc.CalculateDiagram(OneDVertices);
        List<Vector2> workingVertices = new List<Vector2>();
        HashSet<int> allVertices = new HashSet<int>();
        HashSet<int> sitesIndex = SamplePointsByNoise();

        foreach (VoronoiDiagram.Edge vEdge in diagram.Edges)
        {

            if (vEdge.Type == VoronoiDiagram.EdgeType.RayCCW || vEdge.Type == VoronoiDiagram.EdgeType.RayCW || vEdge.Type == VoronoiDiagram.EdgeType.Line)
            {
                sitesIndex.Remove(vEdge.Site);
            }
        }

        foreach (VoronoiDiagram.Edge vEdge in diagram.Edges)
        {
            if (!sitesIndex.Contains(vEdge.Site))
            {
                continue;
            }

            if (
                !(IsVertexInBound(diagram.Vertices[vEdge.Vert0], width, height) &&
                IsVertexInBound(diagram.Vertices[vEdge.Vert1], width, height))
                )
            {
                sitesIndex.Remove(vEdge.Site);
                continue;
            }

            if (!allVertices.Contains(vEdge.Vert0))
            {
                allVertices.Add(vEdge.Vert0);
                workingVertices.Add(diagram.Vertices[vEdge.Vert0]);
            }

            if (!allVertices.Contains(vEdge.Vert1))
            {
                allVertices.Add(vEdge.Vert1);
                workingVertices.Add(diagram.Vertices[vEdge.Vert1]);
            }

            if (SiteToEdgeMap.ContainsKey(vEdge.Site))
            {
                SiteToEdgeMap[vEdge.Site].Add(vEdge);
            }
            else
            {
                SiteToEdgeMap.Add(vEdge.Site, new List<VoronoiDiagram.Edge>() { vEdge });
            }
        }

        // UseDelaunayMesh(workingVertices);
        UseVoronoiCenteriodMesh(SiteToEdgeMap, diagram.Sites, diagram.Vertices);
    }

    // Sampled Voronoi sites and edges to create irregular plane mesh, with noisy edges
    void UseVoronoiCenteriodMesh(Dictionary<int, List<VoronoiDiagram.Edge>> SiteToEdgeMap, List<Vector2> voronoiSites, List<Vector2> voronoiVertices)
    {
        List<Vector3> meshVertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Dictionary<(int, int), int> EdgeToSiteMap = new Dictionary<(int, int), int>();
        foreach (int siteIdx in SiteToEdgeMap.Keys)
        {
            foreach (var edge in SiteToEdgeMap[siteIdx])
            {
                if (EdgeToSiteMap.ContainsKey((edge.Vert0, edge.Vert1)))
                {
                    EdgeToSiteMap[(edge.Vert0, edge.Vert1)] += 1;
                }
                else
                {
                    EdgeToSiteMap.Add((edge.Vert0, edge.Vert1), 1);
                };

                if (EdgeToSiteMap.ContainsKey((edge.Vert1, edge.Vert0)))
                {
                    EdgeToSiteMap[(edge.Vert1, edge.Vert0)] += 1;
                }
                else
                {
                    EdgeToSiteMap.Add((edge.Vert1, edge.Vert0), 1);
                };

            }
        }


        foreach (int siteIdx in SiteToEdgeMap.Keys)
        {
            var centriod = voronoiSites[siteIdx];
            meshVertices.Add(new Vector3(centriod.x, 0, centriod.y));
            int centriodIdx = meshVertices.Count - 1;
            foreach (var edge in SiteToEdgeMap[siteIdx])
            {
                if (!IsSharedEdge(EdgeToSiteMap, edge))
                {
                    Vector2[] pts = new Vector2[Segment + 1];
                    for (int i = 0; i < Segment; i++)
                    {
                        var p1 = pts[i];
                        float ratio = (float)i / (float)Segment;
                        if (i == 0)
                        {
                            p1 = Vector2.Lerp(voronoiVertices[edge.Vert0], voronoiVertices[edge.Vert1], ratio);
                        }
                        ratio = (float)(i + 1) / (float)Segment;
                        var p2 = Vector2.Lerp(voronoiVertices[edge.Vert0], voronoiVertices[edge.Vert1], ratio);
                        if (i < Segment - 1)
                        {
                            p2 = ResampleEdgeVertex(p2, (p2 - centriod).normalized);
                        }
                        pts[i] = p1;
                        pts[i + 1] = p2;
                        triangles.Add(centriodIdx);
                        meshVertices.Add(new Vector3(p1.x, 0, p1.y));
                        triangles.Add(meshVertices.Count - 1);
                        meshVertices.Add(new Vector3(p2.x, 0, p2.y));
                        triangles.Add(meshVertices.Count - 1);
                    }
                }
                else
                {
                    triangles.Add(centriodIdx);
                    meshVertices.Add(new Vector3(voronoiVertices[edge.Vert0].x, 0, voronoiVertices[edge.Vert0].y));
                    triangles.Add(meshVertices.Count - 1);
                    meshVertices.Add(new Vector3(voronoiVertices[edge.Vert1].x, 0, voronoiVertices[edge.Vert1].y));
                    triangles.Add(meshVertices.Count - 1);
                }

            }

        }
        triangles.Reverse();
        VerticesToMesh(meshVertices.ToArray(), triangles.ToArray());
    }

    Vector2 ResampleEdgeVertex(Vector2 pt, Vector2 direction)
    {
        float noise = 2f * (Mathf.PerlinNoise((float)pt.x / width * noiseScale + (float)offsetX, (float)pt.y / height * noiseScale + (float)offsetY) - 0.5f);

        var mag = noiseMagnitude * noise;
        return pt + direction * mag;
    }
    // Create a mesh from Delaunay triangles
    void UseDelaunayMesh(List<Vector2> vertices)
    {
        var delaunayCal = new DelaunayCalculator();
        var delaunayTri = delaunayCal.CalculateTriangulation(vertices);
        List<Vector3> vertices3D = new List<Vector3>();
        foreach (var vertex2D in vertices)
        {
            vertices3D.Add(new Vector3(vertex2D.x, 0, vertex2D.y));
        }
        delaunayTri.Triangles.Reverse();
        VerticesToMesh(vertices3D.ToArray(), delaunayTri.Triangles.ToArray());
    }

    bool IsSharedEdge(Dictionary<(int, int), int> EdgeToSiteMap, VoronoiDiagram.Edge edge)
    {
        if (EdgeToSiteMap.ContainsKey((edge.Vert0, edge.Vert1)))
        {
            return EdgeToSiteMap[(edge.Vert0, edge.Vert1)] > 1;
        }
        if (EdgeToSiteMap.ContainsKey((edge.Vert1, edge.Vert0)))
        {
            return EdgeToSiteMap[(edge.Vert1, edge.Vert0)] > 1;
        }
        return false;
    }

    bool IsVertexInBound(Vector2 vertex, int x, int y)
    {
        if (vertex.x <= 0 || vertex.y <= 0)
        {
            return false;
        }
        if (vertex.x >= x || vertex.y >= y)
        {
            return false;
        }
        return true;
    }

    // BFS expanding selected sites from center of the diagram
    HashSet<int> SamplePointsByNoise()
    {
        if (vertices == null)
        {
            GenerateNewRandomPoints();
        }

        int centerCellX = numGrid / 2;
        int centerCellY = numGrid / 2;
        HashSet<int> selectedIndex = new HashSet<int>();
        HashSet<int> visited = new HashSet<int>();
        int centerCellIndex = centerCellY * numGrid + centerCellX;
        selectedIndex.Add(centerCellIndex);
        visited.Add(centerCellIndex);
        Vector2 offset = new Vector2(Random.Range(3, 20), Random.Range(3, 20));

        float decay = 0.6f;
        float amp = 10f;

        Queue<int> Cells = new Queue<int>();
        Cells.Enqueue(centerCellIndex);
        int[,] direction = {
            {1, 0},
            {0, 1},
            {-1, 0},
            {0, -1},
        };

        while (Cells.Count > 0)
        {
            var curr = Cells.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                int nextX = curr % numGrid + direction[d, 0];
                int nextY = curr / numGrid + direction[d, 1];
                if (nextX <= 1 || nextY <= 1 || nextX >= numGrid - 1 || nextY >= numGrid - 1)
                {
                    continue;
                }
                int nextCellIndex = nextY * numGrid + nextX;
                if (
                    visited.Contains(nextCellIndex)
                )
                {
                    continue;
                }
                visited.Add(nextCellIndex);
                float noise = Mathf.PerlinNoise((float)(offset.x + (float)nextX / numGrid), (float)(offset.y + (float)nextY / numGrid));
                noise *= amp;
                amp *= decay;
                if (noise > 0.1)
                {
                    selectedIndex.Add(nextCellIndex);
                    Cells.Enqueue(nextCellIndex);
                }
            }
        }

        return selectedIndex;
    }
}
