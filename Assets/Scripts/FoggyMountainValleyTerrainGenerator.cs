using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class FoggyMountainValleyTerrainGenerator : MonoBehaviour
{
    [Header("Core")]
    public int seed = 2312268;
    public bool clearPreviousGeneratedProps = true;
    public Terrain targetTerrain;

    [Header("Terrain Size")]
    public int terrainWidth = 500;
    public int terrainLength = 500;
    public int terrainHeight = 120;
    public int heightmapResolution = 1025;
    public int alphamapResolution = 1024;
    public int detailResolution = 1024;
    public int detailResolutionPerPatch = 16;

    [Header("Mountain Shape")]
    [Range(0.1f, 2f)] public float overallHeightMultiplier = 1.0f;
    [Range(0f, 1f)] public float valleyFloorHeight = 0.105f;
    [Range(0f, 1f)] public float leftRidgeX = 0.22f;
    [Range(0f, 1f)] public float rightRidgeX = 0.78f;
    [Range(0.01f, 0.4f)] public float ridgeWidth = 0.16f;
    [Range(0f, 1f)] public float ridgeHeight = 0.78f;
    [Range(0f, 1f)] public float sideMountainHeight = 0.55f;
    [Range(0f, 1f)] public float midHillHeight = 0.22f;
    [Range(0f, 1f)] public float surfaceRoughness = 0.075f;
    [Range(0f, 1f)] public float jaggedPeakAmount = 0.34f;
    [Range(0f, 1f)] public float terraceAmount = 0.22f;

    [Header("Noise")]
    public float largeNoiseScale = 1.8f;
    public float mediumNoiseScale = 5.4f;
    public float smallNoiseScale = 18f;
    public float microNoiseScale = 42f;
    public int fractalOctaves = 5;
    [Range(0.1f, 0.9f)] public float fractalPersistence = 0.48f;
    [Range(1.5f, 3.5f)] public float fractalLacunarity = 2.05f;

    [Header("Valley Path")]
    [Range(0.01f, 0.2f)] public float pathWidth = 0.045f;
    [Range(0f, 0.12f)] public float pathFlattenWidth = 0.075f;
    [Range(0f, 1f)] public float pathLowering = 0.028f;
    [Range(0f, 0.2f)] public float pathWindingAmount = 0.105f;
    [Range(0f, 0.1f)] public float pathTextureSoftness = 0.028f;
    public int cachedPathPointCount = 220;

    [Header("Clearing")]
    public Vector2 clearingCenter = new Vector2(0.47f, 0.42f);
    [Range(0.02f, 0.25f)] public float clearingRadius = 0.13f;
    [Range(0f, 0.25f)] public float clearingEdgeBand = 0.045f;
    [Range(0f, 1f)] public float clearingFlattenStrength = 0.86f;
    [Range(0f, 0.25f)] public float clearingTargetHeight = 0.122f;

    [Header("Rocky Outcrop")]
    public Vector2 rockyOutcropCenter = new Vector2(0.87f, 0.52f);
    [Range(0.02f, 0.35f)] public float rockyOutcropRadius = 0.17f;
    [Range(0f, 1f)] public float rockyOutcropHeight = 0.28f;
    [Range(0f, 1f)] public float ravineDepth = 0.34f;

    [Header("Terrain Textures")]
    public Texture2D mossGrassTexture;
    public Texture2D rockTexture;
    public Texture2D dirtPathTexture;
    public Texture2D darkSoilTexture;
    public Texture2D gravelTexture;

    public Vector2 grassTileSize = new Vector2(18, 18);
    public Vector2 rockTileSize = new Vector2(22, 22);
    public Vector2 dirtTileSize = new Vector2(14, 14);
    public Vector2 darkSoilTileSize = new Vector2(18, 18);
    public Vector2 gravelTileSize = new Vector2(10, 10);

    [Header("Texture Blend Rules")]
    [Range(0f, 1f)] public float rockSlopeStart = 0.34f;
    [Range(0f, 1f)] public float rockSlopeFull = 0.68f;
    [Range(0f, 1f)] public float highAltitudeRockStart = 0.42f;
    [Range(0f, 1f)] public float gravelAroundCampStrength = 0.42f;
    [Range(0f, 1f)] public float darkSoilLowAreaStrength = 0.20f;

    [Header("Trees")]
    public GameObject[] pineTreePrefabs;
    public int treeCandidateCount = 7000;
    [Range(0f, 1f)] public float treeDensity = 0.38f;
    public float minTreeDistance = 5.5f;
    public float pathTreeExclusionDistance = 16f;
    public float clearingTreeExclusionDistance = 55f;
    [Range(0f, 1f)] public float minTreeSlope = 0.05f;
    [Range(0f, 1f)] public float maxTreeSlope = 0.60f;
    public Vector2 treeHeightScale = new Vector2(0.75f, 1.45f);
    public Vector2 treeWidthScale = new Vector2(0.75f, 1.25f);

    [Header("Underbrush Details")]
    public Texture2D underbrushTexture;
    [Range(0f, 1f)] public float underbrushDensity = 0.38f;
    public int maxUnderbrushPerPatch = 4;
    public Color underbrushHealthyColor = new Color(0.30f, 0.38f, 0.24f);
    public Color underbrushDryColor = new Color(0.18f, 0.20f, 0.15f);

    [Header("Prop Prefabs")]
    public GameObject[] cratePrefabs;
    public GameObject[] barrelPrefabs;
    public GameObject[] fencePrefabs;
    public GameObject[] watchtowerPrefabs;
    public GameObject[] caveEntrancePrefabs;
    public GameObject[] bridgeMarkerPrefabs;

    [Header("Prop Density")]
    public int crateCount = 32;
    public int barrelCount = 14;
    public int fenceCount = 18;
    public int caveEntranceCount = 3;
    public int bridgeMarkerCount = 2;
    public float crateMinSpacing = 9f;
    public float barrelMinSpacing = 7f;
    public float fenceMinSpacing = 10f;
    public float caveMinSpacing = 55f;
    public float bridgeMinSpacing = 80f;
    public float maxPropSlopeDegrees = 24f;
    public float propPathOffsetMin = 7f;
    public float propPathOffsetMax = 20f;
    public float propClearingEdgeOffset = 12f;
    public Vector2 propScaleRange = new Vector2(0.85f, 1.25f);

    [Header("Special Placement")]
    public Vector2 watchtowerPosition = new Vector2(0.58f, 0.49f);
    public Vector2 mainCavePosition = new Vector2(0.18f, 0.36f);
    public Vector2 rightCavePosition = new Vector2(0.76f, 0.48f);

    [Header("Atmosphere")]
    public bool applyAtmosphere = true;
    public Color fogColor = new Color(0.55f, 0.63f, 0.67f);
    [Range(0.001f, 0.08f)] public float fogDensity = 0.022f;
    public Color ambientColor = new Color(0.45f, 0.49f, 0.51f);
    public Color directionalLightColor = new Color(0.70f, 0.76f, 0.80f);
    [Range(0f, 2f)] public float directionalLightIntensity = 0.65f;

    [Header("Debug")]
    public bool drawPathGizmos = true;
    public bool drawClearingGizmos = true;

    private const string GeneratedRootName = "Generated_Foggy_Mountain_Valley";
    private const int GrassLayer = 0;
    private const int RockLayer = 1;
    private const int DirtLayer = 2;
    private const int DarkSoilLayer = 3;
    private const int GravelLayer = 4;

    private float[,] generatedHeights;
    private List<Vector2> cachedPathPoints = new List<Vector2>();
    private System.Random rng;

    [ContextMenu("Generate Foggy Mountain Valley")]
    public void Generate()
    {
        rng = new System.Random(seed);

        if (clearPreviousGeneratedProps)
            ClearGeneratedRoot();

        cachedPathPoints = BuildPathPoints();

        targetTerrain = CreateOrPrepareTerrain();
        GenerateHeightmap(targetTerrain.terrainData);
        PaintTerrainLayers(targetTerrain.terrainData);
        ScatterUnderbrushDetails(targetTerrain.terrainData);
        ScatterTerrainTrees(targetTerrain.terrainData);
        ScatterProps();
        PlaceSpecialProps();
        ApplyAtmosphereSettings();

        targetTerrain.Flush();
    }

    private Terrain CreateOrPrepareTerrain()
    {
        Terrain terrain = targetTerrain;

        if (terrain == null)
            terrain = GetComponent<Terrain>();

        if (terrain == null)
            terrain = Terrain.activeTerrain;

        TerrainData data = new TerrainData();
        data.heightmapResolution = Mathf.ClosestPowerOfTwo(heightmapResolution - 1) + 1;
        data.alphamapResolution = Mathf.ClosestPowerOfTwo(alphamapResolution);
        data.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        if (terrain == null)
        {
            GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
            terrainObject.name = "Foggy_Mountain_Valley_Terrain";
            terrain = terrainObject.GetComponent<Terrain>();
        }
        else
        {
            terrain.terrainData = data;

            TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
            if (terrainCollider == null)
                terrainCollider = terrain.gameObject.AddComponent<TerrainCollider>();

            terrainCollider.terrainData = data;
        }

        terrain.transform.position = Vector3.zero;
        terrain.heightmapPixelError = 8f;
        terrain.basemapDistance = 1000f;
        terrain.drawInstanced = true;

        targetTerrain = terrain;
        return terrain;
    }

    private void ClearGeneratedRoot()
    {
        GameObject existing = GameObject.Find(GeneratedRootName);

        if (existing == null)
            return;

        if (Application.isPlaying)
            Destroy(existing);
        else
            DestroyImmediate(existing);
    }

    private Transform GetGeneratedRoot()
    {
        GameObject root = GameObject.Find(GeneratedRootName);

        if (root == null)
            root = new GameObject(GeneratedRootName);

        return root.transform;
    }

    private void GenerateHeightmap(TerrainData terrainData)
    {
        int res = terrainData.heightmapResolution;
        generatedHeights = new float[res, res];

        float offsetX = seed * 0.0137f;
        float offsetZ = seed * 0.0191f;

        for (int z = 0; z < res; z++)
        {
            float nz = z / (float)(res - 1);

            for (int x = 0; x < res; x++)
            {
                float nx = x / (float)(res - 1);

                float pathX = GetPathCenterX(nz);
                float valleyDistance = Mathf.Abs(nx - pathX);
                float valleyMask = 1f - Smooth01(valleyDistance / 0.36f);

                float leftRidge = RidgeMask(nx, leftRidgeX, ridgeWidth);
                float rightRidge = RidgeMask(nx, rightRidgeX, ridgeWidth);
                float sideMask = Mathf.Clamp01(Mathf.Max(leftRidge, rightRidge));

                float backMountainMask = Smooth01(nz);
                float frontMountainMask = Smooth01(1f - nz) * 0.42f;

                float largeNoise = Fbm(nx * largeNoiseScale + offsetX, nz * largeNoiseScale + offsetZ, fractalOctaves, fractalPersistence, fractalLacunarity);
                float mediumNoise = Fbm(nx * mediumNoiseScale + offsetX, nz * mediumNoiseScale + offsetZ, 4, 0.52f, 2.1f);
                float smallNoise = Fbm(nx * smallNoiseScale + offsetX, nz * smallNoiseScale + offsetZ, 3, 0.46f, 2.05f);
                float ridgedNoise = RidgedFbm(nx * mediumNoiseScale + offsetX * 1.3f, nz * mediumNoiseScale + offsetZ * 1.2f, 5);

                float mountainBase =
                    sideMask * ridgeHeight +
                    backMountainMask * sideMountainHeight * 0.55f +
                    frontMountainMask * sideMountainHeight * 0.22f;

                float jaggedPeaks =
                    sideMask *
                    Mathf.Pow(ridgedNoise, 1.45f) *
                    jaggedPeakAmount;

                float midHills =
                    mediumNoise *
                    midHillHeight *
                    Mathf.Lerp(1f, 0.35f, valleyMask);

                float roughness =
                    smallNoise *
                    surfaceRoughness *
                    Mathf.Lerp(0.35f, 1f, sideMask);

                float height =
                    valleyFloorHeight +
                    mountainBase +
                    jaggedPeaks +
                    midHills +
                    roughness +
                    largeNoise * 0.04f;

                height = ApplyValleyLowering(height, valleyMask);
                height = ApplyPathCarving(height, nx, nz);
                height = ApplyClearingFlatten(height, nx, nz);
                height = ApplyRockyOutcrop(height, nx, nz, ridgedNoise);
                height = ApplyRavineCut(height, nx, nz);

                float micro = Fbm(nx * microNoiseScale + offsetX, nz * microNoiseScale + offsetZ, 2, 0.4f, 2f);
                height += micro * 0.012f * Mathf.Lerp(0.4f, 1f, sideMask);

                if (sideMask > 0.28f && terraceAmount > 0f)
                {
                    float terraced = Mathf.Round(height * 24f) / 24f;
                    height = Mathf.Lerp(height, terraced, terraceAmount * sideMask);
                }

                height = Mathf.Clamp01(height * overallHeightMultiplier);
                generatedHeights[z, x] = height;
            }
        }

        SmoothValleyOnly(generatedHeights, 2, 0.38f);
        terrainData.SetHeights(0, 0, generatedHeights);
    }

    private float ApplyValleyLowering(float height, float valleyMask)
    {
        float lowered = height - valleyMask * 0.08f;
        return Mathf.Lerp(height, lowered, 0.72f);
    }

    private float ApplyPathCarving(float height, float nx, float nz)
    {
        float dist = DistanceToPath01(new Vector2(nx, nz));
        float pathMask = 1f - Smooth01(dist / pathFlattenWidth);

        if (pathMask <= 0f)
            return height;

        float pathTarget = Mathf.Max(0.075f, height - pathLowering);
        float flattenTarget = Mathf.Lerp(pathTarget, clearingTargetHeight, 0.16f);

        return Mathf.Lerp(height, flattenTarget, pathMask * 0.88f);
    }

    private float ApplyClearingFlatten(float height, float nx, float nz)
    {
        float d = Vector2.Distance(new Vector2(nx, nz), clearingCenter);
        float mask = 1f - Smooth01(d / clearingRadius);

        if (mask <= 0f)
            return height;

        float shapedTarget = clearingTargetHeight + Fbm(nx * 24f, nz * 24f, 2, 0.45f, 2f) * 0.012f;
        return Mathf.Lerp(height, shapedTarget, mask * clearingFlattenStrength);
    }

    private float ApplyRockyOutcrop(float height, float nx, float nz, float ridgedNoise)
    {
        float d = Vector2.Distance(new Vector2(nx, nz), rockyOutcropCenter);
        float mask = 1f - Smooth01(d / rockyOutcropRadius);

        if (mask <= 0f)
            return height;

        float outcrop = rockyOutcropHeight * mask * Mathf.Lerp(0.55f, 1.25f, ridgedNoise);
        return Mathf.Clamp01(height + outcrop);
    }

    private float ApplyRavineCut(float height, float nx, float nz)
    {
        float edgeMask = Smooth01((nx - 0.88f) / 0.12f);
        float zMask = 1f - Smooth01(Mathf.Abs(nz - 0.55f) / 0.36f);
        float cut = edgeMask * zMask * ravineDepth;

        return Mathf.Clamp01(height - cut);
    }

    private void SmoothValleyOnly(float[,] heights, int iterations, float strength)
    {
        int resZ = heights.GetLength(0);
        int resX = heights.GetLength(1);

        for (int i = 0; i < iterations; i++)
        {
            float[,] copy = (float[,])heights.Clone();

            for (int z = 1; z < resZ - 1; z++)
            {
                float nz = z / (float)(resZ - 1);

                for (int x = 1; x < resX - 1; x++)
                {
                    float nx = x / (float)(resX - 1);
                    float pathX = GetPathCenterX(nz);
                    float valleyDistance = Mathf.Abs(nx - pathX);
                    float valleyMask = 1f - Smooth01(valleyDistance / 0.32f);

                    if (valleyMask <= 0.05f)
                        continue;

                    float avg =
                        copy[z, x] +
                        copy[z - 1, x] +
                        copy[z + 1, x] +
                        copy[z, x - 1] +
                        copy[z, x + 1];

                    avg /= 5f;
                    heights[z, x] = Mathf.Lerp(copy[z, x], avg, strength * valleyMask);
                }
            }
        }
    }

    private float RidgeMask(float x, float center, float width)
    {
        float d = Mathf.Abs(x - center) / Mathf.Max(width, 0.0001f);
        return Mathf.Pow(1f - Mathf.Clamp01(d), 1.85f);
    }

    private void PaintTerrainLayers(TerrainData terrainData)
    {
        TerrainLayer[] layers = new TerrainLayer[5];

        layers[GrassLayer] = CreateTerrainLayer(mossGrassTexture, new Color(0.26f, 0.33f, 0.22f), grassTileSize, "Generated_Moss_Grass");
        layers[RockLayer] = CreateTerrainLayer(rockTexture, new Color(0.36f, 0.38f, 0.37f), rockTileSize, "Generated_Faceted_Rock");
        layers[DirtLayer] = CreateTerrainLayer(dirtPathTexture, new Color(0.36f, 0.30f, 0.23f), dirtTileSize, "Generated_Damp_Dirt_Path");
        layers[DarkSoilLayer] = CreateTerrainLayer(darkSoilTexture, new Color(0.20f, 0.21f, 0.19f), darkSoilTileSize, "Generated_Dark_Soil");
        layers[GravelLayer] = CreateTerrainLayer(gravelTexture, new Color(0.31f, 0.32f, 0.30f), gravelTileSize, "Generated_Gravel");

        terrainData.terrainLayers = layers;

        int alphaW = terrainData.alphamapWidth;
        int alphaH = terrainData.alphamapHeight;
        float[,,] alpha = new float[alphaH, alphaW, layers.Length];

        for (int z = 0; z < alphaH; z++)
        {
            float nz = z / (float)(alphaH - 1);

            for (int x = 0; x < alphaW; x++)
            {
                float nx = x / (float)(alphaW - 1);

                float height = SampleGeneratedHeight01(nx, nz);
                float slope = EstimateSlope01(nx, nz);

                float pathDist = DistanceToPath01(new Vector2(nx, nz));
                float pathMask = 1f - Smooth01(pathDist / (pathWidth + pathTextureSoftness));

                float clearingDist = Vector2.Distance(new Vector2(nx, nz), clearingCenter);
                float clearingMask = 1f - Smooth01(clearingDist / (clearingRadius + clearingEdgeBand));
                float clearingEdgeMask = Mathf.Clamp01(1f - Mathf.Abs(clearingDist - clearingRadius) / Mathf.Max(clearingEdgeBand, 0.001f));

                float outcropDist = Vector2.Distance(new Vector2(nx, nz), rockyOutcropCenter);
                float outcropMask = 1f - Smooth01(outcropDist / rockyOutcropRadius);

                float highRock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(highAltitudeRockStart, 0.86f, height));
                float steepRock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(rockSlopeStart, rockSlopeFull, slope));
                float rock = Mathf.Clamp01(Mathf.Max(steepRock, highRock) + outcropMask * 0.45f);

                float dirt = Mathf.Clamp01(pathMask * 1.4f);
                float gravel = Mathf.Clamp01(clearingEdgeMask * gravelAroundCampStrength + outcropMask * 0.35f);
                float darkSoil = Mathf.Clamp01((1f - height) * darkSoilLowAreaStrength + clearingMask * 0.14f);
                float grass = Mathf.Clamp01(1f - rock * 0.78f - dirt * 0.85f + clearingMask * 0.18f);

                alpha[z, x, GrassLayer] = grass;
                alpha[z, x, RockLayer] = rock;
                alpha[z, x, DirtLayer] = dirt;
                alpha[z, x, DarkSoilLayer] = darkSoil;
                alpha[z, x, GravelLayer] = gravel;

                NormalizeSplat(alpha, z, x, layers.Length);
            }
        }

        terrainData.SetAlphamaps(0, 0, alpha);
    }

    private TerrainLayer CreateTerrainLayer(Texture2D texture, Color fallbackColor, Vector2 tileSize, string layerName)
    {
        TerrainLayer layer = new TerrainLayer();
        layer.name = layerName;
        layer.diffuseTexture = texture != null ? texture : CreateFallbackTexture(fallbackColor);
        layer.tileSize = tileSize;
        layer.tileOffset = Vector2.zero;
        return layer;
    }

    private Texture2D CreateFallbackTexture(Color color)
    {
        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.name = "Runtime_Fallback_Terrain_Texture";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;

        Color[] colors = new Color[16];

        for (int i = 0; i < colors.Length; i++)
        {
            float noise = ((i % 4) + (i / 4)) % 2 == 0 ? 0.92f : 1.08f;
            colors[i] = color * noise;
            colors[i].a = 1f;
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    private void NormalizeSplat(float[,,] alpha, int z, int x, int layerCount)
    {
        float sum = 0f;

        for (int i = 0; i < layerCount; i++)
            sum += alpha[z, x, i];

        if (sum <= 0.0001f)
        {
            alpha[z, x, GrassLayer] = 1f;
            return;
        }

        for (int i = 0; i < layerCount; i++)
            alpha[z, x, i] /= sum;
    }

    private void ScatterUnderbrushDetails(TerrainData terrainData)
    {
        if (underbrushTexture == null || underbrushDensity <= 0f)
        {
            terrainData.detailPrototypes = Array.Empty<DetailPrototype>();
            return;
        }

        DetailPrototype detail = new DetailPrototype();
        detail.prototypeTexture = underbrushTexture;
        detail.renderMode = DetailRenderMode.GrassBillboard;
        detail.healthyColor = underbrushHealthyColor;
        detail.dryColor = underbrushDryColor;
        detail.minWidth = 0.45f;
        detail.maxWidth = 0.95f;
        detail.minHeight = 0.35f;
        detail.maxHeight = 0.90f;
        detail.noiseSpread = 0.65f;

        terrainData.detailPrototypes = new[] { detail };
        terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);

        int[,] detailMap = new int[detailResolution, detailResolution];

        for (int z = 0; z < detailResolution; z++)
        {
            float nz = z / (float)(detailResolution - 1);

            for (int x = 0; x < detailResolution; x++)
            {
                float nx = x / (float)(detailResolution - 1);

                float height = SampleGeneratedHeight01(nx, nz);
                float slope = EstimateSlope01(nx, nz);
                float pathDist = DistanceToPath01(new Vector2(nx, nz));
                float clearingDist = Vector2.Distance(new Vector2(nx, nz), clearingCenter);

                bool valley = height < 0.28f && slope < 0.28f;
                bool awayFromPath = pathDist > pathWidth * 1.8f;
                bool notCenterOfClearing = clearingDist > clearingRadius * 0.22f;

                if (!valley || !awayFromPath || !notCenterOfClearing)
                {
                    detailMap[z, x] = 0;
                    continue;
                }

                float noise = Fbm(nx * 28f + seed, nz * 28f + seed, 3, 0.5f, 2f);
                float chance = underbrushDensity * Mathf.Lerp(0.25f, 1f, noise);

                detailMap[z, x] = rng.NextDouble() < chance ? rng.Next(1, maxUnderbrushPerPatch + 1) : 0;
            }
        }

        terrainData.SetDetailLayer(0, 0, 0, detailMap);
    }

    private void ScatterTerrainTrees(TerrainData terrainData)
    {
        if (pineTreePrefabs == null || pineTreePrefabs.Length == 0 || treeDensity <= 0f)
        {
            terrainData.treePrototypes = Array.Empty<TreePrototype>();
            terrainData.treeInstances = Array.Empty<TreeInstance>();
            return;
        }

        List<TreePrototype> prototypes = new List<TreePrototype>();

        for (int i = 0; i < pineTreePrefabs.Length; i++)
        {
            if (pineTreePrefabs[i] == null)
                continue;

            TreePrototype prototype = new TreePrototype();
            prototype.prefab = pineTreePrefabs[i];
            prototype.bendFactor = 0.08f;
            prototypes.Add(prototype);
        }

        if (prototypes.Count == 0)
            return;

        terrainData.treePrototypes = prototypes.ToArray();

        List<TreeInstance> trees = new List<TreeInstance>();
        List<Vector2> accepted = new List<Vector2>();

        int attempts = Mathf.Max(250, treeCandidateCount);

        for (int i = 0; i < attempts; i++)
        {
            Vector2 p = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());

            float height = SampleGeneratedHeight01(p.x, p.y);
            float slope = EstimateSlope01(p.x, p.y);

            if (slope < minTreeSlope || slope > maxTreeSlope)
                continue;

            if (DistanceToPathWorld(p) < pathTreeExclusionDistance)
                continue;

            if (Vector2.Distance(p, clearingCenter) * terrainWidth < clearingTreeExclusionDistance)
                continue;

            if (Vector2.Distance(p, rockyOutcropCenter) * terrainWidth < 25f)
                continue;

            float slopePreference = Mathf.InverseLerp(minTreeSlope, maxTreeSlope, slope);
            float ridgePreference = Mathf.Clamp01(RidgeMask(p.x, leftRidgeX, ridgeWidth * 1.8f) + RidgeMask(p.x, rightRidgeX, ridgeWidth * 1.8f));
            float valleyPreference = 1f - Mathf.Clamp01(Mathf.Abs(p.x - GetPathCenterX(p.y)) / 0.42f);

            float chance = treeDensity * Mathf.Clamp01(0.20f + slopePreference * 0.45f + ridgePreference * 0.45f + valleyPreference * 0.20f);

            if (rng.NextDouble() > chance)
                continue;

            bool tooClose = false;
            float minDist01 = minTreeDistance / terrainWidth;

            for (int k = 0; k < accepted.Count; k++)
            {
                if (Vector2.Distance(accepted[k], p) < minDist01)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            accepted.Add(p);

            TreeInstance tree = new TreeInstance();
            tree.position = new Vector3(p.x, height, p.y);
            tree.prototypeIndex = rng.Next(0, prototypes.Count);
            tree.heightScale = Mathf.Lerp(treeHeightScale.x, treeHeightScale.y, (float)rng.NextDouble());
            tree.widthScale = Mathf.Lerp(treeWidthScale.x, treeWidthScale.y, (float)rng.NextDouble());
            tree.color = Color.Lerp(new Color(0.22f, 0.28f, 0.20f), new Color(0.34f, 0.40f, 0.28f), (float)rng.NextDouble());
            tree.lightmapColor = Color.white;

            trees.Add(tree);
        }

        terrainData.treeInstances = trees.ToArray();
    }

    private void ScatterProps()
    {
        Transform root = GetGeneratedRoot();
        List<Vector3> placed = new List<Vector3>();

        ScatterPrefabGroup(cratePrefabs, crateCount, crateMinSpacing, PlacementMode.PathAndClearingEdge, placed, root, "Crates");
        ScatterPrefabGroup(barrelPrefabs, barrelCount, barrelMinSpacing, PlacementMode.PathAndClearingEdge, placed, root, "Barrels");
        ScatterPrefabGroup(fencePrefabs, fenceCount, fenceMinSpacing, PlacementMode.ClearingEdge, placed, root, "Fences");
        ScatterPrefabGroup(caveEntrancePrefabs, caveEntranceCount, caveMinSpacing, PlacementMode.RockyOutcrop, placed, root, "CaveMarkers");
        ScatterPrefabGroup(bridgeMarkerPrefabs, bridgeMarkerCount, bridgeMinSpacing, PlacementMode.RavineEdge, placed, root, "BridgeMarkers");
    }

    private enum PlacementMode
    {
        PathAndClearingEdge,
        ClearingEdge,
        RockyOutcrop,
        RavineEdge
    }

    private void ScatterPrefabGroup(
        GameObject[] prefabs,
        int count,
        float minSpacing,
        PlacementMode mode,
        List<Vector3> placed,
        Transform root,
        string parentName)
    {
        if (prefabs == null || prefabs.Length == 0 || count <= 0)
            return;

        Transform parent = CreateChildParent(root, parentName);

        int placedCount = 0;
        int attempts = count * 80;

        for (int i = 0; i < attempts && placedCount < count; i++)
        {
            Vector2 normalPosition = GetCandidatePosition(mode);

            if (!IsInsideMap(normalPosition))
                continue;

            float slopeDegrees = EstimateSlope01(normalPosition.x, normalPosition.y) * 90f;

            if (slopeDegrees > maxPropSlopeDegrees)
                continue;

            Vector3 world = NormalToWorld(normalPosition);

            bool tooClose = false;

            for (int k = 0; k < placed.Count; k++)
            {
                if (Vector3.Distance(placed[k], world) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            GameObject prefab = PickPrefab(prefabs);

            if (prefab == null)
                continue;

            GameObject instance = InstantiatePrefabSafe(prefab, parent);
            instance.name = $"{parentName}_{placedCount + 1:00}";
            instance.transform.position = world;

            float yaw = (float)rng.NextDouble() * 360f;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            float scale = Mathf.Lerp(propScaleRange.x, propScaleRange.y, (float)rng.NextDouble());
            instance.transform.localScale = Vector3.one * scale;

            EnsurePropColliders(instance);

            placed.Add(world);
            placedCount++;
        }
    }

    private Vector2 GetCandidatePosition(PlacementMode mode)
    {
        switch (mode)
        {
            case PlacementMode.PathAndClearingEdge:
            {
                bool choosePath = rng.NextDouble() < 0.62f;

                if (choosePath)
                {
                    float t = Mathf.Lerp(0.08f, 0.92f, (float)rng.NextDouble());
                    float pathX = GetPathCenterX(t);

                    float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                    float offsetWorld = Mathf.Lerp(propPathOffsetMin, propPathOffsetMax, (float)rng.NextDouble()) * side;
                    float offset01 = offsetWorld / terrainWidth;

                    return new Vector2(pathX + offset01, t);
                }

                return RandomPointOnClearingEdge();
            }

            case PlacementMode.ClearingEdge:
                return RandomPointOnClearingEdge();

            case PlacementMode.RockyOutcrop:
            {
                Vector2 random = RandomInsideUnitCircle();
                random.Normalize();
                random *= rockyOutcropRadius * Mathf.Lerp(0.45f, 0.92f, (float)rng.NextDouble());
                return rockyOutcropCenter + random;
            }

            case PlacementMode.RavineEdge:
            {
                float z = Mathf.Lerp(0.30f, 0.74f, (float)rng.NextDouble());
                float x = Mathf.Lerp(0.84f, 0.91f, (float)rng.NextDouble());
                return new Vector2(x, z);
            }
        }

        return clearingCenter;
    }

    private Vector2 RandomPointOnClearingEdge()
    {
        float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)rng.NextDouble());
        float radiusWorld = clearingRadius * terrainWidth + Mathf.Lerp(-propClearingEdgeOffset, propClearingEdgeOffset, (float)rng.NextDouble());
        float radius01 = radiusWorld / terrainWidth;

        return clearingCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius01;
    }

    private Vector2 RandomInsideUnitCircle()
    {
        float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)rng.NextDouble());
        float radius = Mathf.Sqrt((float)rng.NextDouble());
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private GameObject PickPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
            return null;

        for (int i = 0; i < 20; i++)
        {
            GameObject prefab = prefabs[rng.Next(0, prefabs.Length)];

            if (prefab != null)
                return prefab;
        }

        return null;
    }

    private GameObject InstantiatePrefabSafe(GameObject prefab, Transform parent)
    {
        GameObject instance;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance != null)
                return instance;
        }
#endif

        instance = Instantiate(prefab, parent);
        return instance;
    }

    private void EnsurePropColliders(GameObject instance)
    {
        if (instance.GetComponentInChildren<Collider>() != null)
            return;

        MeshRenderer renderer = instance.GetComponentInChildren<MeshRenderer>();

        if (renderer != null)
        {
            BoxCollider box = instance.AddComponent<BoxCollider>();
            box.center = instance.transform.InverseTransformPoint(renderer.bounds.center);
            box.size = renderer.bounds.size;
            return;
        }

        instance.AddComponent<BoxCollider>();
    }

    private Transform CreateChildParent(Transform root, string childName)
    {
        Transform child = root.Find(childName);

        if (child != null)
            return child;

        GameObject obj = new GameObject(childName);
        obj.transform.SetParent(root);
        return obj.transform;
    }

    private void PlaceSpecialProps()
    {
        Transform root = GetGeneratedRoot();

        PlaceSingleSpecial(watchtowerPrefabs, watchtowerPosition, root, "Watchtower_Main", 1.2f, 0f);
        PlaceSingleSpecial(caveEntrancePrefabs, mainCavePosition, root, "Cave_Left_Main", 1.25f, 25f);
        PlaceSingleSpecial(caveEntrancePrefabs, rightCavePosition, root, "Cave_Right_Main", 1.15f, -18f);

        CreateSpawnMarker("PlayerSpawn", new Vector2(0.42f, 0.20f), root, Color.green);
        CreateSpawnMarker("LargeEnemySpawn", new Vector2(0.55f, 0.72f), root, Color.red);

        for (int i = 0; i < 8; i++)
        {
            float t = Mathf.Lerp(0.12f, 0.88f, i / 7f);
            Vector2 p = new Vector2(GetPathCenterX(t), t);
            CreateSpawnMarker($"EnemyPatrolPoint_{i + 1:00}", p, root, new Color(1f, 0.55f, 0.1f));
        }
    }

    private void PlaceSingleSpecial(GameObject[] prefabs, Vector2 normalizedPosition, Transform root, string name, float scale, float yaw)
    {
        GameObject prefab = PickPrefab(prefabs);

        if (prefab == null)
            return;

        GameObject instance = InstantiatePrefabSafe(prefab, root);
        instance.name = name;
        instance.transform.position = NormalToWorld(normalizedPosition);
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        instance.transform.localScale = Vector3.one * scale;

        EnsurePropColliders(instance);
    }

    private void CreateSpawnMarker(string name, Vector2 normalizedPosition, Transform root, Color gizmoColor)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(root);
        marker.transform.position = NormalToWorld(normalizedPosition) + Vector3.up * 0.5f;

        TerrainValleyMarker markerComponent = marker.AddComponent<TerrainValleyMarker>();
        markerComponent.gizmoColor = gizmoColor;
        markerComponent.gizmoRadius = name.Contains("LargeEnemy") ? 3.5f : 1.5f;
    }

    private void ApplyAtmosphereSettings()
    {
        if (!applyAtmosphere)
            return;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;

        Light directional = FindDirectionalLight();

        if (directional == null)
        {
            GameObject lightObject = new GameObject("Generated_Overcast_Directional_Light");
            directional = lightObject.AddComponent<Light>();
            directional.type = LightType.Directional;
        }

        directional.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
        directional.color = directionalLightColor;
        directional.intensity = directionalLightIntensity;
        directional.shadows = LightShadows.Soft;
    }

    private Light FindDirectionalLight()
{
    Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

    for (int i = 0; i < lights.Length; i++)
    {
        if (lights[i].type == LightType.Directional)
            return lights[i];
    }

    return null;
}

    private List<Vector2> BuildPathPoints()
    {
        List<Vector2> points = new List<Vector2>(cachedPathPointCount);

        for (int i = 0; i < cachedPathPointCount; i++)
        {
            float t = i / (float)(cachedPathPointCount - 1);
            points.Add(new Vector2(GetPathCenterX(t), t));
        }

        return points;
    }

    private float GetPathCenterX(float z01)
    {
        float waveA = Mathf.Sin(z01 * Mathf.PI * 2.15f + 0.45f) * pathWindingAmount;
        float waveB = Mathf.Sin(z01 * Mathf.PI * 5.10f + 1.20f) * pathWindingAmount * 0.34f;
        float valleyPull = Mathf.Sin(z01 * Mathf.PI) * -0.025f;

        return Mathf.Clamp01(0.50f + waveA + waveB + valleyPull);
    }

    private float DistanceToPath01(Vector2 p)
    {
        if (cachedPathPoints == null || cachedPathPoints.Count < 2)
            cachedPathPoints = BuildPathPoints();

        float best = float.MaxValue;

        for (int i = 0; i < cachedPathPoints.Count - 1; i++)
        {
            float d = DistancePointSegment(p, cachedPathPoints[i], cachedPathPoints[i + 1]);
            if (d < best)
                best = d;
        }

        return best;
    }

    private float DistanceToPathWorld(Vector2 p)
    {
        return DistanceToPath01(p) * terrainWidth;
    }

    private float DistancePointSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.00001f);
        t = Mathf.Clamp01(t);
        return Vector2.Distance(p, a + ab * t);
    }

    private Vector3 NormalToWorld(Vector2 normalized)
    {
        normalized.x = Mathf.Clamp01(normalized.x);
        normalized.y = Mathf.Clamp01(normalized.y);

        float x = normalized.x * terrainWidth;
        float z = normalized.y * terrainLength;
        float y = 0f;

        if (targetTerrain != null)
            y = targetTerrain.SampleHeight(new Vector3(x, 0f, z)) + targetTerrain.transform.position.y;
        else
            y = SampleGeneratedHeight01(normalized.x, normalized.y) * terrainHeight;

        return new Vector3(x, y, z);
    }

    private bool IsInsideMap(Vector2 p)
    {
        return p.x > 0.03f && p.x < 0.97f && p.y > 0.03f && p.y < 0.97f;
    }

    private float SampleGeneratedHeight01(float nx, float nz)
    {
        if (generatedHeights == null)
            return 0f;

        int resZ = generatedHeights.GetLength(0);
        int resX = generatedHeights.GetLength(1);

        float x = Mathf.Clamp01(nx) * (resX - 1);
        float z = Mathf.Clamp01(nz) * (resZ - 1);

        int x0 = Mathf.FloorToInt(x);
        int z0 = Mathf.FloorToInt(z);
        int x1 = Mathf.Min(x0 + 1, resX - 1);
        int z1 = Mathf.Min(z0 + 1, resZ - 1);

        float tx = x - x0;
        float tz = z - z0;

        float a = Mathf.Lerp(generatedHeights[z0, x0], generatedHeights[z0, x1], tx);
        float b = Mathf.Lerp(generatedHeights[z1, x0], generatedHeights[z1, x1], tx);

        return Mathf.Lerp(a, b, tz);
    }

    private float EstimateSlope01(float nx, float nz)
    {
        float stepX = 1f / Mathf.Max(terrainWidth, 1);
        float stepZ = 1f / Mathf.Max(terrainLength, 1);

        float hL = SampleGeneratedHeight01(nx - stepX, nz) * terrainHeight;
        float hR = SampleGeneratedHeight01(nx + stepX, nz) * terrainHeight;
        float hD = SampleGeneratedHeight01(nx, nz - stepZ) * terrainHeight;
        float hU = SampleGeneratedHeight01(nx, nz + stepZ) * terrainHeight;

        float dx = (hR - hL) * 0.5f;
        float dz = (hU - hD) * 0.5f;

        float slopeRadians = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz));
        float slopeDegrees = slopeRadians * Mathf.Rad2Deg;

        return Mathf.Clamp01(slopeDegrees / 90f);
    }

    private float Fbm(float x, float z, int octaves, float persistence, float lacunarity)
    {
        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float max = 0f;

        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
            max += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return max > 0f ? total / max : 0f;
    }

    private float RidgedFbm(float x, float z, int octaves)
    {
        float total = 0f;
        float amplitude = 0.5f;
        float frequency = 1f;
        float max = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float n = Mathf.PerlinNoise(x * frequency, z * frequency);
            n = 1f - Mathf.Abs(n * 2f - 1f);
            n *= n;

            total += n * amplitude;
            max += amplitude;

            amplitude *= 0.5f;
            frequency *= 2.05f;
        }

        return max > 0f ? total / max : 0f;
    }

    private float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private void OnDrawGizmosSelected()
    {
        if (drawPathGizmos)
        {
            Gizmos.color = Color.yellow;

            List<Vector2> points = cachedPathPoints;
            if (points == null || points.Count < 2)
                points = BuildPathPoints();

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 a = new Vector3(points[i].x * terrainWidth, 2f, points[i].y * terrainLength);
                Vector3 b = new Vector3(points[i + 1].x * terrainWidth, 2f, points[i + 1].y * terrainLength);
                Gizmos.DrawLine(a, b);
            }
        }

        if (drawClearingGizmos)
        {
            Gizmos.color = new Color(0f, 1f, 0.3f, 0.35f);
            Vector3 c = new Vector3(clearingCenter.x * terrainWidth, 3f, clearingCenter.y * terrainLength);
            Gizmos.DrawWireSphere(c, clearingRadius * terrainWidth);
        }
    }
}