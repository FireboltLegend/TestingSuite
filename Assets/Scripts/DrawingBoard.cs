using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

public class DrawingBoard : MonoBehaviour
{
    public RawImage drawingSurface;
    public Color drawColor = Color.red;
    public string wetCondition = "";
    public int brushSize = 5;

    private Texture2D texture;
    private RectTransform rectTransform;
    public ToolController toolController;

    private Vector2Int? lastDrawPos = null;
    private List<Vector2Int> drawnPoints = new List<Vector2Int>();
    private List<List<Vector2Int>> allShapes = new List<List<Vector2Int>>();
    private List<Vector2Int> currentShape = new List<Vector2Int>();

    void Start()
    {
        rectTransform = drawingSurface.GetComponent<RectTransform>();
        int width = (int)rectTransform.rect.width;
        int height = (int)rectTransform.rect.height;

        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        ClearBoard();

        drawingSurface.texture = texture;
    }

    void Update()
    {
        if (wetCondition == "Hot_Wet")
        {
            drawColor = Color.red;
        }
        else if (wetCondition == "Cold_Wet")
        {
            drawColor = Color.blue;
        }
        else
        {
            drawColor = Color.green;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out localPoint);

            Vector2Int texCoord = new Vector2Int(
                (int)(localPoint.x + rectTransform.rect.width / 2),
                (int)(localPoint.y + rectTransform.rect.height / 2)
            );

            if (lastDrawPos == null)
            {
                // New shape starting
                currentShape = new List<Vector2Int>();
                currentShape.Add(texCoord);
                DrawAt(texCoord.x, texCoord.y);
                lastDrawPos = texCoord;
            }
            else
            {
                DrawLine(lastDrawPos.Value, texCoord);
                currentShape.Add(texCoord);
                lastDrawPos = texCoord;
            }
        }
        else if (lastDrawPos != null)
        {
            // Mouse released — close and fill current shape
            if (currentShape.Count > 2)
            {
                DrawLine(currentShape[currentShape.Count - 1], currentShape[0]);  // Close shape
                FillShape(currentShape[0]);  // Fill starting from first point
                allShapes.Add(currentShape); // Store shape
            }

            lastDrawPos = null;
            currentShape = new List<Vector2Int>(); // Reset for next shape
        }
    }

    void FillShape(Vector2Int startPoint)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(startPoint);
        visited.Add(startPoint);

        while (queue.Count > 0)
        {
            Vector2Int point = queue.Dequeue();

            if (!IsInsideTexture(point)) continue;
            Color pixelColor = texture.GetPixel(point.x, point.y);

            if (pixelColor.a != 0) continue; // already drawn or edge

            texture.SetPixel(point.x, point.y, drawColor);

            // check 4 neighbors
            Vector2Int[] directions = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

            foreach (var dir in directions)
            {
                Vector2Int neighbor = point + dir;
                if (!visited.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        texture.Apply();
    }

    bool IsInsideTexture(Vector2Int point)
    {
        return point.x >= 0 && point.x < texture.width && point.y >= 0 && point.y < texture.height;
    }

    public void ClearBoard()
    {
        Color[] clearColorArray = new Color[texture.width * texture.height];
        for (int i = 0; i < clearColorArray.Length; ++i)
            clearColorArray[i] = new Color(0, 0, 0, 0); // fully transparent
        texture.SetPixels(clearColorArray);
        texture.Apply();
        drawnPoints.Clear();
        allShapes.Clear();
        currentShape.Clear();
    }

    void DrawAt(int x, int y)
    {
        for (int i = -brushSize; i <= brushSize; i++)
        {
            for (int j = -brushSize; j <= brushSize; j++)
            {
                int px = x + i;
                int py = y + j;
                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                {
                    texture.SetPixel(px, py, drawColor);
                }
            }
        }
    }

    void DrawLine(Vector2Int start, Vector2Int end)
    {
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);

        int sx = start.x < end.x ? 1 : -1;
        int sy = start.y < end.y ? 1 : -1;

        int err = dx - dy;
        int x = start.x;
        int y = start.y;

        while (true)
        {
            DrawAt(x, y);

            if (x == end.x && y == end.y) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
        texture.Apply();
    }

    public void SaveDrawing()
    {
        byte[] bytes = texture.EncodeToPNG();
        string timeNow = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string folderPath = Path.Combine(Application.persistentDataPath, "p" + toolController.participantNumber.ToString() + "_" + toolController.pathTime);
        string path = Path.Combine(folderPath, string.Format("{0}_{1}.png", toolController.saveString, timeNow));
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        File.WriteAllBytes(path, bytes);
        Debug.Log("Saved drawing to: " + path);

        ClearBoard();
    }

    public void ClearDrawing()
    {
        // Fill the texture with fully transparent pixels
        Color[] clearColors = new Color[texture.width * texture.height];
        for (int i = 0; i < clearColors.Length; i++)
        {
            clearColors[i] = new Color(0, 0, 0, 0);  // transparent
        }

        texture.SetPixels(clearColors);
        texture.Apply();
        drawnPoints.Clear();
        allShapes.Clear();
        currentShape.Clear();
    }
}