using UnityEngine;

public class MaterialManager : MonoBehaviour
{
    [Header("Base Materials")]
    [SerializeField] private Material grassMaterial;
    [SerializeField] private Material rockMaterial;
    [SerializeField] private Material snowMaterial;

    [Header("Material Settings")]
    [SerializeField] private float tiling = 50f;
    [SerializeField] private TerrainLayer[] terrainLayers;

    void Start()
    {
        CreateDefaultMaterials();
        SetupTerrainLayers();
    }

    private void CreateDefaultMaterials()
    {
        // Create default materials if none are assigned
        if (grassMaterial == null)
        {
            grassMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            grassMaterial.name = "Grass Material";
            grassMaterial.color = new Color(0.2f, 0.5f, 0.2f); // Green
        }

        if (rockMaterial == null)
        {
            rockMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            rockMaterial.name = "Rock Material";
            rockMaterial.color = new Color(0.5f, 0.5f, 0.5f); // Gray
        }

        if (snowMaterial == null)
        {
            snowMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            snowMaterial.name = "Snow Material";
            snowMaterial.color = new Color(0.95f, 0.95f, 0.95f); // White
        }
    }

    private void SetupTerrainLayers()
    {
        Terrain terrain = FindObjectOfType<Terrain>();
        if (terrain == null)
        {
            Debug.LogWarning("No terrain found in scene!");
            return;
        }

        // Create terrain layers
        terrainLayers = new TerrainLayer[3];

        // Grass layer (base)
        terrainLayers[0] = new TerrainLayer();
        terrainLayers[0].diffuseTexture = CreateDefaultTexture(grassMaterial.color);
        terrainLayers[0].tileSize = new Vector2(tiling, tiling);
        terrainLayers[0].diffuseRemapMax = Vector4.one;
        terrainLayers[0].name = "Grass Layer";

        // Rock layer (slopes)
        terrainLayers[1] = new TerrainLayer();
        terrainLayers[1].diffuseTexture = CreateDefaultTexture(rockMaterial.color);
        terrainLayers[1].tileSize = new Vector2(tiling, tiling);
        terrainLayers[1].diffuseRemapMax = Vector4.one;
        terrainLayers[1].name = "Rock Layer";

        // Snow layer (peaks)
        terrainLayers[2] = new TerrainLayer();
        terrainLayers[2].diffuseTexture = CreateDefaultTexture(snowMaterial.color);
        terrainLayers[2].tileSize = new Vector2(tiling, tiling);
        terrainLayers[2].diffuseRemapMax = Vector4.one;
        terrainLayers[2].name = "Snow Layer";

        // Apply layers to terrain
        terrain.terrainData.terrainLayers = terrainLayers;
    }

    private Texture2D CreateDefaultTexture(Color color)
    {
        // Create a simple 2x2 texture
        Texture2D texture = new Texture2D(2, 2);
        Color[] colors = new Color[4] { color, color, color, color };
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    // Helper method to get terrain layers
    public TerrainLayer[] GetTerrainLayers()
    {
        return terrainLayers;
    }

    // Helper method to update tiling
    public void UpdateTiling(float newTiling)
    {
        tiling = newTiling;
        if (terrainLayers != null)
        {
            foreach (var layer in terrainLayers)
            {
                if (layer != null)
                {
                    layer.tileSize = new Vector2(tiling, tiling);
                }
            }
        }
    }
}
