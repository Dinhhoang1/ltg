using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// SCRIPT DUY NHẤT QUẢN LÝ TOÀN BỘ GAME BLOCK BLAST (PHIÊN BẢN NÂNG CẤP SIÊU MƯỢT & HIỆU ỨNG JUICY)
/// - Quản lý bàn cờ 8x8 tự động khớp khung
/// - Sinh ngẫu nhiên các khối hình đa dạng từ các ảnh 1x1
/// - KÉO THẢ SIÊU MƯỢT: Khối nhấc lên có độ nảy đàn hồi (Elastic Bounce), nghiêng nhẹ theo quán tính tay (Physics Tilt)
/// - HIỆU ỨNG PREVIEW BÁO TRƯỚC HÀNG SẮP ĂN: Khi rê khối sắp tạo thành hàng/cột hoàn chỉnh, cả hàng đó sẽ phát sáng nhấp nháy
/// - HIỆU ỨNG NỔ WAVE LAN TỎA (Blast Wave): Khi ăn hàng, các khối nổ theo làn sóng từ tâm ra ngoài kèm hạt nổ li ti (Particle Sparks)
/// - HIỆU ỨNG RUNG BÀN CỜ (Screen Shake): Rung lắc bàn cờ khi nổ dòng, tăng mạnh khi ăn Combo
/// - CHỮ ĐIỂM BAY & COMBO (Floating Text): Hiện chữ nổi "+300 COMBO x2!", "AMAZING!", "UNBELIEVABLE!" bay lên
/// - Tự động hồi khối mới với hiệu ứng Pop-in từng ô
/// </summary>
public class BlockBlastGame : MonoBehaviour
{
    public static BlockBlastGame Instance { get; private set; }

    [Header("=== 1. CÀI ĐẶT BÀN CỜ 8x8 ===")]
    [Tooltip("Kéo đối tượng bàn cờ Image (1) vào đây")]
    public RectTransform boardContainer;
    [Tooltip("Khoảng cách đệm từ viền bàn cờ vào trong")]
    public float boardPadding = 12f;
    [Tooltip("Khoảng cách giữa các ô trên bàn cờ")]
    public float cellMargin = 4f;
    [Tooltip("Ảnh ô trống mặc định (để trống nếu muốn trong suốt)")]
    public Sprite emptyCellSprite;
    [Tooltip("Màu ô trống khi chưa có khối (Alpha = 0 để trong suốt)")]
    public Color emptyCellColor = new Color(0, 0, 0, 0);

    [Header("=== 2. CÁC KHỐI 1x1 ĐẦU VÀO ===")]
    [Tooltip("Kéo tất cả các ảnh 1x1 từ thư mục Asset2 vào đây")]
    public Sprite[] block1x1Sprites;

    [Header("=== 3. CÁC Ô CHỨA KHỐI (SLOTS) ===")]
    [Tooltip("Kéo 3 slot con của 'put block' vào đây")]
    public Transform[] spawnSlots;
    [Tooltip("Tỉ lệ thu nhỏ khi khối nằm chờ trong slot")]
    public float slotScale = 0.55f;
    [Tooltip("Tỉ lệ phóng to khi nhấc khối lên kéo")]
    public float dragScale = 1.0f;
    [Tooltip("Độ lệch Y để khối bay cao hơn ngón tay/chuột, không bị che tầm nhìn")]
    public float dragYOffset = 135f;

    [Header("=== 4. HIỂN THỊ ĐIỂM SỐ ===")]
    [Tooltip("Kéo đối tượng Text (TMP) hiển thị điểm vào đây")]
    public TextMeshProUGUI scoreTextTMP;
    public TextMeshProUGUI highScoreTextTMP;
    public Text legacyScoreText;

    [Header("=== 5. ÂM THANH ===")]
    public AudioSource audioSource;
    public AudioClip placeSound;
    public AudioClip clearSound;
    public AudioClip comboSound;
    public AudioClip gameOverSound;

    [Header("=== 6. ĐIỀU KHIỂN NÚT BẤM & GIAO DIỆN GAME OVER ===")]
    [Tooltip("Kéo bảng GameOver (Gmeovera) vào đây")]
    public GameObject gameOverPanel;
    [Tooltip("Kéo nút hoặc đối tượng Chơi Lại (Retry) vào đây")]
    public GameObject retryButton;
    [Tooltip("Kéo nút hoặc đối tượng Về Trang Chủ (Home) vào đây")]
    public GameObject homeButton;
    [Tooltip("Kéo Canvas chính vào đây (tùy chọn)")]
    public Canvas mainCanvas;
    [Tooltip("Tên Scene Trang Chủ muốn tải khi bấm nút Home (mặc định là 'Main scene')")]
    public string homeSceneName = "Main scene";
    [Tooltip("Text hiển thị điểm cuối cùng trên bảng GameOver (tùy chọn)")]
    public TextMeshProUGUI gameOverScoreTextTMP;
    [Tooltip("Text hiển thị kỷ lục trên bảng GameOver (tùy chọn)")]
    public TextMeshProUGUI gameOverHighScoreTextTMP;

    // --- Biến nội bộ quản lý bàn cờ ---
    public const int GridSize = 8;
    private Cell[,] gridCells = new Cell[GridSize, GridSize];
    private float cellSize;
    private float topLeftX;
    private float topLeftY;
    private Vector2 originalBoardAnchoredPos;
    private RectTransform effectsContainer;

    // --- Biến điểm số & Game state ---
    private int currentScore = 0;
    private int highScore = 0;
    private int comboStreak = 0;
    private bool isGameOver = false;

    public class Cell
    {
        public int row;
        public int col;
        public bool isOccupied;
        public Image image;
        public RectTransform rect;
        public Vector2 localPos;
    }

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

    private void Awake()
    {
        Instance = this;
        InitializeShapeLibrary();
    }

    private void Start()
    {
        highScore = PlayerPrefs.GetInt("BlockBlast_HighScore", 0);
        UpdateScoreUI();

        if (mainCanvas == null)
        {
            mainCanvas = boardContainer != null ? boardContainer.GetComponentInParent<Canvas>() : Object.FindAnyObjectByType<Canvas>();
        }

        // Tự động tìm kiếm nếu chưa gán trên Inspector
        AutoDetectGameOverElements();

        // Gắn sự kiện click cho các nút
        SetupButtonListeners();

        // Ẩn bảng Game Over và các nút khi bắt đầu chơi
        HideGameOverUI();

        InitializeBoard();
        RespawnAllThreeShapes();
    }

    /// <summary>
    /// Tự động tìm kiếm các đối tượng Gmeovera, Home, Retry trong Canvas nếu chưa kéo thả
    /// </summary>
    public void AutoDetectGameOverElements()
    {
        Transform canvasTransform = mainCanvas != null ? mainCanvas.transform : (boardContainer != null ? boardContainer.GetComponentInParent<Canvas>()?.transform : null);

        // 1. Tìm bảng GameOver "Gmeovera"
        if (gameOverPanel == null && canvasTransform != null)
        {
            Transform t = canvasTransform.Find("Gmeovera");
            if (t == null) t = canvasTransform.Find("GameOver");
            if (t == null) t = canvasTransform.Find("GameOverPanel");
            if (t != null) gameOverPanel = t.gameObject;
        }

        // 2. Tìm nút Home
        if (homeButton == null && canvasTransform != null)
        {
            Transform t = canvasTransform.Find("Home");
            if (t == null && gameOverPanel != null) t = gameOverPanel.transform.Find("Home");
            if (t != null) homeButton = t.gameObject;
        }

        // 3. Tìm nút Retry
        if (retryButton == null && canvasTransform != null)
        {
            Transform t = canvasTransform.Find("Retry");
            if (t == null && gameOverPanel != null) t = gameOverPanel.transform.Find("Retry");
            if (t != null) retryButton = t.gameObject;
        }

        // 4. Tìm Text điểm số nếu chưa gán
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

    /// <summary>
    /// Gắn sự kiện click an toàn cho các nút Home và Retry (tự động thêm Button component nếu đối tượng chỉ có Image)
    /// </summary>
    public void SetupButtonListeners()
    {
        // Gắn sự kiện cho nút Retry
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

        // Gắn sự kiện cho nút Home
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

    /// <summary>
    /// Ẩn bảng Game Over và các nút Home, Retry
    /// </summary>
    public void HideGameOverUI()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (homeButton != null) homeButton.SetActive(false);
        if (retryButton != null) retryButton.SetActive(false);
        if (autoGameOverPanel != null) autoGameOverPanel.SetActive(false);
    }

    /// <summary>
    /// Hiển thị bảng Game Over và đưa các nút Home, Retry lên trên cùng
    /// </summary>
    public void ShowGameOverUI()
    {
        // Đặt màn che tối mờ ngay dưới bảng Game Over
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

    #region KHỞI TẠO BÀN CỜ
    private void InitializeBoard()
    {
        if (boardContainer == null)
        {
            Debug.LogError("[BlockBlastGame] Chưa gán boardContainer! Hãy kéo Image (1) vào Inspector.");
            return;
        }

        originalBoardAnchoredPos = boardContainer.anchoredPosition;

        float boardWidth = boardContainer.rect.width;
        float boardHeight = boardContainer.rect.height;
        float availableWidth = boardWidth - boardPadding * 2f;
        float availableHeight = boardHeight - boardPadding * 2f;
        cellSize = Mathf.Min(availableWidth, availableHeight) / GridSize;

        topLeftX = -boardWidth * 0.5f + boardPadding;
        topLeftY = boardHeight * 0.5f - boardPadding;

        // Xóa grid cũ nếu có
        Transform oldGrid = boardContainer.Find("AutoGenerated_GridCells");
        if (oldGrid != null) Destroy(oldGrid.gameObject);
        Transform oldEffects = boardContainer.Find("AutoGenerated_Effects");
        if (oldEffects != null) Destroy(oldEffects.gameObject);

        // Tạo root chứa 64 ô
        GameObject gridRoot = new GameObject("AutoGenerated_GridCells", typeof(RectTransform));
        gridRoot.transform.SetParent(boardContainer, false);
        RectTransform gridRootRect = gridRoot.GetComponent<RectTransform>();
        gridRootRect.anchorMin = Vector2.zero;
        gridRootRect.anchorMax = Vector2.one;
        gridRootRect.sizeDelta = Vector2.zero;
        gridRootRect.anchoredPosition = Vector2.zero;

        for (int r = 0; r < GridSize; r++)
        {
            for (int c = 0; c < GridSize; c++)
            {
                GameObject cellObj = new GameObject($"Cell_{r}_{c}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                cellObj.transform.SetParent(gridRoot.transform, false);

                RectTransform cellRect = cellObj.GetComponent<RectTransform>();
                cellRect.anchorMin = new Vector2(0.5f, 0.5f);
                cellRect.anchorMax = new Vector2(0.5f, 0.5f);
                cellRect.pivot = new Vector2(0.5f, 0.5f);
                cellRect.sizeDelta = new Vector2(cellSize - cellMargin, cellSize - cellMargin);

                Vector2 pos = GetCellLocalPos(r, c);
                cellRect.anchoredPosition = pos;

                Image cellImage = cellObj.GetComponent<Image>();
                cellImage.sprite = emptyCellSprite;
                cellImage.color = emptyCellColor;
                cellImage.raycastTarget = false;

                Cell cell = new Cell
                {
                    row = r,
                    col = c,
                    isOccupied = false,
                    image = cellImage,
                    rect = cellRect,
                    localPos = pos
                };

                gridCells[r, c] = cell;
            }
        }

        // Tạo root chứa hiệu ứng nổi
        GameObject fxObj = new GameObject("AutoGenerated_Effects", typeof(RectTransform));
        fxObj.transform.SetParent(boardContainer, false);
        effectsContainer = fxObj.GetComponent<RectTransform>();
        effectsContainer.anchorMin = Vector2.zero;
        effectsContainer.anchorMax = Vector2.one;
        effectsContainer.sizeDelta = Vector2.zero;
        effectsContainer.anchoredPosition = Vector2.zero;
        effectsContainer.SetAsLastSibling();
    }

    public Vector2 GetCellLocalPos(int row, int col)
    {
        float x = topLeftX + (col + 0.5f) * cellSize;
        float y = topLeftY - (row + 0.5f) * cellSize;
        return new Vector2(x, y);
    }
    #endregion

    #region THƯ VIỆN HÌNH DÁNG KHỐI
    private void InitializeShapeLibrary()
    {
        shapeLibrary.Clear();
        // 1. Đơn 1x1
        shapeLibrary.Add(new ShapePattern("Dot_1x1", new Vector2Int(0, 0)));
        // 2. Thẳng 2
        shapeLibrary.Add(new ShapePattern("Line_2_H", new Vector2Int(0, 0), new Vector2Int(1, 0)));
        shapeLibrary.Add(new ShapePattern("Line_2_V", new Vector2Int(0, 0), new Vector2Int(0, 1)));
        // 3. Thẳng 3
        shapeLibrary.Add(new ShapePattern("Line_3_H", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)));
        shapeLibrary.Add(new ShapePattern("Line_3_V", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)));
        // 4. Thẳng 4
        shapeLibrary.Add(new ShapePattern("Line_4_H", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0)));
        shapeLibrary.Add(new ShapePattern("Line_4_V", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3)));
        // 5. Thẳng 5
        shapeLibrary.Add(new ShapePattern("Line_5_H", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0)));
        shapeLibrary.Add(new ShapePattern("Line_5_V", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4)));
        // 6. Vuông 2x2
        shapeLibrary.Add(new ShapePattern("Square_2x2", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)));
        // 7. Vuông 3x3
        shapeLibrary.Add(new ShapePattern("Square_3x3",
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
            new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)));
        // 8. Chữ L nhỏ (2x2)
        shapeLibrary.Add(new ShapePattern("L_Small_1", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)));
        shapeLibrary.Add(new ShapePattern("L_Small_2", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1)));
        shapeLibrary.Add(new ShapePattern("L_Small_3", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1)));
        shapeLibrary.Add(new ShapePattern("L_Small_4", new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)));
        // 9. Chữ L lớn (3x3)
        shapeLibrary.Add(new ShapePattern("L_Big_1", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)));
        shapeLibrary.Add(new ShapePattern("L_Big_2", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)));
        shapeLibrary.Add(new ShapePattern("L_Big_3", new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(1, 2), new Vector2Int(0, 2)));
        shapeLibrary.Add(new ShapePattern("L_Big_4", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2)));
        // 10. Chữ T (3x2)
        shapeLibrary.Add(new ShapePattern("T_1", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1)));
        shapeLibrary.Add(new ShapePattern("T_2", new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)));
        shapeLibrary.Add(new ShapePattern("T_3", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 1)));
        shapeLibrary.Add(new ShapePattern("T_4", new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(0, 1)));
        // 11. Chữ Z, S (3x2)
        shapeLibrary.Add(new ShapePattern("Z_1", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1)));
        shapeLibrary.Add(new ShapePattern("S_1", new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)));
    }
    #endregion

    #region SINH KHỐI VỚI HIỆU ỨNG POP-IN ĐÀN HỒI
    /// <summary>
    /// Xóa toàn bộ khối cũ trong các slot và ngay lập tức sinh ra 3 khối mới tinh (Dùng khi Retry)
    /// </summary>
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

            // Tách và xóa ngay lập tức tất cả khối cũ trong slot
            for (int c = slot.childCount - 1; c >= 0; c--)
            {
                Transform child = slot.GetChild(c);
                child.SetParent(null); // Tách ngay lập tức để slot.childCount về 0
                Destroy(child.gameObject);
            }

            // Sinh khối mới tinh
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
        rootRect.localScale = Vector3.zero; // Bắt đầu từ 0 để pop-in

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
            img.raycastTarget = true; // Bật để nhận sự kiện kéo thả

            blockRects.Add(br);
        }

        DraggableBlockShape dragHandler = shapeRoot.AddComponent<DraggableBlockShape>();
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
            // Easing Elastic Out
            float scale = Mathf.Sin(t * Mathf.PI * 0.5f) * targetScale * 1.15f;
            if (t > 0.7f) scale = Mathf.Lerp(targetScale * 1.15f, targetScale, (t - 0.7f) / 0.3f);
            target.localScale = Vector3.one * scale;
            yield return null;
        }
        target.localScale = Vector3.one * targetScale;
    }
    #endregion

    #region PREVIEW CỰC THÔNG MINH (BÁO TRƯỚC CẢ HÀNG SẮP ĂN)
    private List<Cell> currentPreviewCells = new List<Cell>();
    private List<Cell> currentLinePreviewCells = new List<Cell>();

    public void UpdateDragPreview(DraggableBlockShape shape)
    {
        List<Cell> targetCells = FindMatchingCellsForShape(shape);

        if (AreCellListsEqual(currentPreviewCells, targetCells)) return;

        ClearPreview();

        if (targetCells != null && targetCells.Count > 0)
        {
            currentPreviewCells = targetCells;
            // 1. Hiển thị mờ các ô khối sẽ đặt xuống
            foreach (var cell in currentPreviewCells)
            {
                cell.image.sprite = shape.BlockSprite;
                cell.image.color = new Color(1f, 1f, 1f, 0.45f);
            }

            // 2. TÍNH TRƯỚC: Nếu đặt khối này xuống thì có hàng/cột nào sẽ được ĂN ĐIỂM không?
            List<Cell> predictedLines = PredictLinesIfPlaced(targetCells);
            if (predictedLines.Count > 0)
            {
                currentLinePreviewCells = predictedLines;
                foreach (var cell in currentLinePreviewCells)
                {
                    if (!cell.isOccupied)
                    {
                        // Ô trống trong hàng sắp ăn sẽ sáng vàng/trắng rực rỡ
                        cell.image.sprite = shape.BlockSprite;
                        cell.image.color = new Color(1f, 1f, 0.6f, 0.75f);
                    }
                    else
                    {
                        // Ô đã có sẵn trên hàng sắp ăn sẽ sáng rực lên báo hiệu
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

        // Check hàng ngang
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

        // Check cột dọc
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

    public List<Cell> FindMatchingCellsForShape(DraggableBlockShape shape)
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

    public bool TryPlaceShape(DraggableBlockShape shape)
    {
        ClearPreview();
        List<Cell> targetCells = FindMatchingCellsForShape(shape);

        if (targetCells == null || targetCells.Count == 0)
            return false;

        // Điền vào ô và hiệu ứng dội (Punch Scale)
        foreach (var cell in targetCells)
        {
            cell.isOccupied = true;
            cell.image.sprite = shape.BlockSprite;
            cell.image.color = Color.white;
            StartCoroutine(PunchScaleCellRoutine(cell.rect, 1.25f, 0.12f));
        }

        // Khi di chuyển / đặt khối xuống thì KHÔNG cộng điểm (chỉ cộng điểm khi ăn hàng)
        PlaySound(placeSound);

        // Rung nhẹ khi đặt khối
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
    #endregion

    #region HIỆU ỨNG ĂN HÀNG DẠNG SÓNG (WAVE BLAST), HẠT NỔ & CHỮ BAY
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
            PlaySound(comboStreak > 1 ? comboSound : clearSound);

            // Rung màn hình tăng theo số hàng và combo
            float shakeStrength = 6f + totalLines * 4f + comboStreak * 2f;
            TriggerBoardShake(0.22f, shakeStrength);

            // Gom các ô cần xóa
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

            // Tính vị trí trung tâm để hiện chữ Combo và bay hạt sao điểm
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

            // Hiệu ứng nổ dạng sóng (Wave Blast): nổ từng ô lần lượt cực kỳ thích mắt
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

        // Bắn hạt nổ tung toé
        SpawnBurstSparks(blastPos, blastSprite);

        // Khối phóng to lóe sáng rồi thu nhỏ biến mất
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
            // Easing punch scale
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
    #endregion

    #region QUẢN LÝ ĐIỂM SỐ & HIỆU ỨNG TĂNG ĐIỂM
    private int displayedScore = 0;
    private Coroutine scoreRollRoutine;
    private Coroutine scorePunchRoutine;

    // Cộng điểm CHỈ KHI ĂN ĐƯỢC HÀNG/CỘT kèm hiệu ứng sao bay và số nhảy vùn vụt
    public void AddLineClearScore(int points, Vector3 blastWorldOrigin)
    {
        int startScore = currentScore;
        currentScore += points;
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("BlockBlast_HighScore", highScore);
        }

        // 1. Bắn các ngôi sao điểm bay từ vị trí ăn hàng lên chữ Score
        SpawnFlyingScoreStars(blastWorldOrigin, 6);

        // 2. Chạy hiệu ứng số nhảy liên hồi từ điểm cũ lên điểm mới
        if (scoreRollRoutine != null) StopCoroutine(scoreRollRoutine);
        scoreRollRoutine = StartCoroutine(AnimateScoreRollRoutine(startScore, currentScore));

        // 3. Chạy hiệu ứng Text Score phóng to nảy đàn hồi và phát sáng vàng rực
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
            // EaseOutQuad: nhảy nhanh lúc đầu rồi hãm lại mượt mà
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
        Color punchGoldColor = new Color(1f, 0.88f, 0.2f); // Vàng kim lấp lánh

        float elapsed = 0f;
        float duration = 0.38f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Bouncy punch: 1.0 -> 1.45 -> 1.0
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
            sImg.color = new Color(1f, 0.92f, 0.35f); // Màu vàng sao
            sImg.raycastTarget = false;

            StartCoroutine(FlyingStarRoutine(sRect, targetPos, i * 0.035f));
        }
    }

    private IEnumerator FlyingStarRoutine(RectTransform rect, Vector3 targetPos, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        Vector3 startPos = rect.position;
        // Điểm cong Bezier để hạt bay lượn hình vòng cung mềm mại
        Vector3 controlPoint = (startPos + targetPos) * 0.5f + (Vector3)(Random.insideUnitCircle * 90f);

        float elapsed = 0f;
        float duration = 0.42f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float u = 1f - t;
            // Bezier Quadratic
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

    public bool IsGameOver => isGameOver;
    private GameObject autoGameOverPanel;

    public void CheckGameOverState()
    {
        if (isGameOver) return;

        List<DraggableBlockShape> activeShapes = new List<DraggableBlockShape>();
        foreach (var slot in spawnSlots)
        {
            if (slot != null)
            {
                DraggableBlockShape s = slot.GetComponentInChildren<DraggableBlockShape>();
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
                // Khối không vừa ô nào trên bàn cờ thì làm tối màu đi để nhận biết
                shape.SetDimmed(true);
            }
        }

        // NẾU TẤT CẢ CÁC KHỐI ĐỀU KHÔNG VỪA NỮA => XỬ THUA (GAME OVER)
        if (!hasAnyValidMove)
        {
            // Làm tối tất cả các khối còn lại khi thua
            foreach (var shape in activeShapes)
            {
                shape.SetDimmed(true);
            }
            TriggerGameOver();
        }
    }

    private CanvasGroup boardCanvasGroup;
    private GameObject screenDimmer;

    private void SetBackgroundDimmed(bool dimmed)
    {
        // 1. Làm mờ & tối toàn bộ bàn cờ đằng sau khi thua
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

        // 2. Làm mờ & tối các ô slot chứa khối ở đáy
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

                    DraggableBlockShape s = slot.GetComponentInChildren<DraggableBlockShape>();
                    if (s != null) s.SetDimmed(dimmed);
                }
            }
        }

        // 3. Màn che tối mờ phủ kín màn hình (Black Backdrop Scrim)
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

                    DraggableBlockShape s = slot.GetComponentInChildren<DraggableBlockShape>();
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
        img.raycastTarget = true; // Khóa chạm vào bàn cờ phía sau
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

    private void TriggerGameOver()
    {
        isGameOver = true;
        PlaySound(gameOverSound);
        Debug.Log("<color=red>GAME OVER! Không còn vị trí nào vừa với các khối còn lại.</color>");

        // Rung mạnh bàn cờ báo hiệu thua
        TriggerBoardShake(0.35f, 9f);

        // Làm mờ toàn bộ cảnh nền đằng sau
        SetBackgroundDimmed(true);

        // Tự động tìm kiếm các đối tượng GameOver nếu chưa gán
        AutoDetectGameOverElements();

        // Hiển thị giao diện Game Over lên trước lớp mờ
        if (gameOverPanel != null || homeButton != null || retryButton != null)
        {
            ShowGameOverUI();
        }
        else
        {
            ShowAutoGameOverPopup();
        }
    }

    private void ShowAutoGameOverPopup()
    {
        Canvas canvas = boardContainer != null ? boardContainer.GetComponentInParent<Canvas>() : null;
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        if (autoGameOverPanel == null)
        {
            // 1. Tạo màn che tối mờ phủ kín màn hình
            autoGameOverPanel = new GameObject("Auto_GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            autoGameOverPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = autoGameOverPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            Image panelBg = autoGameOverPanel.GetComponent<Image>();
            panelBg.color = new Color(0.04f, 0.04f, 0.12f, 0.88f);

            // 2. Tạo khung Card thông báo ở giữa
            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(autoGameOverPanel.transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(760, 680);
            cardRect.anchoredPosition = Vector2.zero;

            Image cardBg = card.GetComponent<Image>();
            cardBg.color = new Color(0.11f, 0.13f, 0.28f, 0.98f); // Xanh tím sành điệu

            // 3. Tiêu đề GAME OVER
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

            // 4. Dòng thông báo hết nước
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

            // 5. Điểm số
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

            // 6. Kỷ lục cao nhất
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

            // 7. Nút CHƠI LẠI
            GameObject btnObj = new GameObject("RestartBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(card.transform, false);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -170);
            btnRect.sizeDelta = new Vector2(380, 100);

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(0.18f, 0.78f, 0.36f); // Nút xanh lá nổi bật

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

        // Cập nhật số điểm mới nhất
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

    // Xử lý khi bấm nút Retry (Chơi lại)
    public void OnClickRetry()
    {
        RestartGame();
    }

    // Xử lý khi bấm nút Home (Về trang chủ)
    public void OnClickHome()
    {
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

        // Khôi phục độ sáng bình thường cho bàn cờ và các khối
        EnsureBlocksNormalBrightness();

        // Xóa sạch khối cũ và lập tức sinh ra 3 khối mới tinh vào 3 slot
        RespawnAllThreeShapes();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
    #endregion
}

/// <summary>
/// Component điều khiển tương tác kéo thả mượt mà với hiệu ứng quán tính (Tilt) & bay về vị trí cũ (Smooth Return)
/// </summary>
public class DraggableBlockShape : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private BlockBlastGame game;
    private Transform originalSlot;
    private int slotIndex;
    private List<Vector2Int> points;
    private Sprite blockSprite;
    private List<RectTransform> blockRects;

    private float slotScale;
    private float dragScale;
    private float dragYOffset;

    private RectTransform rootRect;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;
    private bool isDragging = false;

    private Vector3 targetWorldPos;
    private Vector3 previousDragPos;
    private float targetTiltAngle = 0f;

    public List<Vector2Int> Points => points;
    public Sprite BlockSprite => blockSprite;
    public List<RectTransform> BlockRects => blockRects;
    public bool IsPlaced { get; set; } = false;

    public void Setup(BlockBlastGame game, Transform slot, int slotIndex, List<Vector2Int> points, Sprite sprite, List<RectTransform> rects, float sScale, float dScale, float yOffset)
    {
        this.game = game;
        this.originalSlot = slot;
        this.slotIndex = slotIndex;
        this.points = points;
        this.blockSprite = sprite;
        this.blockRects = rects;
        this.slotScale = sScale;
        this.dragScale = dScale;
        this.dragYOffset = yOffset;
        this.IsPlaced = false;

        this.rootRect = GetComponent<RectTransform>();
        this.canvasGroup = GetComponent<CanvasGroup>();
        this.parentCanvas = GetComponentInParent<Canvas>();
        if (this.parentCanvas == null && game != null && game.boardContainer != null)
            this.parentCanvas = game.boardContainer.GetComponentInParent<Canvas>();
        if (this.parentCanvas == null)
            this.parentCanvas = Object.FindAnyObjectByType<Canvas>();
    }

    private void Update()
    {
        if (isDragging)
        {
            // Lerp mượt mà vị trí bay theo tay/chuột
            transform.position = Vector3.Lerp(transform.position, targetWorldPos, Time.deltaTime * 32f);

            // Quán tính nghiêng (Tilt) theo tốc độ kéo ngang
            float deltaX = targetWorldPos.x - previousDragPos.x;
            targetTiltAngle = Mathf.Clamp(-deltaX * 0.05f, -10f, 10f);
            previousDragPos = targetWorldPos;

            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(0, 0, targetTiltAngle), Time.deltaTime * 18f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (game != null && game.IsGameOver) return;
        if (IsPlaced) return;
        if (returnRoutine != null) StopCoroutine(returnRoutine);

        isDragging = true;
        transform.SetParent(parentCanvas.transform, true);
        transform.SetAsLastSibling();

        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        UpdateTargetPosition(eventData);
        transform.position = targetWorldPos;
        previousDragPos = targetWorldPos;

        // Hiệu ứng nhấc lên đàn hồi (Elastic Punch Scale)
        StartCoroutine(ScaleToRoutine(dragScale, 0.12f));

        game.UpdateDragPreview(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || (game != null && game.IsGameOver)) return;
        UpdateTargetPosition(eventData);
        game.UpdateDragPreview(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        bool placed = game.TryPlaceShape(this);

        if (!placed)
        {
            // Trả về slot ban đầu mượt mà (Smooth Easing Return)
            returnRoutine = StartCoroutine(SmoothReturnRoutine());
        }
    }

    private void UpdateTargetPosition(PointerEventData eventData)
    {
        Vector2 screenPoint = eventData.position + new Vector2(0, dragYOffset);
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentCanvas.GetComponent<RectTransform>(), screenPoint, eventData.pressEventCamera, out Vector3 worldPos))
        {
            targetWorldPos = worldPos;
        }
    }

    private Coroutine returnRoutine;
    private IEnumerator SmoothReturnRoutine()
    {
        Vector3 startWorldPos = transform.position;
        Quaternion startRot = transform.localRotation;
        Vector3 startScale = rootRect.localScale;

        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Easing Out Cubic
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            transform.position = Vector3.Lerp(startWorldPos, originalSlot.position, ease);
            transform.localRotation = Quaternion.Lerp(startRot, Quaternion.identity, ease);
            rootRect.localScale = Vector3.Lerp(startScale, Vector3.one * slotScale, ease);
            yield return null;
        }

        transform.SetParent(originalSlot, false);
        rootRect.anchoredPosition = Vector2.zero;
        transform.localRotation = Quaternion.identity;
        rootRect.localScale = Vector3.one * slotScale;
        returnRoutine = null;
    }

    private IEnumerator ScaleToRoutine(float targetScale, float duration)
    {
        float elapsed = 0f;
        Vector3 initialScale = rootRect.localScale;
        Vector3 target = Vector3.one * targetScale;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rootRect.localScale = Vector3.Lerp(initialScale, target, t);
            yield return null;
        }
        rootRect.localScale = target;
    }

    public void SetDimmed(bool dimmed)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = dimmed ? 0.38f : 1.0f;
        }

        if (blockRects != null)
        {
            Color targetColor = dimmed ? new Color(0.45f, 0.45f, 0.45f, 0.75f) : Color.white;
            foreach (var br in blockRects)
            {
                if (br != null)
                {
                    Image img = br.GetComponent<Image>();
                    if (img != null) img.color = targetColor;
                }
            }
        }
    }
}
