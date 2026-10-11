using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public partial class BlockBlastGame
{
    public bool IsGameOver => isGameOver;
    private GameObject autoGameOverPanel;
    private CanvasGroup boardCanvasGroup;
    private GameObject screenDimmer;

    public void CheckGameOverState()
    {
        if (isGameOver) return;

        List<DraggableBlock> activeShapes = new List<DraggableBlock>();
        foreach (var slot in spawnSlots)
        {
            if (slot != null)
            {
                DraggableBlock s = slot.GetComponentInChildren<DraggableBlock>();
                if (s != null && s.gameObject.activeInHierarchy && !s.IsPlaced)
                    activeShapes.Add(s);
            }
        }

        if (activeShapes.Count == 0) return;

        bool hasAnyValidMove = false;
        foreach (var shape in activeShapes)
        {
            if (CanShapeFitAnywhere(shape.Points))
            {
                hasAnyValidMove = true;
                shape.SetDimmed(false);
            }
            else
            {
                shape.SetDimmed(true);
            }
        }

        if (!hasAnyValidMove)
        {
            foreach (var shape in activeShapes)
            {
                shape.SetDimmed(true);
            }
            TriggerGameOver();
        }
    }

    private bool CanShapeFitAnywhere(List<Vector2Int> points)
    {
        for (int r = 0; r < GridSize; r++)
        {
            for (int c = 0; c < GridSize; c++)
            {
                if (CanShapeFitAt(points, r, c)) return true;
            }
        }
        return false;
    }

    private bool CanShapeFitAt(List<Vector2Int> points, int anchorRow, int anchorCol)
    {
        foreach (var pt in points)
        {
            int r = anchorRow + pt.y;
            int c = anchorCol + pt.x;
            if (r < 0 || r >= GridSize || c < 0 || c >= GridSize) return false;
            if (gridCells[r, c].isOccupied) return false;
        }
        return true;
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        PlaySound(gameOverSound, gameOverSoundVolume);
        Debug.Log("<color=red>GAME OVER! Không còn vị trí nào vừa với các khối còn lại.</color>");

        TriggerBoardShake(0.35f, 9f);
        SetBackgroundDimmed(true);
        AutoDetectGameOverElements();

        if (gameOverPanel != null || homeButton != null || retryButton != null)
        {
            ShowGameOverUI();
        }
        else
        {
            ShowAutoGameOverPopup();
        }
    }

    private void SetBackgroundDimmed(bool dimmed)
    {
        if (boardContainer != null)
        {
            if (boardCanvasGroup == null)
            {
                boardCanvasGroup = boardContainer.GetComponent<CanvasGroup>();
                if (boardCanvasGroup == null) boardCanvasGroup = boardContainer.gameObject.AddComponent<CanvasGroup>();
            }

            if (dimmed)
            {
                StartCoroutine(FadeCanvasGroupRoutine(boardCanvasGroup, 0.32f, 0.35f));
            }
            else
            {
                boardCanvasGroup.alpha = 1.0f;
            }
        }

        if (spawnSlots != null)
        {
            foreach (var slot in spawnSlots)
            {
                if (slot != null)
                {
                    CanvasGroup cg = slot.GetComponent<CanvasGroup>();
                    if (cg == null) cg = slot.gameObject.AddComponent<CanvasGroup>();

                    if (dimmed)
                    {
                        StartCoroutine(FadeCanvasGroupRoutine(cg, 0.28f, 0.35f));
                    }
                    else
                    {
                        cg.alpha = 1.0f;
                    }

                    DraggableBlock s = slot.GetComponentInChildren<DraggableBlock>();
                    if (s != null) s.SetDimmed(dimmed);
                }
            }
        }

        EnsureScreenDimmer();
        if (screenDimmer != null)
        {
            if (dimmed)
            {
                screenDimmer.SetActive(true);
                screenDimmer.transform.SetAsLastSibling();
                Image dimImg = screenDimmer.GetComponent<Image>();
                if (dimImg != null)
                {
                    dimImg.color = new Color(0f, 0f, 0f, 0f);
                    StartCoroutine(FadeImageAlphaRoutine(dimImg, 0f, 0.78f, 0.35f));
                }
            }
            else
            {
                screenDimmer.SetActive(false);
            }
        }
    }

    private void EnsureBlocksNormalBrightness()
    {
        if (boardCanvasGroup != null) boardCanvasGroup.alpha = 1.0f;
        if (screenDimmer != null) screenDimmer.SetActive(false);

        if (spawnSlots != null)
        {
            foreach (var slot in spawnSlots)
            {
                if (slot != null)
                {
                    CanvasGroup cg = slot.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = 1.0f;

                    DraggableBlock s = slot.GetComponentInChildren<DraggableBlock>();
                    if (s != null) s.SetDimmed(false);
                }
            }
        }
    }

    private void EnsureScreenDimmer()
    {
        if (screenDimmer != null) return;
        Canvas canvas = mainCanvas != null ? mainCanvas : (boardContainer != null ? boardContainer.GetComponentInParent<Canvas>() : null);
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        screenDimmer = new GameObject("ScreenDimmer_Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        screenDimmer.transform.SetParent(canvas.transform, false);
        RectTransform rt = screenDimmer.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image img = screenDimmer.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.78f);
        img.raycastTarget = true;
        screenDimmer.SetActive(false);
    }

    private IEnumerator FadeCanvasGroupRoutine(CanvasGroup cg, float targetAlpha, float duration)
    {
        if (cg == null) yield break;
        float startAlpha = cg.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;
    }

    private IEnumerator FadeImageAlphaRoutine(Image img, float startA, float targetA, float duration)
    {
        if (img == null) yield break;
        Color c = img.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startA, targetA, elapsed / duration);
            img.color = c;
            yield return null;
        }
        c.a = targetA;
        img.color = c;
    }

    public void AutoDetectGameOverElements()
    {
        Transform canvasTransform = mainCanvas != null ? mainCanvas.transform : (boardContainer != null ? boardContainer.GetComponentInParent<Canvas>()?.transform : null);

        if (gameOverPanel == null && canvasTransform != null)
        {
            Transform t = canvasTransform.Find("Gmeovera");
            if (t == null) t = canvasTransform.Find("GameOver");
            if (t == null) t = canvasTransform.Find("GameOverPanel");
            if (t != null) gameOverPanel = t.gameObject;
        }

        if (homeButton == null && canvasTransform != null)
        {
            Transform t = canvasTransform.Find("Home");
            if (t == null && gameOverPanel != null) t = gameOverPanel.transform.Find("Home");
            if (t != null) homeButton = t.gameObject;
        }

        if (retryButton == null && canvasTransform != null)
        {
            Transform t = canvasTransform.Find("Retry");
            if (t == null && gameOverPanel != null) t = gameOverPanel.transform.Find("Retry");
            if (t != null) retryButton = t.gameObject;
        }

        if (gameOverPanel != null && (gameOverScoreTextTMP == null || gameOverHighScoreTextTMP == null))
        {
            TextMeshProUGUI[] tmps = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                string lower = t.gameObject.name.ToLower();
                if ((lower.Contains("score") || lower.Contains("diem")) && !lower.Contains("high") && !lower.Contains("best") && !lower.Contains("luc"))
                {
                    if (gameOverScoreTextTMP == null) gameOverScoreTextTMP = t;
                }
                else if (lower.Contains("high") || lower.Contains("best") || lower.Contains("luc"))
                {
                    if (gameOverHighScoreTextTMP == null) gameOverHighScoreTextTMP = t;
                }
            }
        }
    }

    public void SetupButtonListeners()
    {
        if (retryButton != null)
        {
            Button btn = retryButton.GetComponent<Button>();
            if (btn == null) btn = retryButton.GetComponentInChildren<Button>(true);
            if (btn == null) btn = retryButton.AddComponent<Button>();
            btn.onClick.RemoveListener(OnClickRetry);
            btn.onClick.AddListener(OnClickRetry);

            Graphic g = retryButton.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = true;
        }

        if (homeButton != null)
        {
            Button btn = homeButton.GetComponent<Button>();
            if (btn == null) btn = homeButton.GetComponentInChildren<Button>(true);
            if (btn == null) btn = homeButton.AddComponent<Button>();
            btn.onClick.RemoveListener(OnClickHome);
            btn.onClick.AddListener(OnClickHome);

            Graphic g = homeButton.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = true;
        }
    }

    public void HideGameOverUI()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (homeButton != null) homeButton.SetActive(false);
        if (retryButton != null) retryButton.SetActive(false);
        if (autoGameOverPanel != null) autoGameOverPanel.SetActive(false);
    }

    public void ShowGameOverUI()
    {
        if (screenDimmer != null && screenDimmer.activeSelf)
        {
            screenDimmer.transform.SetAsLastSibling();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }

        if (homeButton != null)
        {
            homeButton.SetActive(true);
            homeButton.transform.SetAsLastSibling();
        }

        if (retryButton != null)
        {
            retryButton.SetActive(true);
            retryButton.transform.SetAsLastSibling();
        }

        SetupButtonListeners();

        if (gameOverScoreTextTMP != null)
            gameOverScoreTextTMP.text = currentScore.ToString();
        if (gameOverHighScoreTextTMP != null)
            gameOverHighScoreTextTMP.text = highScore.ToString();
    }

    private void ShowAutoGameOverPopup()
    {
        Canvas canvas = boardContainer != null ? boardContainer.GetComponentInParent<Canvas>() : null;
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        if (autoGameOverPanel == null)
        {
            autoGameOverPanel = new GameObject("Auto_GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            autoGameOverPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = autoGameOverPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            Image panelBg = autoGameOverPanel.GetComponent<Image>();
            panelBg.color = new Color(0.04f, 0.04f, 0.12f, 0.88f);

            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(autoGameOverPanel.transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(760, 680);
            cardRect.anchoredPosition = Vector2.zero;

            Image cardBg = card.GetComponent<Image>();
            cardBg.color = new Color(0.11f, 0.13f, 0.28f, 0.98f);

            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(card.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 205);
            titleRect.sizeDelta = new Vector2(700, 100);
            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.text = "GAME OVER";
            titleText.fontSize = 72;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.32f, 0.32f);

            GameObject subObj = new GameObject("SubTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(card.transform, false);
            RectTransform subRect = subObj.GetComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0, 135);
            subRect.sizeDelta = new Vector2(700, 50);
            TextMeshProUGUI subText = subObj.GetComponent<TextMeshProUGUI>();
            subText.text = "Hết chỗ xếp khối!";
            subText.fontSize = 32;
            subText.alignment = TextAlignmentOptions.Center;
            subText.color = new Color(0.8f, 0.8f, 0.9f);

            GameObject scoreObj = new GameObject("ScoreText", typeof(RectTransform), typeof(TextMeshProUGUI));
            scoreObj.transform.SetParent(card.transform, false);
            RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
            scoreRect.anchoredPosition = new Vector2(0, 45);
            scoreRect.sizeDelta = new Vector2(700, 80);
            TextMeshProUGUI sText = scoreObj.GetComponent<TextMeshProUGUI>();
            sText.text = $"ĐIỂM SỐ: {currentScore}";
            sText.fontSize = 52;
            sText.fontStyle = FontStyles.Bold;
            sText.alignment = TextAlignmentOptions.Center;
            sText.color = Color.white;

            GameObject bestObj = new GameObject("BestText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bestObj.transform.SetParent(card.transform, false);
            RectTransform bestRect = bestObj.GetComponent<RectTransform>();
            bestRect.anchoredPosition = new Vector2(0, -35);
            bestRect.sizeDelta = new Vector2(700, 60);
            TextMeshProUGUI bText = bestObj.GetComponent<TextMeshProUGUI>();
            bText.text = $"KỶ LỤC: {highScore}";
            bText.fontSize = 38;
            bText.alignment = TextAlignmentOptions.Center;
            bText.color = new Color(1f, 0.85f, 0.25f);

            GameObject btnObj = new GameObject("RestartBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(card.transform, false);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -170);
            btnRect.sizeDelta = new Vector2(380, 100);

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(0.18f, 0.78f, 0.36f);

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(RestartGame);

            GameObject btnTextObj = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
            btnTextRect.anchoredPosition = Vector2.zero;
            btnTextRect.sizeDelta = new Vector2(360, 90);
            TextMeshProUGUI btnText = btnTextObj.GetComponent<TextMeshProUGUI>();
            btnText.text = "CHƠI LẠI";
            btnText.fontSize = 44;
            btnText.fontStyle = FontStyles.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
        }

        TextMeshProUGUI[] tmps = autoGameOverPanel.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var t in tmps)
        {
            if (t.gameObject.name == "ScoreText") t.text = $"ĐIỂM SỐ: {currentScore}";
            if (t.gameObject.name == "BestText") t.text = $"KỶ LỤC: {highScore}";
        }

        autoGameOverPanel.SetActive(true);
        autoGameOverPanel.transform.SetAsLastSibling();

        Transform cardTransform = autoGameOverPanel.transform.Find("Card");
        if (cardTransform != null)
        {
            StartCoroutine(PunchScaleCellRoutine(cardTransform.GetComponent<RectTransform>(), 1.15f, 0.22f));
        }
    }

    public void OnClickRetry()
    {
        PlaySound(buttonClickSound, buttonClickVolume);
        RestartGame();
    }

    public void OnClickHome()
    {
        PlaySound(buttonClickSound, buttonClickVolume);
        string targetScene = !string.IsNullOrWhiteSpace(homeSceneName) ? homeSceneName : "Main scene";
        Debug.Log($"<color=cyan>[BlockBlastGame] Nút Home được bấm! Đang chuyển về Scene: {targetScene}</color>");
        try
        {
            SceneManager.LoadScene(targetScene);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[BlockBlastGame] Lỗi khi chuyển Scene '{targetScene}': {ex.Message}");
        }
    }

    public void RestartGame()
    {
        SetBackgroundDimmed(false);
        HideGameOverUI();

        if (scoreRollRoutine != null) StopCoroutine(scoreRollRoutine);
        if (scorePunchRoutine != null) StopCoroutine(scorePunchRoutine);
        currentScore = 0;
        displayedScore = 0;
        comboStreak = 0;
        isGameOver = false;
        UpdateScoreUI();

        for (int r = 0; r < GridSize; r++)
        {
            for (int c = 0; c < GridSize; c++)
            {
                gridCells[r, c].isOccupied = false;
                gridCells[r, c].image.sprite = emptyCellSprite;
                gridCells[r, c].image.color = emptyCellColor;
                gridCells[r, c].rect.localScale = Vector3.one;
            }
        }

        EnsureBlocksNormalBrightness();
        RespawnAllThreeShapes();
    }
}
