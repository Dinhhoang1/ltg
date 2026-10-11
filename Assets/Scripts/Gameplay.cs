using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class BlockBlastGame
{
    private List<Cell> currentPreviewCells = new List<Cell>();
    private List<Cell> currentLinePreviewCells = new List<Cell>();

    public void UpdateDragPreview(DraggableBlock shape)
    {
        List<Cell> targetCells = FindMatchingCellsForShape(shape);

        if (AreCellListsEqual(currentPreviewCells, targetCells)) return;

        ClearPreview();

        if (targetCells != null && targetCells.Count > 0)
        {
            currentPreviewCells = targetCells;
            foreach (var cell in currentPreviewCells)
            {
                cell.image.sprite = shape.BlockSprite;
                cell.image.color = new Color(1f, 1f, 1f, 0.45f);
            }

            List<Cell> predictedLines = PredictLinesIfPlaced(targetCells);
            if (predictedLines.Count > 0)
            {
                currentLinePreviewCells = predictedLines;
                foreach (var cell in currentLinePreviewCells)
                {
                    if (!cell.isOccupied)
                    {
                        cell.image.sprite = shape.BlockSprite;
                        cell.image.color = new Color(1f, 1f, 0.6f, 0.75f);
                    }
                    else
                    {
                        cell.image.color = new Color(1.3f, 1.3f, 1.3f, 1f);
                    }
                }
            }
        }
    }

    private List<Cell> PredictLinesIfPlaced(List<Cell> hypotheticalCells)
    {
        HashSet<Vector2Int> hypotheticalOccupied = new HashSet<Vector2Int>();
        foreach (var c in hypotheticalCells)
            hypotheticalOccupied.Add(new Vector2Int(c.col, c.row));

        List<Cell> lineCells = new List<Cell>();

        for (int r = 0; r < GridSize; r++)
        {
            bool willComplete = true;
            for (int c = 0; c < GridSize; c++)
            {
                if (!gridCells[r, c].isOccupied && !hypotheticalOccupied.Contains(new Vector2Int(c, r)))
                {
                    willComplete = false;
                    break;
                }
            }
            if (willComplete)
            {
                for (int c = 0; c < GridSize; c++) lineCells.Add(gridCells[r, c]);
            }
        }

        for (int c = 0; c < GridSize; c++)
        {
            bool willComplete = true;
            for (int r = 0; r < GridSize; r++)
            {
                if (!gridCells[r, c].isOccupied && !hypotheticalOccupied.Contains(new Vector2Int(c, r)))
                {
                    willComplete = false;
                    break;
                }
            }
            if (willComplete)
            {
                for (int r = 0; r < GridSize; r++) lineCells.Add(gridCells[r, c]);
            }
        }

        return lineCells;
    }

    public void ClearPreview()
    {
        if (currentLinePreviewCells.Count > 0)
        {
            foreach (var cell in currentLinePreviewCells)
            {
                if (cell.isOccupied)
                {
                    cell.image.color = Color.white;
                }
            }
            currentLinePreviewCells.Clear();
        }

        if (currentPreviewCells.Count > 0)
        {
            foreach (var cell in currentPreviewCells)
            {
                if (!cell.isOccupied)
                {
                    cell.image.sprite = emptyCellSprite;
                    cell.image.color = emptyCellColor;
                }
            }
            currentPreviewCells.Clear();
        }
    }

    private bool AreCellListsEqual(List<Cell> a, List<Cell> b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
        return true;
    }

    public List<Cell> FindMatchingCellsForShape(DraggableBlock shape)
    {
        List<Cell> matched = new List<Cell>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        foreach (var block in shape.BlockRects)
        {
            Vector3 worldPos = block.position;
            Vector3 localInBoard = boardContainer.InverseTransformPoint(worldPos);

            int col = Mathf.RoundToInt((localInBoard.x - topLeftX) / cellSize - 0.5f);
            int row = Mathf.RoundToInt((topLeftY - localInBoard.y) / cellSize - 0.5f);

            if (row < 0 || row >= GridSize || col < 0 || col >= GridSize) return null;

            Vector2Int coord = new Vector2Int(col, row);
            if (visited.Contains(coord)) return null;

            Vector2 expectedLocal = GetCellLocalPos(row, col);
            if (Vector2.Distance(new Vector2(localInBoard.x, localInBoard.y), expectedLocal) > cellSize * 0.58f)
                return null;

            Cell cell = gridCells[row, col];
            if (cell.isOccupied) return null;

            visited.Add(coord);
            matched.Add(cell);
        }

        return matched;
    }

    public bool TryPlaceShape(DraggableBlock shape)
    {
        ClearPreview();
        List<Cell> targetCells = FindMatchingCellsForShape(shape);

        if (targetCells == null || targetCells.Count == 0)
            return false;

        foreach (var cell in targetCells)
        {
            cell.isOccupied = true;
            cell.image.sprite = shape.BlockSprite;
            cell.image.color = Color.white;
            StartCoroutine(PunchScaleCellRoutine(cell.rect, 1.25f, 0.12f));
        }

        PlaySound(placeSound, placeSoundVolume);
        TriggerBoardShake(0.08f, 3.5f);

        shape.IsPlaced = true;
        shape.gameObject.SetActive(false);
        Destroy(shape.gameObject);
        StartCoroutine(CheckAndClearLinesRoutine(targetCells));
        return true;
    }

    private IEnumerator PunchScaleCellRoutine(RectTransform target, float maxScale, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float s = Mathf.Lerp(maxScale, 1.0f, t);
            target.localScale = Vector3.one * s;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    private IEnumerator CheckAndClearLinesRoutine(List<Cell> placedCells)
    {
        List<int> fullRows = new List<int>();
        List<int> fullCols = new List<int>();

        for (int r = 0; r < GridSize; r++)
        {
            bool full = true;
            for (int c = 0; c < GridSize; c++)
            {
                if (!gridCells[r, c].isOccupied) { full = false; break; }
            }
            if (full) fullRows.Add(r);
        }

        for (int c = 0; c < GridSize; c++)
        {
            bool full = true;
            for (int r = 0; r < GridSize; r++)
            {
                if (!gridCells[r, c].isOccupied) { full = false; break; }
            }
            if (full) fullCols.Add(c);
        }

        int totalLines = fullRows.Count + fullCols.Count;

        if (totalLines > 0)
        {
            comboStreak++;
            int basePoints = totalLines == 1 ? 100 : (totalLines == 2 ? 300 : (totalLines == 3 ? 600 : totalLines * 250));
            int comboBonus = (comboStreak - 1) * 60;
            int earnedScore = basePoints + comboBonus;
            if (comboStreak > 1) PlaySound(comboSound, comboSoundVolume);
            else PlaySound(clearSound, clearSoundVolume);

            float shakeStrength = 6f + totalLines * 4f + comboStreak * 2f;
            TriggerBoardShake(0.22f, shakeStrength);

            List<Cell> cellsToClear = new List<Cell>();
            HashSet<Cell> set = new HashSet<Cell>();
            foreach (int r in fullRows)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (set.Add(gridCells[r, c])) cellsToClear.Add(gridCells[r, c]);
                }
            }
            foreach (int c in fullCols)
            {
                for (int r = 0; r < GridSize; r++)
                {
                    if (set.Add(gridCells[r, c])) cellsToClear.Add(gridCells[r, c]);
                }
            }

            Vector2 avgCenter = Vector2.zero;
            foreach (var c in cellsToClear) avgCenter += c.localPos;
            avgCenter /= cellsToClear.Count;

            Vector3 blastCenterWorld = boardContainer.TransformPoint(avgCenter);
            AddLineClearScore(earnedScore, blastCenterWorld);

            string comboMsg = $"+{earnedScore}";
            if (comboStreak > 1) comboMsg += $"\nCOMBO x{comboStreak}!";
            else if (totalLines >= 3) comboMsg += "\nAMAZING!";
            else if (totalLines == 2) comboMsg += "\nDOUBLE BLAST!";

            ShowFloatingScore(comboMsg, avgCenter, comboStreak > 1 ? new Color(1f, 0.85f, 0.2f) : Color.white);

            for (int i = 0; i < cellsToClear.Count; i++)
            {
                Cell cell = cellsToClear[i];
                StartCoroutine(BlastCellRoutine(cell));
                if (i % 2 == 0) yield return new WaitForSeconds(0.015f);
            }

            yield return new WaitForSeconds(0.18f);
        }
        else
        {
            comboStreak = 0;
        }

        yield return new WaitForSeconds(0.05f);

        bool anyRemaining = false;
        foreach (var slot in spawnSlots)
        {
            if (slot != null && slot.childCount > 0)
            {
                anyRemaining = true;
                break;
            }
        }

        if (!anyRemaining)
        {
            SpawnShapesInEmptySlots();
        }
        else
        {
            CheckGameOverState();
        }
    }

    private IEnumerator BlastCellRoutine(Cell cell)
    {
        Sprite blastSprite = cell.image.sprite;
        Vector2 blastPos = cell.localPos;

        SpawnBurstSparks(blastPos, blastSprite);

        float elapsed = 0f;
        float duration = 0.16f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cell.rect.localScale = Vector3.one * Mathf.Lerp(1.35f, 0f, t);
            cell.image.color = Color.Lerp(Color.white * 2f, Color.white, t);
            yield return null;
        }

        cell.rect.localScale = Vector3.one;
        cell.isOccupied = false;
        cell.image.sprite = emptyCellSprite;
        cell.image.color = emptyCellColor;
    }

    private void SpawnBurstSparks(Vector2 localCenter, Sprite sparkSprite)
    {
        if (effectsContainer == null) return;

        int sparkCount = 4;
        for (int i = 0; i < sparkCount; i++)
        {
            GameObject sparkObj = new GameObject("Spark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sparkObj.transform.SetParent(effectsContainer, false);
            RectTransform sRect = sparkObj.GetComponent<RectTransform>();
            sRect.anchoredPosition = localCenter;
            sRect.sizeDelta = new Vector2(cellSize * 0.35f, cellSize * 0.35f);

            Image sImg = sparkObj.GetComponent<Image>();
            sImg.sprite = sparkSprite;
            sImg.raycastTarget = false;

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(cellSize * 1.5f, cellSize * 3.5f);
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

            StartCoroutine(SparkMovementRoutine(sRect, sImg, velocity));
        }
    }

    private IEnumerator SparkMovementRoutine(RectTransform rect, Image img, Vector2 velocity)
    {
        float elapsed = 0f;
        float duration = 0.28f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rect.anchoredPosition += velocity * Time.deltaTime;
            rect.localScale = Vector3.one * (1f - t);
            rect.Rotate(0, 0, 360f * Time.deltaTime * 2f);
            img.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }
        Destroy(rect.gameObject);
    }

    private void ShowFloatingScore(string text, Vector2 localPos, Color color)
    {
        if (effectsContainer == null) return;

        GameObject textObj = new GameObject("FloatingScore", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(effectsContainer, false);
        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchoredPosition = localPos;
        rt.sizeDelta = new Vector2(500, 150);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 54;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;

        StartCoroutine(FloatingScoreRoutine(rt, tmp));
    }

    private IEnumerator FloatingScoreRoutine(RectTransform rt, TextMeshProUGUI tmp)
    {
        float elapsed = 0f;
        float duration = 0.75f;
        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, 110f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = t < 0.2f ? Mathf.Lerp(0.4f, 1.3f, t / 0.2f) : Mathf.Lerp(1.3f, 1.0f, (t - 0.2f) / 0.8f);
            rt.localScale = Vector3.one * scale;
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            tmp.alpha = 1f - Mathf.Pow(t, 2f);
            yield return null;
        }
        Destroy(rt.gameObject);
    }

    private Coroutine boardShakeRoutine;
    public void TriggerBoardShake(float duration, float magnitude)
    {
        if (boardContainer == null) return;
        if (boardShakeRoutine != null) StopCoroutine(boardShakeRoutine);
        boardShakeRoutine = StartCoroutine(BoardShakeRoutine(duration, magnitude));
    }

    private IEnumerator BoardShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = 1f - (elapsed / duration);
            Vector2 offset = Random.insideUnitCircle * magnitude * percent;
            boardContainer.anchoredPosition = originalBoardAnchoredPos + offset;
            yield return null;
        }
        boardContainer.anchoredPosition = originalBoardAnchoredPos;
    }

    private int displayedScore = 0;
    private Coroutine scoreRollRoutine;
    private Coroutine scorePunchRoutine;

    public void AddLineClearScore(int points, Vector3 blastWorldOrigin)
    {
        int startScore = currentScore;
        currentScore += points;
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("BlockBlast_HighScore", highScore);
        }

        SpawnFlyingScoreStars(blastWorldOrigin, 6);

        if (scoreRollRoutine != null) StopCoroutine(scoreRollRoutine);
        scoreRollRoutine = StartCoroutine(AnimateScoreRollRoutine(startScore, currentScore));

        if (scorePunchRoutine != null) StopCoroutine(scorePunchRoutine);
        scorePunchRoutine = StartCoroutine(PunchScoreTextRoutine());
    }

    private IEnumerator AnimateScoreRollRoutine(int fromScore, int toScore)
    {
        float elapsed = 0f;
        float duration = 0.45f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - (1f - t) * (1f - t);
            displayedScore = Mathf.RoundToInt(Mathf.Lerp(fromScore, toScore, ease));

            if (scoreTextTMP != null) scoreTextTMP.text = displayedScore.ToString();
            if (legacyScoreText != null) legacyScoreText.text = displayedScore.ToString();
            if (highScoreTextTMP != null) highScoreTextTMP.text = Mathf.Max(displayedScore, highScore).ToString();

            yield return null;
        }

        displayedScore = toScore;
        if (scoreTextTMP != null) scoreTextTMP.text = displayedScore.ToString();
        if (legacyScoreText != null) legacyScoreText.text = displayedScore.ToString();
        if (highScoreTextTMP != null) highScoreTextTMP.text = highScore.ToString();
        scoreRollRoutine = null;
    }

    private IEnumerator PunchScoreTextRoutine()
    {
        Transform scoreTransform = scoreTextTMP != null ? scoreTextTMP.transform : (legacyScoreText != null ? legacyScoreText.transform : null);
        if (scoreTransform == null) yield break;

        Color originalColor = scoreTextTMP != null ? scoreTextTMP.color : Color.white;
        Color punchGoldColor = new Color(1f, 0.88f, 0.2f);

        float elapsed = 0f;
        float duration = 0.38f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = t < 0.28f ? Mathf.Lerp(1.0f, 1.45f, t / 0.28f) : Mathf.Lerp(1.45f, 1.0f, (t - 0.28f) / 0.72f);
            scoreTransform.localScale = Vector3.one * scale;

            Color c = Color.Lerp(punchGoldColor, originalColor, t);
            if (scoreTextTMP != null) scoreTextTMP.color = c;

            yield return null;
        }

        scoreTransform.localScale = Vector3.one;
        if (scoreTextTMP != null) scoreTextTMP.color = originalColor;
        scorePunchRoutine = null;
    }

    private void SpawnFlyingScoreStars(Vector3 worldOrigin, int count)
    {
        if (effectsContainer == null) return;
        Vector3 targetPos = scoreTextTMP != null ? scoreTextTMP.transform.position : (legacyScoreText != null ? legacyScoreText.transform.position : boardContainer.position + Vector3.up * 400f);

        for (int i = 0; i < count; i++)
        {
            GameObject starObj = new GameObject("ScoreStar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            starObj.transform.SetParent(effectsContainer, false);
            RectTransform sRect = starObj.GetComponent<RectTransform>();
            sRect.position = worldOrigin + (Vector3)(Random.insideUnitCircle * cellSize * 0.45f);
            sRect.sizeDelta = new Vector2(cellSize * 0.35f, cellSize * 0.35f);

            Image sImg = starObj.GetComponent<Image>();
            sImg.sprite = (block1x1Sprites != null && block1x1Sprites.Length > 0) ? block1x1Sprites[Random.Range(0, block1x1Sprites.Length)] : null;
            sImg.color = new Color(1f, 0.92f, 0.35f);
            sImg.raycastTarget = false;

            StartCoroutine(FlyingStarRoutine(sRect, targetPos, i * 0.035f));
        }
    }

    private IEnumerator FlyingStarRoutine(RectTransform rect, Vector3 targetPos, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        Vector3 startPos = rect.position;
        Vector3 controlPoint = (startPos + targetPos) * 0.5f + (Vector3)(Random.insideUnitCircle * 90f);

        float elapsed = 0f;
        float duration = 0.42f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float u = 1f - t;
            rect.position = u * u * startPos + 2f * u * t * controlPoint + t * t * targetPos;
            rect.localScale = Vector3.one * (1f - t * 0.3f);
            rect.Rotate(0, 0, 480f * Time.deltaTime);

            yield return null;
        }

        Destroy(rect.gameObject);
    }

    private void UpdateScoreUI()
    {
        displayedScore = currentScore;
        if (scoreTextTMP != null) scoreTextTMP.text = currentScore.ToString();
        if (legacyScoreText != null) legacyScoreText.text = currentScore.ToString();
        if (highScoreTextTMP != null) highScoreTextTMP.text = highScore.ToString();
    }
}
