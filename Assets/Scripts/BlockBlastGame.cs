using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class BlockBlastGame : MonoBehaviour
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

    [Header("=== 5. ÂM THANH & NHẠC NỀN ===")]
    public AudioSource audioSource;
    public AudioClip placeSound;
    [Range(0f, 1f)] [Tooltip("Âm lượng tiếng đặt khối")]
    public float placeSoundVolume = 1.0f;
    public AudioClip clearSound;
    [Range(0f, 1f)] [Tooltip("Âm lượng tiếng ăn 1 hàng")]
    public float clearSoundVolume = 0.8f;
    public AudioClip comboSound;
    [Range(0f, 1f)] [Tooltip("Âm lượng tiếng ăn Combo")]
    public float comboSoundVolume = 0.9f;
    public AudioClip gameOverSound;
    [Range(0f, 1f)] [Tooltip("Âm lượng tiếng thua cuộc")]
    public float gameOverSoundVolume = 0.8f;
    public AudioClip buttonClickSound;
    [Range(0f, 1f)] [Tooltip("Âm lượng tiếng bấm nút")]
    public float buttonClickVolume = 0.7f;
    public AudioClip bgmMusic;
    [Range(0f, 1f)] [Tooltip("Âm lượng nhạc nền BGM")]
    public float bgmVolume = 0.5f;

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

    public const int GridSize = 8;
    private Cell[,] gridCells = new Cell[GridSize, GridSize];
    private float cellSize;
    private float topLeftX;
    private float topLeftY;
    private Vector2 originalBoardAnchoredPos;
    private RectTransform effectsContainer;

    private int currentScore = 0;
    private int highScore = 0;
    private int comboStreak = 0;
    private bool isGameOver = false;

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

        AutoDetectGameOverElements();
        SetupButtonListeners();
        HideGameOverUI();
        InitializeBoard();
        RespawnAllThreeShapes();

        if (bgmMusic != null)
        {
            AudioSource bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.clip = bgmMusic;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }
    }

    private void PlaySound(AudioClip clip, float volumeScale = 1.0f)
    {
        if (clip == null) return;
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.PlayOneShot(clip, volumeScale);
    }
}
