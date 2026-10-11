using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class BlockBlastGame
{
    public class ShapePattern
    {
        public string name;
        public List<Vector2Int> points;
        public ShapePattern(string name, params Vector2Int[] pts)
        {
            this.name = name;
            this.points = new List<Vector2Int>(pts);
        }
    }

    private List<ShapePattern> shapeLibrary = new List<ShapePattern>();

    private void InitializeShapeLibrary()
    {
        shapeLibrary.Clear();
        shapeLibrary.Add(new ShapePattern("Dot_1x1", new Vector2Int(0, 0)));
        shapeLibrary.Add(new ShapePattern("Line_2_H", new Vector2Int(0, 0), new Vector2Int(1, 0)));
        shapeLibrary.Add(new ShapePattern("Line_2_V", new Vector2Int(0, 0), new Vector2Int(0, 1)));
        shapeLibrary.Add(new ShapePattern("Line_3_H", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)));
        shapeLibrary.Add(new ShapePattern("Line_3_V", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)));
        shapeLibrary.Add(new ShapePattern("Line_4_H", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0)));
        shapeLibrary.Add(new ShapePattern("Line_4_V", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3)));
        shapeLibrary.Add(new ShapePattern("Line_5_H", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0)));
        shapeLibrary.Add(new ShapePattern("Line_5_V", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4)));
        shapeLibrary.Add(new ShapePattern("Square_2x2", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)));
        shapeLibrary.Add(new ShapePattern("Square_3x3",
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
            new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)));
        shapeLibrary.Add(new ShapePattern("L_Small_1", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)));
        shapeLibrary.Add(new ShapePattern("L_Small_2", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1)));
        shapeLibrary.Add(new ShapePattern("L_Small_3", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1)));
        shapeLibrary.Add(new ShapePattern("L_Small_4", new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)));
        shapeLibrary.Add(new ShapePattern("L_Big_1", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)));
        shapeLibrary.Add(new ShapePattern("L_Big_2", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)));
        shapeLibrary.Add(new ShapePattern("L_Big_3", new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(1, 2), new Vector2Int(0, 2)));
        shapeLibrary.Add(new ShapePattern("L_Big_4", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2)));
        shapeLibrary.Add(new ShapePattern("T_1", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1)));
        shapeLibrary.Add(new ShapePattern("T_2", new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)));
        shapeLibrary.Add(new ShapePattern("T_3", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 1)));
        shapeLibrary.Add(new ShapePattern("T_4", new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(0, 1)));
        shapeLibrary.Add(new ShapePattern("Z_1", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1)));
        shapeLibrary.Add(new ShapePattern("S_1", new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)));
    }

    public void RespawnAllThreeShapes()
    {
        if (spawnSlots == null || spawnSlots.Length == 0) return;
        if (block1x1Sprites == null || block1x1Sprites.Length == 0)
        {
            Debug.LogWarning("[BlockBlastGame] Chưa gán danh sách block1x1Sprites trong Inspector!");
            return;
        }

        for (int i = 0; i < spawnSlots.Length; i++)
        {
            Transform slot = spawnSlots[i];
            if (slot == null) continue;

            for (int c = slot.childCount - 1; c >= 0; c--)
            {
                Transform child = slot.GetChild(c);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            ShapePattern pattern = shapeLibrary[Random.Range(0, shapeLibrary.Count)];
            Sprite blockSprite = block1x1Sprites[Random.Range(0, block1x1Sprites.Length)];
            CreateShapeInSlot(slot, pattern, blockSprite, i, i * 0.08f);
        }

        CheckGameOverState();
    }

    public void SpawnShapesInEmptySlots()
    {
        if (spawnSlots == null || spawnSlots.Length == 0) return;
        if (block1x1Sprites == null || block1x1Sprites.Length == 0)
        {
            Debug.LogWarning("[BlockBlastGame] Chưa gán danh sách block1x1Sprites trong Inspector!");
            return;
        }

        for (int i = 0; i < spawnSlots.Length; i++)
        {
            Transform slot = spawnSlots[i];
            if (slot == null || slot.childCount > 0) continue;

            ShapePattern pattern = shapeLibrary[Random.Range(0, shapeLibrary.Count)];
            Sprite blockSprite = block1x1Sprites[Random.Range(0, block1x1Sprites.Length)];

            CreateShapeInSlot(slot, pattern, blockSprite, i, i * 0.08f);
        }

        CheckGameOverState();
    }

    private void CreateShapeInSlot(Transform slot, ShapePattern pattern, Sprite sprite, int slotIndex, float delay)
    {
        GameObject shapeRoot = new GameObject("BlockShape", typeof(RectTransform), typeof(CanvasGroup));
        shapeRoot.transform.SetParent(slot, false);
        RectTransform rootRect = shapeRoot.GetComponent<RectTransform>();
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.localScale = Vector3.zero;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var pt in pattern.points)
        {
            if (pt.x < minX) minX = pt.x;
            if (pt.x > maxX) maxX = pt.x;
            if (pt.y < minY) minY = pt.y;
            if (pt.y > maxY) maxY = pt.y;
        }

        float centerCol = (minX + maxX) * 0.5f;
        float centerRow = (minY + maxY) * 0.5f;

        List<RectTransform> blockRects = new List<RectTransform>();

        foreach (var pt in pattern.points)
        {
            GameObject blockObj = new GameObject($"b_{pt.x}_{pt.y}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blockObj.transform.SetParent(shapeRoot.transform, false);

            RectTransform br = blockObj.GetComponent<RectTransform>();
            br.sizeDelta = new Vector2(cellSize - cellMargin, cellSize - cellMargin);
            br.anchoredPosition = new Vector2((pt.x - centerCol) * cellSize, -(pt.y - centerRow) * cellSize);

            Image img = blockObj.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = true;

            blockRects.Add(br);
        }

        DraggableBlock dragHandler = shapeRoot.AddComponent<DraggableBlock>();
        dragHandler.Setup(this, slot, slotIndex, pattern.points, sprite, blockRects, slotScale, dragScale, dragYOffset);

        StartCoroutine(PopInShapeRoutine(rootRect, slotScale, delay));
    }

    private IEnumerator PopInShapeRoutine(RectTransform target, float targetScale, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        float elapsed = 0f;
        float duration = 0.28f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Sin(t * Mathf.PI * 0.5f) * targetScale * 1.15f;
            if (t > 0.7f) scale = Mathf.Lerp(targetScale * 1.15f, targetScale, (t - 0.7f) / 0.3f);
            target.localScale = Vector3.one * scale;
            yield return null;
        }
        target.localScale = Vector3.one * targetScale;
    }
}
