#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class FogTerrainReplicaGenerator
{
    private const float TerrainSize = 500f;
    private const float TerrainHeight = 130f;
    private const int HeightResolution = 513;
    private const int AlphamapResolution = 512;
    private const string GeneratedFolder = "Assets/GeneratedFogTerrain";
    private const string ScenePath = "Assets/Scenes/FogTerrainReplica.unity";

    private static Terrain terrain;
    private static TerrainData terrainData;
    private static Transform root;
    private static Materials mats;
    private static readonly System.Random rng = new System.Random(87421);

    private static readonly Vector2[] MainPath =
    {
        new Vector2(-220f, -175f),
        new Vector2(-150f, -115f),
        new Vector2(-70f, -68f),
        new Vector2(10f, -25f),
        new Vector2(83f, 32f),
        new Vector2(153f, 88f),
        new Vector2(220f, 165f),
    };

    private static readonly Vector2[] SidePath =
    {
        new Vector2(-210f, 75f),
        new Vector2(-126f, 42f),
        new Vector2(-35f, 5f),
        new Vector2(75f, -55f),
        new Vector2(195f, -135f),
    };

    [MenuItem("Tools/Fog Terrain Replica/Generate Terrain Scene")]
    public static void GenerateTerrainScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolders();
        mats = CreateMaterials();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject("Foggy Low Poly Terrain Replica Root").transform;

        CreateTerrain();
        PaintTerrainLayers();
        CreateLowPolyGroundVisual();
        CreateRoadSignposts();
        SpawnTrees();
        SpawnBoulders();
        SpawnCrateZones();
        CreateHidingSpaces();
        CreateWatchtower();
        CreateIndustrialFacility();
        CreateBackgroundMountains();
        SetupLightingFogAndCameras();
        CreateLargeEnemyRouteMarkers();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = root.gameObject;
        EditorUtility.DisplayDialog(
            "Fog Terrain Replica Created",
            "Generated one Unity Terrain scene with low-poly trees, boulders, hills, fog, crates, hiding spaces, mountains, cameras, and enemy route markers.\n\nOpen Assets/Scenes/FogTerrainReplica.unity",
            "Done");
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "GeneratedFogTerrain"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "GeneratedFogTerrain/Materials"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "GeneratedFogTerrain/Terrain"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "GeneratedFogTerrain/Meshes"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
        AssetDatabase.Refresh();
    }

    private static void CreateTerrain()
    {
        terrainData = new TerrainData
        {
            heightmapResolution = HeightResolution,
            alphamapResolution = AlphamapResolution,
            baseMapResolution = 512,
            size = new Vector3(TerrainSize, TerrainHeight, TerrainSize)
        };

        float[,] heights = new float[HeightResolution, HeightResolution];
        for (int z = 0; z < HeightResolution; z++)
        {
            for (int x = 0; x < HeightResolution; x++)
            {
                float wx = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, x / (float)(HeightResolution - 1));
                float wz = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, z / (float)(HeightResolution - 1));
                heights[z, x] = Mathf.Clamp01(EvaluateHeight01(wx, wz));
            }
        }

        terrainData.SetHeights(0, 0, heights);
        terrainData.name = "FogTerrainReplica_TerrainData";
        CreateOrReplaceAsset(terrainData, GeneratedFolder + "/Terrain/FogTerrainReplica_TerrainData.asset");

        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name = "Single Unity Terrain - Foggy Mountain Outpost";
        terrainObject.transform.position = new Vector3(-TerrainSize * 0.5f, 0f, -TerrainSize * 0.5f);
        terrainObject.transform.SetParent(root);

        terrain = terrainObject.GetComponent<Terrain>();
        terrain.heightmapPixelError = 3f;
        terrain.basemapDistance = 700f;
        terrain.drawInstanced = true;
    }

    private static float EvaluateHeight01(float wx, float wz)
    {
        float h = 0.035f;

        h += FractalNoise(wx, wz) * 0.045f;

        // Playable rolling hills and rocky shelves.
        h += Gaussian(wx, wz, -160f, -100f, 95f, 0.08f);
        h += Gaussian(wx, wz, 105f, -95f, 105f, 0.10f);
        h += Gaussian(wx, wz, -25f, 50f, 130f, 0.09f);
        h += Gaussian(wx, wz, 155f, 115f, 110f, 0.07f);

        // Larger perimeter peaks for the foggy mountain frame.
        h += Gaussian(wx, wz, -245f, 215f, 110f, 0.37f);
        h += Gaussian(wx, wz, 235f, 220f, 120f, 0.42f);
        h += Gaussian(wx, wz, -235f, -210f, 130f, 0.23f);
        h += Gaussian(wx, wz, 238f, -205f, 130f, 0.22f);

        // Central ridges, like the concept shots.
        h += Ridge(wx, wz, -85f, -55f, 110f, 0.042f);
        h += Ridge(wx, wz, 58f, 35f, 140f, 0.038f);

        float dMain = DistanceToPolyline(new Vector2(wx, wz), MainPath);
        float dSide = DistanceToPolyline(new Vector2(wx, wz), SidePath);
        float dPath = Mathf.Min(dMain, dSide);

        // Flatten the walkable dirt paths without making the whole terrain flat.
        if (dPath < 18f)
        {
            float flat = 0.055f + Mathf.PerlinNoise(wx * 0.018f + 4f, wz * 0.018f + 9f) * 0.012f;
            h = Mathf.Lerp(h, flat, Mathf.InverseLerp(18f, 0f, dPath) * 0.82f);
        }

        // Subtle edge drop gives cliff/ravine feeling.
        float edge = Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz));
        if (edge > 214f)
        {
            h -= Mathf.InverseLerp(214f, 250f, edge) * 0.035f;
        }

        return h;
    }

    private static float FractalNoise(float wx, float wz)
    {
        float n = 0f;
        float amp = 1f;
        float freq = 0.012f;
        float total = 0f;

        for (int i = 0; i < 4; i++)
        {
            n += (Mathf.PerlinNoise(wx * freq + 21.7f, wz * freq - 14.3f) - 0.5f) * amp;
            total += amp;
            amp *= 0.5f;
            freq *= 2f;
        }

        return n / total;
    }

    private static float Gaussian(float wx, float wz, float cx, float cz, float radius, float amp)
    {
        float dx = wx - cx;
        float dz = wz - cz;
        return Mathf.Exp(-(dx * dx + dz * dz) / (radius * radius)) * amp;
    }

    private static float Ridge(float wx, float wz, float cx, float cz, float radius, float amp)
    {
        float dx = wx - cx;
        float dz = wz - cz;
        float d = Mathf.Sqrt(dx * dx * 0.45f + dz * dz * 1.2f);
        return Mathf.Exp(-(d * d) / (radius * radius)) * amp;
    }

    private static void PaintTerrainLayers()
    {
        TerrainLayer grass = CreateLayer("Mossy_Grass_Layer", new Color(0.26f, 0.34f, 0.22f), 28f);
        TerrainLayer dirt = CreateLayer("Dirt_Path_Layer", new Color(0.39f, 0.32f, 0.24f), 20f);
        TerrainLayer rock = CreateLayer("Cold_Rock_Layer", new Color(0.34f, 0.36f, 0.36f), 35f);

        terrainData.terrainLayers = new[] { grass, dirt, rock };

        float[,,] alpha = new float[AlphamapResolution, AlphamapResolution, 3];
        for (int z = 0; z < AlphamapResolution; z++)
        {
            for (int x = 0; x < AlphamapResolution; x++)
            {
                float nx = x / (float)(AlphamapResolution - 1);
                float nz = z / (float)(AlphamapResolution - 1);
                float wx = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, nx);
                float wz = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, nz);
                float pathDistance = Mathf.Min(DistanceToPolyline(new Vector2(wx, wz), MainPath), DistanceToPolyline(new Vector2(wx, wz), SidePath));
                float steep = terrainData.GetSteepness(nx, nz);

                float dirtWeight = Mathf.InverseLerp(20f, 0f, pathDistance);
                float rockWeight = Mathf.InverseLerp(27f, 52f, steep) * 0.85f;
                float grassWeight = Mathf.Max(0.05f, 1f - dirtWeight - rockWeight);

                float total = grassWeight + dirtWeight + rockWeight;
                alpha[z, x, 0] = grassWeight / total;
                alpha[z, x, 1] = dirtWeight / total;
                alpha[z, x, 2] = rockWeight / total;
            }
        }

        terrainData.SetAlphamaps(0, 0, alpha);
    }

    private static TerrainLayer CreateLayer(string name, Color color, float tileSize)
    {
        Texture2D texture = CreateSolidTexture(name + "_Texture", color);
        TerrainLayer layer = new TerrainLayer
        {
            diffuseTexture = texture,
            tileSize = new Vector2(tileSize, tileSize),
            name = name
        };
        CreateOrReplaceAsset(layer, GeneratedFolder + "/Terrain/" + name + ".terrainlayer");
        return layer;
    }

    private static Texture2D CreateSolidTexture(string name, Color color)
    {
        Texture2D tex = new Texture2D(8, 8, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };

        Color[] pixels = new Color[64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        tex.SetPixels(pixels);
        tex.Apply();
        CreateOrReplaceAsset(tex, GeneratedFolder + "/Terrain/" + name + ".asset");
        return tex;
    }

    private static void CreateLowPolyGroundVisual()
    {
        int cells = 96;
        float step = TerrainSize / cells;

        List<Vector3> vertices = new List<Vector3>();
        List<int> grassTriangles = new List<int>();
        List<int> dirtTriangles = new List<int>();
        List<int> rockTriangles = new List<int>();

        for (int z = 0; z < cells; z++)
        {
            for (int x = 0; x < cells; x++)
            {
                Vector3 a = GroundPoint(-TerrainSize * 0.5f + x * step, -TerrainSize * 0.5f + z * step, 0.1f);
                Vector3 b = GroundPoint(-TerrainSize * 0.5f + (x + 1) * step, -TerrainSize * 0.5f + z * step, 0.1f);
                Vector3 c = GroundPoint(-TerrainSize * 0.5f + x * step, -TerrainSize * 0.5f + (z + 1) * step, 0.1f);
                Vector3 d = GroundPoint(-TerrainSize * 0.5f + (x + 1) * step, -TerrainSize * 0.5f + (z + 1) * step, 0.1f);

                int matA = GroundMaterialIndex((a + b + c) / 3f, a, b, c);
                AddTriangle(vertices, GetTriangleList(matA, grassTriangles, dirtTriangles, rockTriangles), a, c, b);

                int matB = GroundMaterialIndex((b + c + d) / 3f, b, c, d);
                AddTriangle(vertices, GetTriangleList(matB, grassTriangles, dirtTriangles, rockTriangles), b, c, d);
            }
        }

        Mesh mesh = new Mesh { name = "FacetedLowPolyTerrainVisual" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.subMeshCount = 3;
        mesh.SetTriangles(grassTriangles, 0);
        mesh.SetTriangles(dirtTriangles, 1);
        mesh.SetTriangles(rockTriangles, 2);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        CreateOrReplaceAsset(mesh, GeneratedFolder + "/Meshes/FacetedLowPolyTerrainVisual.asset");

        GameObject visual = new GameObject("Low Poly Ground Visual - faceted overlay");
        visual.transform.SetParent(root);
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = visual.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new[] { mats.grass, mats.dirt, mats.rock };
        mr.shadowCastingMode = ShadowCastingMode.On;
        mr.receiveShadows = true;
    }

    private static List<int> GetTriangleList(int matIndex, List<int> grass, List<int> dirt, List<int> rock)
    {
        if (matIndex == 1) return dirt;
        if (matIndex == 2) return rock;
        return grass;
    }

    private static int GroundMaterialIndex(Vector3 center, Vector3 a, Vector3 b, Vector3 c)
    {
        float pathDistance = Mathf.Min(DistanceToPolyline(new Vector2(center.x, center.z), MainPath), DistanceToPolyline(new Vector2(center.x, center.z), SidePath));
        if (pathDistance < 14f)
            return 1;

        float slope = Vector3.Angle(Vector3.up, Vector3.Cross(b - a, c - a).normalized);
        slope = Mathf.Min(slope, 180f - slope);
        if (slope > 36f || center.y > 23f)
            return 2;

        return 0;
    }

    private static void AddTriangle(List<Vector3> vertices, List<int> indices, Vector3 a, Vector3 b, Vector3 c)
    {
        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
    }

    private static void SpawnTrees()
    {
        Transform parent = Group("Low Poly Trees");
        int created = 0;
        int attempts = 0;

        while (created < 185 && attempts < 1000)
        {
            attempts++;
            float x = Range(-232f, 232f);
            float z = Range(-232f, 232f);
            float pathDistance = Mathf.Min(DistanceToPolyline(new Vector2(x, z), MainPath), DistanceToPolyline(new Vector2(x, z), SidePath));
            if (pathDistance < 18f) continue;

            float nx = Mathf.InverseLerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, x);
            float nz = Mathf.InverseLerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, z);
            if (terrainData.GetSteepness(nx, nz) > 37f) continue;

            Vector3 pos = GroundPoint(x, z, 0f);
            float scale = Range(0.75f, 1.65f);
            CreatePineTree("Pine_LowPoly_" + created.ToString("000"), pos, scale, parent);
            created++;
        }
    }

    private static void CreatePineTree(string name, Vector3 pos, float scale, Transform parent)
    {
        GameObject tree = new GameObject(name);
        tree.transform.position = pos;
        tree.transform.rotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
        tree.transform.SetParent(parent);

        GameObject trunk = MeshObject("Trunk", CreateCylinderMesh(0.28f, 1.9f, 6), mats.wood, tree.transform);
        trunk.transform.localPosition = new Vector3(0f, 0.95f * scale, 0f);
        trunk.transform.localScale = Vector3.one * scale;

        for (int i = 0; i < 3; i++)
        {
            float radius = (1.85f - i * 0.43f) * scale;
            float height = (2.1f - i * 0.28f) * scale;
            GameObject cone = MeshObject("Needles_" + i, CreateConeMesh(radius, height, 7), mats.tree, tree.transform);
            cone.transform.localPosition = new Vector3(0f, (2.0f + i * 1.1f) * scale, 0f);
        }

        CapsuleCollider col = tree.AddComponent<CapsuleCollider>();
        col.height = 5.6f * scale;
        col.radius = 0.65f * scale;
        col.center = new Vector3(0f, 2.8f * scale, 0f);
    }

    private static void SpawnBoulders()
    {
        Transform parent = Group("Boulders and Rock Cover");

        for (int i = 0; i < 95; i++)
        {
            float x = Range(-235f, 235f);
            float z = Range(-235f, 235f);
            float pathDistance = Mathf.Min(DistanceToPolyline(new Vector2(x, z), MainPath), DistanceToPolyline(new Vector2(x, z), SidePath));
            if (pathDistance < 7f) continue;

            float scale = Range(1.2f, 5.2f);
            CreateBoulder("LowPoly_Boulder_" + i.ToString("000"), GroundPoint(x, z, -0.05f), new Vector3(scale * Range(0.8f, 1.4f), scale * Range(0.5f, 1.1f), scale * Range(0.8f, 1.5f)), parent, true);
        }

        // Larger landmark rock walls near hide spots.
        CreateRockCluster(new Vector2(-176f, -152f), 8, parent);
        CreateRockCluster(new Vector2(118f, -98f), 10, parent);
        CreateRockCluster(new Vector2(82f, 78f), 9, parent);
    }

    private static void CreateRockCluster(Vector2 center, int amount, Transform parent)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector2 offset = RandomInsideCircle(Range(4f, 20f));
            float scale = Range(3f, 8f);
            CreateBoulder("RockCluster_" + center.x.ToString("0") + "_" + i, GroundPoint(center.x + offset.x, center.y + offset.y, -0.1f), new Vector3(scale * 1.2f, scale * 0.95f, scale), parent, true);
        }
    }

    private static void SpawnCrateZones()
    {
        Transform parent = Group("Industrial Crates and Props");

        CreateCrateCluster(new Vector3(-188f, 0f, -168f), 20f, parent);
        CreateCrateCluster(new Vector3(-92f, 0f, -42f), -25f, parent);
        CreateCrateCluster(new Vector3(30f, 0f, -8f), 55f, parent);
        CreateCrateCluster(new Vector3(126f, 0f, 68f), -15f, parent);
        CreateCrateCluster(new Vector3(185f, 0f, -118f), 65f, parent);

        CreateContainerHideZone(new Vector3(-24f, 0f, -14f), 13f, parent, "Central Container Cover");
        CreateContainerHideZone(new Vector3(138f, 0f, -72f), -30f, parent, "Right Ridge Container Cover");
    }

    private static void CreateCrateCluster(Vector3 center, float rotation, Transform parent)
    {
        Transform cluster = new GameObject("Crate Cluster").transform;
        cluster.position = GroundPoint(center.x, center.z, 0f);
        cluster.rotation = Quaternion.Euler(0f, rotation, 0f);
        cluster.SetParent(parent);

        int count = 4 + rng.Next(0, 5);
        for (int i = 0; i < count; i++)
        {
            Vector2 off = RandomInsideCircle(Range(2f, 8f));
            float size = Range(1.2f, 2.3f);
            CreateCrate("Wooden Crate", cluster.TransformPoint(new Vector3(off.x, size * 0.5f, off.y)), new Vector3(size, size, size), Quaternion.Euler(0f, Range(0f, 360f), 0f), cluster);
        }

        for (int i = 0; i < 2; i++)
        {
            Vector2 off = RandomInsideCircle(Range(2f, 7f));
            CreateBarrel(cluster.TransformPoint(new Vector3(off.x, 0.75f, off.y)), cluster);
        }
    }

    private static void CreateHidingSpaces()
    {
        Transform parent = Group("Player Hiding Spaces");

        CreateRockAlcove("Left Foreground Rock Alcove", new Vector2(-202f, -178f), 28f, parent);
        CreateRockAlcove("Mid Ridge Rock Alcove", new Vector2(72f, 52f), -25f, parent);
        CreateRockAlcove("Right Cliff Storage Cave", new Vector2(182f, -112f), -42f, parent);
        CreateTrenchHide("Wooden Trench Hide - Front Left", new Vector2(-122f, -184f), 12f, parent);
        CreateTrenchHide("Fence Pocket Hide - Lower Right", new Vector2(175f, -170f), -18f, parent);
        CreateCrateHide("Stacked Crate Hide - Central Path", new Vector2(18f, -38f), 45f, parent);
    }

    private static void CreateRockAlcove(string name, Vector2 center, float rotation, Transform parent)
    {
        Transform alcove = new GameObject(name).transform;
        alcove.position = GroundPoint(center.x, center.y, 0f);
        alcove.rotation = Quaternion.Euler(0f, rotation, 0f);
        alcove.SetParent(parent);

        CreateBoulder("Left Wall", alcove.TransformPoint(new Vector3(-5.5f, 0f, 1.5f)), new Vector3(7f, 5f, 8f), alcove, true);
        CreateBoulder("Right Wall", alcove.TransformPoint(new Vector3(5.5f, 0f, 1.5f)), new Vector3(7f, 5f, 8f), alcove, true);
        CreateBoulder("Back Rock", alcove.TransformPoint(new Vector3(0f, 0f, 6.4f)), new Vector3(11f, 6f, 4f), alcove, true);
        CreateBoulder("Roof Rock", alcove.TransformPoint(new Vector3(0f, 4.2f, 3.8f)), new Vector3(12f, 2.2f, 7f), alcove, true);

        GameObject back = Cube("Dark Interior", mats.dark, alcove);
        back.transform.localPosition = new Vector3(0f, 2.0f, 6.9f);
        back.transform.localScale = new Vector3(9f, 4f, 0.45f);

        CreateCrate("Alcove Crate", alcove.TransformPoint(new Vector3(-2.2f, 0.7f, 3.4f)), new Vector3(1.4f, 1.4f, 1.4f), alcove.rotation, alcove);
        AddWarmLight(alcove.TransformPoint(new Vector3(0f, 2.7f, 5.8f)), alcove, 1.2f, 9f);
        AddHideMarker(name + " HidePoint", alcove.TransformPoint(new Vector3(0f, 1f, 2.2f)), "Rock Alcove", alcove);
    }

    private static void CreateTrenchHide(string name, Vector2 center, float rotation, Transform parent)
    {
        Transform trench = new GameObject(name).transform;
        trench.position = GroundPoint(center.x, center.y, 0f);
        trench.rotation = Quaternion.Euler(0f, rotation, 0f);
        trench.SetParent(parent);

        CreateWallSegment("Back Wooden Cover", new Vector3(0f, 1.6f, 4.5f), new Vector3(10f, 3.2f, 0.55f), trench);
        CreateWallSegment("Left Wooden Cover", new Vector3(-5f, 1.4f, 0.3f), new Vector3(0.55f, 2.8f, 8f), trench);
        CreateWallSegment("Right Wooden Cover", new Vector3(5f, 1.4f, 0.3f), new Vector3(0.55f, 2.8f, 8f), trench);

        for (int i = 0; i < 4; i++)
        {
            CreateCrate("Small Trench Crate", trench.TransformPoint(new Vector3(Range(-3.2f, 3.2f), 0.55f, Range(-1.5f, 2.8f))), new Vector3(1.1f, 1.1f, 1.1f), trench.rotation, trench);
        }

        AddHideMarker(name + " HidePoint", trench.TransformPoint(new Vector3(0f, 1f, 1f)), "Wooden Trench", trench);
    }

    private static void CreateCrateHide(string name, Vector2 center, float rotation, Transform parent)
    {
        Transform hide = new GameObject(name).transform;
        hide.position = GroundPoint(center.x, center.y, 0f);
        hide.rotation = Quaternion.Euler(0f, rotation, 0f);
        hide.SetParent(parent);

        for (int i = 0; i < 10; i++)
        {
            float layer = i < 6 ? 0.8f : 2.25f;
            float x = (i % 3 - 1) * 1.8f;
            float z = i < 6 ? (i / 3) * 1.8f : 3.7f;
            CreateCrate("Stacked Hide Crate", hide.TransformPoint(new Vector3(x, layer, z)), new Vector3(1.55f, 1.55f, 1.55f), hide.rotation, hide);
        }

        AddHideMarker(name + " HidePoint", hide.TransformPoint(new Vector3(0f, 1f, -2.2f)), "Crate Stack", hide);
    }

    private static void CreateContainerHideZone(Vector3 center, float rotation, Transform parent, string name)
    {
        Transform zone = new GameObject(name).transform;
        zone.position = GroundPoint(center.x, center.z, 0f);
        zone.rotation = Quaternion.Euler(0f, rotation, 0f);
        zone.SetParent(parent);

        GameObject container = Cube("Industrial Container", mats.container, zone);
        container.transform.localPosition = new Vector3(0f, 2.1f, 0f);
        container.transform.localScale = new Vector3(9f, 4.2f, 4f);

        GameObject openDark = Cube("Open Dark Side", mats.dark, zone);
        openDark.transform.localPosition = new Vector3(0f, 2.15f, -2.05f);
        openDark.transform.localScale = new Vector3(7.7f, 2.8f, 0.1f);

        CreateCrate("Container Side Crate", zone.TransformPoint(new Vector3(-5.8f, 0.8f, -2.6f)), new Vector3(1.6f, 1.6f, 1.6f), zone.rotation, zone);
        CreateBarrel(zone.TransformPoint(new Vector3(5.5f, 0.7f, -2.6f)), zone);
        AddHideMarker(name + " HidePoint", zone.TransformPoint(new Vector3(0f, 1f, -3.4f)), "Container Cover", zone);
    }

    private static void CreateWatchtower()
    {
        Transform parent = Group("Watchtower Landmark");
        Transform tower = new GameObject("Central Watchtower").transform;
        tower.position = GroundPoint(-42f, 82f, 0f);
        tower.rotation = Quaternion.Euler(0f, -8f, 0f);
        tower.SetParent(parent);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                GameObject pole = Cube("Tower Leg", mats.woodDark, tower);
                pole.transform.localPosition = new Vector3(x * 2.5f, 8f, z * 2.5f);
                pole.transform.localScale = new Vector3(0.35f, 16f, 0.35f);
            }
        }

        for (int i = 0; i < 4; i++)
        {
            GameObject brace = Cube("Cross Brace", mats.woodDark, tower);
            brace.transform.localPosition = new Vector3(0f, 6f + i * 2f, 2.65f * (i % 2 == 0 ? 1 : -1));
            brace.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 35f : -35f);
            brace.transform.localScale = new Vector3(6f, 0.22f, 0.22f);
        }

        GameObject platform = Cube("Tower Platform", mats.wood, tower);
        platform.transform.localPosition = new Vector3(0f, 16f, 0f);
        platform.transform.localScale = new Vector3(7.8f, 0.55f, 7.8f);

        GameObject cabin = Cube("Lookout Cabin", mats.woodDark, tower);
        cabin.transform.localPosition = new Vector3(0f, 18.4f, 0f);
        cabin.transform.localScale = new Vector3(5.2f, 3.6f, 5.2f);

        AddWarmLight(tower.TransformPoint(new Vector3(0f, 18.7f, -2.9f)), tower, 0.9f, 18f);
    }

    private static void CreateIndustrialFacility()
    {
        Transform parent = Group("Distant Industrial Facility");
        Transform facility = new GameObject("Foggy Distant Factory Silhouette").transform;
        facility.position = GroundPoint(190f, 180f, 0.2f);
        facility.rotation = Quaternion.Euler(0f, -22f, 0f);
        facility.SetParent(parent);

        GameObject blockA = Cube("Factory Main Block", mats.concrete, facility);
        blockA.transform.localPosition = new Vector3(0f, 4f, 0f);
        blockA.transform.localScale = new Vector3(22f, 8f, 13f);

        GameObject blockB = Cube("Factory Side Block", mats.concreteDark, facility);
        blockB.transform.localPosition = new Vector3(-13f, 3f, 4f);
        blockB.transform.localScale = new Vector3(10f, 6f, 9f);

        GameObject stack = MeshObject("Smokestack", CreateCylinderMesh(1.7f, 24f, 10), mats.concreteDark, facility);
        stack.transform.localPosition = new Vector3(12f, 12f, 1f);

        GameObject bridge = Cube("Ravine Bridge", mats.woodDark, facility);
        bridge.transform.localPosition = new Vector3(-28f, 5.5f, -1f);
        bridge.transform.localScale = new Vector3(38f, 1.1f, 4f);
    }

    private static void CreateBackgroundMountains()
    {
        Transform parent = Group("Background Low Poly Mountains");
        CreateMountain("Back Left Mountain", new Vector2(-245f, 230f), 78f, 105f, parent);
        CreateMountain("Back Center Mountain", new Vector2(-10f, 248f), 92f, 135f, parent);
        CreateMountain("Back Right Mountain", new Vector2(245f, 230f), 96f, 145f, parent);
        CreateMountain("Far Right Peak", new Vector2(252f, 70f), 64f, 95f, parent);
    }

    private static void CreateMountain(string name, Vector2 center, float radius, float height, Transform parent)
    {
        Mesh mesh = CreateMountainMesh(radius, height, 11, 4);
        GameObject mountain = MeshObject(name, mesh, mats.mountain, parent);
        mountain.transform.position = GroundPoint(center.x, center.y, -0.2f);
        mountain.transform.rotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
        mountain.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    private static void SetupLightingFogAndCameras()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.025f;
        RenderSettings.fogColor = new Color(0.55f, 0.60f, 0.64f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.42f, 0.45f);
        RenderSettings.reflectionIntensity = 0.12f;

        GameObject sun = new GameObject("Cold Overcast Directional Light");
        sun.transform.rotation = Quaternion.Euler(47f, -37f, 0f);
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.72f, 0.78f, 0.82f);
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        sun.transform.SetParent(root);

        CreateCamera("Camera 01 - Wide Concept View", new Vector3(0f, 142f, -300f), new Vector3(52f, 0f, 0f), 50f);
        CreateCamera("Camera 02 - Overhead Layout View", new Vector3(0f, 235f, -120f), new Vector3(63f, 0f, 0f), 58f);
        CreateCamera("Camera 03 - Ground Gameplay Path View", new Vector3(-165f, 23f, -190f), new Vector3(11f, 42f, 0f), 62f, true);
    }

    private static void CreateCamera(string name, Vector3 pos, Vector3 rot, float fov, bool main = false)
    {
        GameObject camObject = new GameObject(name);
        camObject.transform.position = pos;
        camObject.transform.rotation = Quaternion.Euler(rot);
        camObject.transform.SetParent(root);

        Camera cam = camObject.AddComponent<Camera>();
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.15f;
        cam.farClipPlane = 650f;
        cam.enabled = main;
        cam.backgroundColor = RenderSettings.fogColor;
        cam.clearFlags = CameraClearFlags.SolidColor;

        if (main)
        {
            camObject.tag = "MainCamera";
            AudioListener listener = camObject.AddComponent<AudioListener>();
            listener.enabled = true;
        }
    }

    private static void CreateLargeEnemyRouteMarkers()
    {
        Transform parent = Group("Large Enemy Patrol Route Markers");
        Vector3[] points =
        {
            GroundPoint(-188f, -165f, 1.2f),
            GroundPoint(-92f, -72f, 1.2f),
            GroundPoint(14f, -30f, 1.2f),
            GroundPoint(94f, 38f, 1.2f),
            GroundPoint(170f, 88f, 1.2f),
        };

        for (int i = 0; i < points.Length; i++)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Enemy Patrol Point " + (i + 1).ToString("00");
            marker.transform.position = points[i];
            marker.transform.localScale = Vector3.one * 2f;
            marker.GetComponent<Renderer>().sharedMaterial = mats.enemyRoute;
            marker.transform.SetParent(parent);
        }
    }

    private static void CreateRoadSignposts()
    {
        Transform parent = Group("Small Path Details");
        for (int i = 0; i < MainPath.Length; i += 2)
        {
            Vector3 p = GroundPoint(MainPath[i].x + Range(-6f, 6f), MainPath[i].y + Range(-6f, 6f), 0f);
            GameObject post = Cube("Broken Signpost", mats.woodDark, parent);
            post.transform.position = p + Vector3.up * 1.3f;
            post.transform.localScale = new Vector3(0.25f, 2.6f, 0.25f);
            GameObject board = Cube("Small Board", mats.wood, post.transform);
            board.transform.localPosition = new Vector3(0f, 0.6f, 0.2f);
            board.transform.localRotation = Quaternion.Euler(0f, Range(-30f, 30f), Range(-10f, 10f));
            board.transform.localScale = new Vector3(2f, 0.45f, 0.18f);
        }
    }

    private static GameObject Cube(string name, Material material, Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        cube.transform.SetParent(parent);
        return cube;
    }

    private static void CreateWallSegment(string name, Vector3 localPosition, Vector3 localScale, Transform parent)
    {
        GameObject wall = Cube(name, mats.woodDark, parent);
        wall.transform.localPosition = localPosition;
        wall.transform.localScale = localScale;
    }

    private static void CreateCrate(string name, Vector3 worldPos, Vector3 scale, Quaternion rotation, Transform parent)
    {
        GameObject crate = Cube(name, mats.crate, parent);
        crate.transform.position = worldPos;
        crate.transform.rotation = rotation;
        crate.transform.localScale = scale;

        GameObject bandA = Cube("Crate Band A", mats.woodDark, crate.transform);
        bandA.transform.localScale = new Vector3(1.08f, 0.12f, 1.08f);
        bandA.transform.localPosition = new Vector3(0f, 0.32f, 0f);

        GameObject bandB = Cube("Crate Band B", mats.woodDark, crate.transform);
        bandB.transform.localScale = new Vector3(1.08f, 0.12f, 1.08f);
        bandB.transform.localPosition = new Vector3(0f, -0.32f, 0f);
    }

    private static void CreateBarrel(Vector3 worldPos, Transform parent)
    {
        GameObject barrel = MeshObject("Blue Industrial Barrel", CreateCylinderMesh(0.55f, 1.35f, 12), mats.barrel, parent);
        barrel.transform.position = worldPos;
        barrel.transform.rotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
        barrel.AddComponent<CapsuleCollider>().height = 1.35f;
    }

    private static void AddWarmLight(Vector3 worldPos, Transform parent, float intensity, float range)
    {
        GameObject lightObj = new GameObject("Warm Hide Space Light");
        lightObj.transform.position = worldPos;
        lightObj.transform.SetParent(parent);
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.62f, 0.32f);
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }

    private static void AddHideMarker(string name, Vector3 worldPosition, string type, Transform parent)
    {
        GameObject marker = new GameObject(name);
        marker.transform.position = worldPosition;
        marker.transform.SetParent(parent);
        HideSpotMarker hideSpot = marker.AddComponent<HideSpotMarker>();
        hideSpot.hideType = type;
        hideSpot.safeRadius = 4f;
    }

    private static void CreateBoulder(string name, Vector3 worldPosition, Vector3 scale, Transform parent, bool collider)
    {
        Mesh mesh = CreateBoulderMesh();
        GameObject rock = MeshObject(name, mesh, mats.rock, parent);
        rock.transform.position = worldPosition;
        rock.transform.rotation = Quaternion.Euler(Range(-6f, 6f), Range(0f, 360f), Range(-6f, 6f));
        rock.transform.localScale = scale;

        if (collider)
        {
            MeshCollider mc = rock.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
        }
    }

    private static GameObject MeshObject(string name, Mesh mesh, Material material, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.On;
        mr.receiveShadows = true;
        return go;
    }

    private static Mesh CreateConeMesh(float radius, float height, int segments)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Vector3 tip = new Vector3(0f, height, 0f);
        Vector3 center = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i * Mathf.PI * 2f / segments;
            float a1 = (i + 1) * Mathf.PI * 2f / segments;
            Vector3 p0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
            Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
            AddTriangle(vertices, triangles, p0, tip, p1);
            AddTriangle(vertices, triangles, center, p1, p0);
        }

        Mesh mesh = new Mesh { name = "LowPolyCone" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateCylinderMesh(float radius, float height, int segments)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        float half = height * 0.5f;
        Vector3 topCenter = new Vector3(0f, half, 0f);
        Vector3 bottomCenter = new Vector3(0f, -half, 0f);

        for (int i = 0; i < segments; i++)
        {
            float a0 = i * Mathf.PI * 2f / segments;
            float a1 = (i + 1) * Mathf.PI * 2f / segments;
            Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, -half, Mathf.Sin(a0) * radius);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, -half, Mathf.Sin(a1) * radius);
            Vector3 t0 = new Vector3(Mathf.Cos(a0) * radius, half, Mathf.Sin(a0) * radius);
            Vector3 t1 = new Vector3(Mathf.Cos(a1) * radius, half, Mathf.Sin(a1) * radius);

            AddTriangle(vertices, triangles, b0, t0, b1);
            AddTriangle(vertices, triangles, b1, t0, t1);
            AddTriangle(vertices, triangles, topCenter, t1, t0);
            AddTriangle(vertices, triangles, bottomCenter, b0, b1);
        }

        Mesh mesh = new Mesh { name = "LowPolyCylinder" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateBoulderMesh()
    {
        Vector3[] baseVerts =
        {
            new Vector3(-1, 1.618f, 0), new Vector3(1, 1.618f, 0), new Vector3(-1, -1.618f, 0), new Vector3(1, -1.618f, 0),
            new Vector3(0, -1, 1.618f), new Vector3(0, 1, 1.618f), new Vector3(0, -1, -1.618f), new Vector3(0, 1, -1.618f),
            new Vector3(1.618f, 0, -1), new Vector3(1.618f, 0, 1), new Vector3(-1.618f, 0, -1), new Vector3(-1.618f, 0, 1)
        };

        int[,] faces =
        {
            {0,11,5},{0,5,1},{0,1,7},{0,7,10},{0,10,11},
            {1,5,9},{5,11,4},{11,10,2},{10,7,6},{7,1,8},
            {3,9,4},{3,4,2},{3,2,6},{3,6,8},{3,8,9},
            {4,9,5},{2,4,11},{6,2,10},{8,6,7},{9,8,1}
        };

        Vector3[] distorted = new Vector3[baseVerts.Length];
        for (int i = 0; i < baseVerts.Length; i++)
            distorted[i] = Distort(baseVerts[i].normalized);

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int f = 0; f < faces.GetLength(0); f++)
        {
            Vector3 a = distorted[faces[f, 0]];
            Vector3 b = distorted[faces[f, 1]];
            Vector3 c = distorted[faces[f, 2]];
            AddTriangle(verts, tris, a, b, c);
        }

        Mesh mesh = new Mesh { name = "FacetedBoulder" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector3 Distort(Vector3 v)
    {
        float r = Range(0.72f, 1.18f);
        return new Vector3(v.x * r, v.y * Range(0.55f, 1.05f), v.z * r);
    }

    private static Mesh CreateMountainMesh(float radius, float height, int sides, int rings)
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        Vector3 tip = new Vector3(0f, height, 0f);

        for (int ring = 0; ring < rings; ring++)
        {
            float t0 = ring / (float)rings;
            float t1 = (ring + 1) / (float)rings;
            float r0 = Mathf.Lerp(radius, radius * 0.15f, t0);
            float r1 = Mathf.Lerp(radius, radius * 0.15f, t1);
            float y0 = height * t0 * 0.85f;
            float y1 = height * t1 * 0.85f;

            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides;
                float a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p00 = new Vector3(Mathf.Cos(a0) * r0, y0, Mathf.Sin(a0) * r0);
                Vector3 p01 = new Vector3(Mathf.Cos(a1) * r0, y0, Mathf.Sin(a1) * r0);
                Vector3 p10 = ring == rings - 1 ? tip : new Vector3(Mathf.Cos(a0) * r1, y1, Mathf.Sin(a0) * r1);
                Vector3 p11 = ring == rings - 1 ? tip : new Vector3(Mathf.Cos(a1) * r1, y1, Mathf.Sin(a1) * r1);

                AddTriangle(verts, tris, p00, p10, p01);
                if (ring != rings - 1)
                    AddTriangle(verts, tris, p01, p10, p11);
            }
        }

        Mesh mesh = new Mesh { name = "LowPolyMountain" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector3 GroundPoint(float x, float z, float yOffset)
    {
        float y = terrain != null ? terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y : 0f;
        return new Vector3(x, y + yOffset, z);
    }

    private static float DistanceToPolyline(Vector2 point, Vector2[] line)
    {
        float best = float.MaxValue;
        for (int i = 0; i < line.Length - 1; i++)
        {
            best = Mathf.Min(best, DistanceToSegment(point, line[i], line[i + 1]));
        }
        return best;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    private static Vector2 RandomInsideCircle(float radius)
    {
        float a = Range(0f, Mathf.PI * 2f);
        float r = Mathf.Sqrt(Range(0f, 1f)) * radius;
        return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
    }

    private static Transform Group(string name)
    {
        Transform t = root.Find(name);
        if (t != null) return t;
        GameObject go = new GameObject(name);
        go.transform.SetParent(root);
        return go.transform;
    }

    private static float Range(float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private static Materials CreateMaterials()
    {
        return new Materials
        {
            grass = CreateMaterial("LowPoly_Moss_Grass", new Color(0.24f, 0.32f, 0.21f)),
            dirt = CreateMaterial("LowPoly_Dirt_Path", new Color(0.39f, 0.32f, 0.24f)),
            rock = CreateMaterial("LowPoly_Cold_Rock", new Color(0.34f, 0.36f, 0.36f)),
            mountain = CreateMaterial("LowPoly_Distant_Mountain", new Color(0.31f, 0.34f, 0.36f)),
            tree = CreateMaterial("LowPoly_Dark_Pine", new Color(0.13f, 0.20f, 0.14f)),
            wood = CreateMaterial("Aged_Wood", new Color(0.34f, 0.25f, 0.17f)),
            woodDark = CreateMaterial("Dark_Aged_Wood", new Color(0.18f, 0.14f, 0.10f)),
            crate = CreateMaterial("Military_Wood_Crate", new Color(0.28f, 0.23f, 0.15f)),
            barrel = CreateMaterial("Weathered_Blue_Barrel", new Color(0.16f, 0.25f, 0.31f)),
            container = CreateMaterial("Olive_Industrial_Container", new Color(0.20f, 0.27f, 0.22f)),
            concrete = CreateMaterial("Foggy_Concrete", new Color(0.37f, 0.39f, 0.38f)),
            concreteDark = CreateMaterial("Dark_Foggy_Concrete", new Color(0.24f, 0.25f, 0.24f)),
            dark = CreateMaterial("Deep_Cave_Interior", new Color(0.04f, 0.045f, 0.045f)),
            enemyRoute = CreateMaterial("Enemy_Route_Marker", new Color(0.8f, 0.15f, 0.15f))
        };
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("HDRP/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader) { name = name, enableInstancing = true };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.18f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

        CreateOrReplaceAsset(mat, GeneratedFolder + "/Materials/" + name + ".mat");
        return mat;
    }

    private static void CreateOrReplaceAsset(Object asset, string path)
    {
        Object existing = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
    }

    private class Materials
    {
        public Material grass;
        public Material dirt;
        public Material rock;
        public Material mountain;
        public Material tree;
        public Material wood;
        public Material woodDark;
        public Material crate;
        public Material barrel;
        public Material container;
        public Material concrete;
        public Material concreteDark;
        public Material dark;
        public Material enemyRoute;
    }
}
#endif
