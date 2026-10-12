using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClassicModeButton : MonoBehaviour
{
    [Header("CẤU HÌNH CHUYỂN SCENE")]
    [Tooltip("Tên Scene sẽ tải khi bấm nút (mặc định là 'Classicmode')")]
    public string targetSceneName = "Classicmode";

    [Header("GIAO DIỆN NÚT BẤM (TÙY CHỌN)")]
    [Tooltip("Kéo đối tượng nút Classic vào đây. Nếu gắn trực tiếp script này vào nút hoặc để trống, script sẽ tự động tìm kiếm đối tượng 'mode 2' trong Scene.")]
    public GameObject classicButton;

    [Header("HIỆU ỨNG & ÂM THANH")]
    [Tooltip("Bật hiệu ứng nảy nút khi click")]
    public bool enablePunchEffect = true;
    [Tooltip("Âm thanh khi bấm nút (tùy chọn)")]
    public AudioClip clickSound;
    [Range(0f, 1f)] [Tooltip("Âm lượng tiếng bấm nút")]
    public float clickSoundVolume = 0.8f;
    public AudioSource audioSource;

    [Header("NHẠC NỀN MAIN SCENE")]
    [Tooltip("Nhạc nền cho màn hình chính (tùy chọn)")]
    public AudioClip mainBgmMusic;
    [Range(0f, 1f)] [Tooltip("Âm lượng nhạc nền Main Scene")]
    public float mainBgmVolume = 0.5f;

    private AudioSource bgmSource;
    private bool isTransitioning = false;

    private void Awake()
    {
        SetupButton();
    }

    private void Start()
    {
        SetupButton();

        if (mainBgmMusic != null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.clip = mainBgmMusic;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = mainBgmVolume;
            bgmSource.Play();
        }
    }

    private void SetupButton()
    {
        if (classicButton == null)
        {
            if (GetComponent<Image>() != null || GetComponent<Button>() != null)
            {
                classicButton = gameObject;
            }
            else
            {
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

        if (classicButton != null)
        {
            Button btn = classicButton.GetComponent<Button>();
            if (btn == null)
            {
                btn = classicButton.AddComponent<Button>();
            }

            Image img = classicButton.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = true;
            }

            btn.onClick.RemoveListener(OnClassicButtonClicked);
            btn.onClick.AddListener(OnClassicButtonClicked);
        }
        else
        {
            Debug.LogWarning("[ClassicModeButton] Chưa tìm thấy nút Classic! Hãy kéo đối tượng 'mode 2' vào ô Classic Button trên Inspector.");
        }
    }

    public void OnClassicButtonClicked()
    {
        if (isTransitioning) return;

        if (clickSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clickSound, clickSoundVolume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clickSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero, clickSoundVolume);
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

            float elapsed = 0f;
            float duration = 0.07f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                rect.localScale = Vector3.Lerp(originalScale, originalScale * 0.92f, elapsed / duration);
                yield return null;
            }

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
