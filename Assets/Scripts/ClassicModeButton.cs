using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Script quản lý nút Classic ở màn hình chính (Main scene)
/// Khi người chơi nhấn vào nút, sẽ có hiệu ứng nảy đàn hồi (Punch effect) và chuyển sang scene Classicmode.
/// </summary>
public class ClassicModeButton : MonoBehaviour
{
    [Header("=== CẤU HÌNH CHUYỂN SCENE ===")]
    [Tooltip("Tên Scene sẽ tải khi bấm nút (mặc định là 'Classicmode')")]
    public string targetSceneName = "Classicmode";

    [Header("=== GIAO DIỆN NÚT BẤM (TÙY CHỌN) ===")]
    [Tooltip("Kéo đối tượng nút Classic vào đây. Nếu gắn trực tiếp script này vào nút hoặc để trống, script sẽ tự động tìm kiếm đối tượng 'mode 2' trong Scene.")]
    public GameObject classicButton;

    [Header("=== HIỆU ỨNG & ÂM THANH ===")]
    [Tooltip("Bật hiệu ứng nảy nút khi click")]
    public bool enablePunchEffect = true;
    [Tooltip("Âm thanh khi bấm nút (tùy chọn)")]
    public AudioClip clickSound;
    public AudioSource audioSource;

    private bool isTransitioning = false;

    private void Awake()
    {
        SetupButton();
    }

    private void Start()
    {
        SetupButton();
    }

    /// <summary>
    /// Tự động tìm kiếm và cài đặt sự kiện click cho nút Classic
    /// </summary>
    private void SetupButton()
    {
        // 1. Nếu chưa gán classicButton:
        if (classicButton == null)
        {
            // Nếu chính GameObject này chứa Image hoặc Button (gắn trực tiếp vào nút mode 2)
            if (GetComponent<Image>() != null || GetComponent<Button>() != null)
            {
                classicButton = gameObject;
            }
            else
            {
                // Tự động tìm đối tượng mang tên 'mode 2' hoặc 'Classic'
                Transform found = transform.Find("mode 2");
                if (found == null) found = transform.Find("Classic");
                if (found == null)
                {
                    GameObject obj = GameObject.Find("mode 2");
                    if (obj == null) obj = GameObject.Find("Classic");
                    if (obj != null) found = obj.transform;
                }

                if (found != null)
                {
                    classicButton = found.gameObject;
                }
            }
        }

        // 2. Cài đặt component Button và sự kiện OnClick
        if (classicButton != null)
        {
            Button btn = classicButton.GetComponent<Button>();
            if (btn == null)
            {
                btn = classicButton.AddComponent<Button>();
            }

            // Đảm bảo Image có bật raycastTarget để nhận click chuột / chạm ngón tay
            Image img = classicButton.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = true;
            }

            // Gắn sự kiện OnClick an toàn
            btn.onClick.RemoveListener(OnClassicButtonClicked);
            btn.onClick.AddListener(OnClassicButtonClicked);
        }
        else
        {
            Debug.LogWarning("[ClassicModeButton] Chưa tìm thấy nút Classic! Hãy kéo đối tượng 'mode 2' vào ô Classic Button trên Inspector.");
        }
    }

    /// <summary>
    /// Hàm xử lý khi người chơi bấm nút Classic
    /// Có thể gọi qua code hoặc gắn trực tiếp vào sự kiện OnClick trên Inspector
    /// </summary>
    public void OnClassicButtonClicked()
    {
        if (isTransitioning) return;

        // Phát âm thanh click nếu có
        if (clickSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clickSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            }
        }

        if (enablePunchEffect && classicButton != null)
        {
            StartCoroutine(PunchAndLoadRoutine());
        }
        else
        {
            LoadSceneDirectly();
        }
    }

    private IEnumerator PunchAndLoadRoutine()
    {
        isTransitioning = true;

        RectTransform rect = classicButton.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 originalScale = rect.localScale;

            // 1. Thu nhỏ nhẹ (nhấn nút)
            float elapsed = 0f;
            float duration = 0.07f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                rect.localScale = Vector3.Lerp(originalScale, originalScale * 0.92f, elapsed / duration);
                yield return null;
            }

            // 2. Bật nảy lên một chút
            elapsed = 0f;
            duration = 0.09f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                rect.localScale = Vector3.Lerp(originalScale * 0.92f, originalScale * 1.04f, elapsed / duration);
                yield return null;
            }

            rect.localScale = originalScale;
        }

        // Chờ nhẹ 0.05s để mắt người kịp thấy hiệu ứng nảy
        yield return new WaitForSecondsRealtime(0.05f);

        LoadSceneDirectly();
    }

    private void LoadSceneDirectly()
    {
        string sceneToLoad = !string.IsNullOrWhiteSpace(targetSceneName) ? targetSceneName : "Classicmode";
        Debug.Log($"<color=green>[ClassicModeButton] Nút Classic được bấm! Đang chuyển sang Scene: {sceneToLoad}</color>");

        try
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ClassicModeButton] Không thể tải Scene '{sceneToLoad}': {ex.Message}");
        }
    }
}

