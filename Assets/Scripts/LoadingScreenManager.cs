using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý thanh Loading chạy mượt mà theo chuẩn game và tự động chuyển sang Main scene khi tải xong.
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    [Header("=== CẤU HÌNH CHUYỂN SCENE ===")]
    [Tooltip("Tên Scene sẽ chuyển đến sau khi load xong (mặc định là 'Main scene')")]
    public string targetSceneName = "Main scene";

    [Header("=== GIAO DIỆN THANH LOADING ===")]
    [Tooltip("Kéo đối tượng Image màu xanh 'load prog' vào đây (nếu để trống script sẽ tự động tìm kiếm)")]
    public Image progressBarImage;

    [Tooltip("Kéo đối tượng Text (TMP) hiển thị chữ 'Loading' vào đây (tùy chọn)")]
    public TextMeshProUGUI loadingTextTMP;
    public Text legacyLoadingText;

    [Header("=== CÀI ĐẶT THỜI GIAN & HIỆU ỨNG ===")]
    [Tooltip("Thời gian loading chạy (giây)")]
    [Range(1f, 8f)]
    public float loadingDuration = 2.2f;

    [Tooltip("Hiệu ứng chữ chấm động: Loading -> Loading. -> Loading.. -> Loading...")]
    public bool animateDots = true;

    [Tooltip("Hiển thị thêm số phần trăm (VD: Loading... 75%)")]
    public bool showPercentage = false;

    private float currentProgress = 0f;

    private void Awake()
    {
        AutoDetectUI();
        PrepareProgressBar();
    }

    private void Start()
    {
        StartCoroutine(LoadSceneRoutine());
    }

    /// <summary>
    /// Tự động tìm kiếm các phần tử UI trong scene nếu người dùng chưa kéo thủ công
    /// </summary>
    private void AutoDetectUI()
    {
        // 1. Tự tìm Image của thanh tiến trình xanh ('load prog')
        if (progressBarImage == null)
        {
            progressBarImage = GetComponent<Image>();

            if (progressBarImage == null)
            {
                GameObject progObj = GameObject.Find("load prog");
                if (progObj != null)
                {
                    progressBarImage = progObj.GetComponent<Image>();
                }
            }

            if (progressBarImage == null)
            {
                foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
                {
                    string n = img.gameObject.name.ToLower();
                    if (n.Contains("prog") || n.Contains("fill") || n.Contains("bar"))
                    {
                        progressBarImage = img;
                        break;
                    }
                }
            }
        }

        // 2. Tự tìm TextMeshPro hiển thị Loading ('Text (TMP)')
        if (loadingTextTMP == null && legacyLoadingText == null)
        {
            loadingTextTMP = GetComponentInChildren<TextMeshProUGUI>();
            if (loadingTextTMP == null)
            {
                GameObject textObj = GameObject.Find("Text (TMP)");
                if (textObj != null)
                {
                    loadingTextTMP = textObj.GetComponent<TextMeshProUGUI>();
                }
            }

            if (loadingTextTMP == null)
            {
                foreach (TextMeshProUGUI tmp in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                {
                    if (tmp.gameObject.name.ToLower().Contains("text") || tmp.text.ToLower().Contains("load"))
                    {
                        loadingTextTMP = tmp;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Cấu hình Image sang chế độ Filled để thanh chạy từ trái sang phải
    /// </summary>
    private void PrepareProgressBar()
    {
        if (progressBarImage != null)
        {
            progressBarImage.type = Image.Type.Filled;
            progressBarImage.fillMethod = Image.FillMethod.Horizontal;
            progressBarImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressBarImage.fillAmount = 0f;
        }
        else
        {
            Debug.LogWarning("[LoadingScreenManager] Chưa tìm thấy Image thanh tiến trình ('load prog')!");
        }
    }

    private IEnumerator LoadSceneRoutine()
    {
        string sceneToLoad = !string.IsNullOrWhiteSpace(targetSceneName) ? targetSceneName : "Main scene";

        // Bắt đầu tải ngầm Scene
        AsyncOperation asyncOp = null;
        try
        {
            asyncOp = SceneManager.LoadSceneAsync(sceneToLoad);
            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LoadingScreenManager] Không thể nạp ngầm Scene '{sceneToLoad}': {ex.Message}");
        }

        float elapsed = 0f;
        float dotTimer = 0f;
        int dotCount = 0;

        // Vòng lặp chạy thanh tiến trình mượt mà
        while (elapsed < loadingDuration || (asyncOp != null && asyncOp.progress < 0.9f))
        {
            elapsed += Time.deltaTime;
            dotTimer += Time.deltaTime;

            // Hiệu ứng đổi dấu chấm: Loading -> Loading. -> Loading.. -> Loading...
            if (dotTimer >= 0.32f)
            {
                dotTimer = 0f;
                dotCount = (dotCount + 1) % 4;
            }

            // Tính toán tiến trình giả lập tự nhiên theo thời gian
            float timeRatio = Mathf.Clamp01(elapsed / loadingDuration);
            // Sử dụng SmoothStep để đầu và cuối mượt mà
            float targetProgress = Mathf.SmoothStep(0f, 1f, timeRatio);

            // Đồng bộ với tiến trình nạp thực tế
            if (asyncOp != null)
            {
                float realProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
                targetProgress = Mathf.Min(targetProgress, realProgress);
            }

            // Tăng dần tiến trình mượt mà, không bị giật cục
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * 1.5f);
            UpdateUI(currentProgress, dotCount);

            yield return null;
        }

        // Đẩy tiến trình về tròn 100%
        while (currentProgress < 1.0f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, 1.0f, Time.deltaTime * 3.0f);
            UpdateUI(currentProgress, 3);
            yield return null;
        }

        UpdateUI(1.0f, 3);

        // Chờ nhẹ 0.25 giây để người chơi nhìn thấy thanh đã đầy 100%
        yield return new WaitForSeconds(0.25f);

        Debug.Log($"<color=green>[LoadingScreenManager] Đã tải xong 100%! Đang chuyển vào: {sceneToLoad}</color>");

        // Kích hoạt chuyển thẳng vào Scene chính
        if (asyncOp != null)
        {
            asyncOp.allowSceneActivation = true;
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void UpdateUI(float progress, int dotCount)
    {
        // 1. Cập nhật độ đầy của thanh xanh
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = progress;
        }

        // 2. Cập nhật chữ Loading
        string dots = animateDots ? new string('.', dotCount) : "...";
        string displayText = $"Loading{dots}";

        if (showPercentage)
        {
            int percent = Mathf.RoundToInt(progress * 100f);
            displayText = $"Loading{dots} {percent}%";
        }

        if (loadingTextTMP != null)
        {
            loadingTextTMP.text = displayText;
        }
        if (legacyLoadingText != null)
        {
            legacyLoadingText.text = displayText;
        }
    }
}

