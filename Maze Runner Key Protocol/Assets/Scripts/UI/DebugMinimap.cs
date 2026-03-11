using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Netcode;

/// <summary>
/// Dev tool: renders a top-down minimap of the maze with player position icons.
/// Toggle with Tab key. Shows maze walls, player dots (colored), and exit marker.
/// </summary>
public class DebugMinimap : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int mapSize = 250;
    [SerializeField] private int pixelsPerCell = 10;
    // Toggle with Tab key (new Input System)

    private Canvas minimapCanvas;
    private RawImage mapImage;
    private Texture2D mapTexture;
    private Color[] basePixels; // cached maze layout
    private RectTransform mapRect;
    private bool isVisible = true;

    // Cached maze data
    private MazeGrid grid;
    private float cellSize;
    private int gridWidth;
    private int gridHeight;
    private bool mazeReady;

    private static DebugMinimap instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        MazeGenerator.OnMazeReady += () =>
        {
            if (instance == null)
            {
                var go = new GameObject("DebugMinimap");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<DebugMinimap>();
            }
        };
    }

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        MazeGenerator.OnMazeReady += OnMazeReady;
        if (MazeGenerator.IsReady)
            OnMazeReady();
    }

    private void OnDisable()
    {
        MazeGenerator.OnMazeReady -= OnMazeReady;
    }

    private void OnMazeReady()
    {
        var gen = MazeGenerator.Instance;
        if (gen == null || gen.Grid == null) return;

        grid = gen.Grid;
        gridWidth = grid.Width;
        gridHeight = grid.Height;
        // MazeGenerator.cellSize=4: walls at x=c*4, cell centers at c*4+2
        // World x ranges from 0 to gridWidth*4. Texture pixel = (worldX / 4) * pixelsPerCell.
        cellSize = (gridWidth > 0) ? (MazeGenerator.MaxX + MazeGenerator.MinX) / gridWidth : 4f;

        CreateMinimapUI();
        RenderMazeTexture();
        mazeReady = true;
    }

    private void CreateMinimapUI()
    {
        if (minimapCanvas != null) return;

        // Canvas
        var canvasGo = new GameObject("DebugMinimapCanvas");
        canvasGo.transform.SetParent(transform, false);
        minimapCanvas = canvasGo.AddComponent<Canvas>();
        minimapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        minimapCanvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>();

        // Background panel
        var panelGo = new GameObject("MinimapPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-10, -10);
        panelRect.sizeDelta = new Vector2(mapSize + 10, mapSize + 10);
        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.7f);

        // Map image
        var imgGo = new GameObject("MapImage");
        imgGo.transform.SetParent(panelGo.transform, false);
        mapRect = imgGo.AddComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.anchoredPosition = Vector2.zero;
        mapRect.sizeDelta = new Vector2(mapSize, mapSize);
        mapImage = imgGo.AddComponent<RawImage>();
    }

    private void RenderMazeTexture()
    {
        int texW = gridWidth * pixelsPerCell + pixelsPerCell;
        int texH = gridHeight * pixelsPerCell + pixelsPerCell;
        mapTexture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        mapTexture.filterMode = FilterMode.Point;

        // Fill with floor color
        var floorColor = new Color(0.2f, 0.2f, 0.25f);
        var wallColor = new Color(0.7f, 0.7f, 0.7f);
        var pixels = new Color[texW * texH];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = floorColor;

        // Draw horizontal walls
        for (int r = 0; r <= gridHeight; r++)
        {
            for (int c = 0; c < gridWidth; c++)
            {
                if (!grid.HorizontalWalls[r, c]) continue;
                int py = r * pixelsPerCell;
                for (int px = c * pixelsPerCell; px < (c + 1) * pixelsPerCell + 1; px++)
                {
                    if (px >= 0 && px < texW && py >= 0 && py < texH)
                        pixels[py * texW + px] = wallColor;
                }
            }
        }

        // Draw vertical walls
        for (int r = 0; r < gridHeight; r++)
        {
            for (int c = 0; c <= gridWidth; c++)
            {
                if (!grid.VerticalWalls[r, c]) continue;
                int px = c * pixelsPerCell;
                for (int py = r * pixelsPerCell; py < (r + 1) * pixelsPerCell + 1; py++)
                {
                    if (px >= 0 && px < texW && py >= 0 && py < texH)
                        pixels[py * texW + px] = wallColor;
                }
            }
        }

        mapTexture.SetPixels(pixels);
        basePixels = (Color[])pixels.Clone();
        mapTexture.Apply();
        mapImage.texture = mapTexture;
    }

    private void Update()
    {
        // Toggle minimap with Tab (new Input System)
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            isVisible = !isVisible;
            if (minimapCanvas != null)
                minimapCanvas.gameObject.SetActive(isVisible);
        }

        if (!mazeReady || !isVisible || mapTexture == null || basePixels == null) return;

        // Restore cached base, overlay dynamic elements
        mapTexture.SetPixels(basePixels);
        DrawPlayers();
        DrawExit();
        mapTexture.Apply();
    }

    private float playerDebugTimer;

    private void DrawPlayers()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;

        int playerCount = 0;
        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            var netObj = kvp.Value;
            if (netObj == null) continue;
            var health = netObj.GetComponent<PlayerHealth>();
            if (health == null) continue;

            playerCount++;
            var rpv = netObj.GetComponent<RemotePlayerVisuals>();
            int playerIndex = rpv != null ? rpv.GetPlayerIndex() : 0;
            Color dotColor = RemotePlayerVisuals.PlayerColors[playerIndex % RemotePlayerVisuals.PlayerColors.Length];

            if (netObj.IsOwner)
                dotColor = Color.white;

            Vector3 pos = netObj.transform.position;
            DrawDot(pos, dotColor, netObj.IsOwner ? 5 : 4);
        }

        // Debug: log once to verify player detection
        playerDebugTimer += Time.deltaTime;
        if (playerDebugTimer >= 5f)
        {
            playerDebugTimer = 0f;
            Debug.Log($"[DebugMinimap] Drawing {playerCount} players. TexSize={mapTexture.width}x{mapTexture.height}");
        }
    }

    private void DrawExit()
    {
        var exitGateway = ExitGateway.Instance;
        if (exitGateway == null) return;

        DrawDot(exitGateway.transform.position, new Color(1f, 0.85f, 0f), 5); // Gold
    }

    private void DrawDot(Vector3 worldPos, Color color, int radius)
    {
        // World coords: cells go from 0 to gridWidth*cellWorldSize
        // Texture coords: 0 to (gridWidth * pixelsPerCell + pixelsPerCell)
        // Each cell in world = cellWorldSize, in texture = pixelsPerCell
        float cellWorldSize = cellSize;
        int px = Mathf.RoundToInt((worldPos.x / cellWorldSize) * pixelsPerCell);
        int py = Mathf.RoundToInt((worldPos.z / cellWorldSize) * pixelsPerCell);

        int texW = mapTexture.width;
        int texH = mapTexture.height;

        // Draw filled circle
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx * dx + dy * dy > radius * radius) continue;
                int x = px + dx;
                int y = py + dy;
                if (x >= 0 && x < texW && y >= 0 && y < texH)
                    mapTexture.SetPixel(x, y, color);
            }
        }
    }
}
