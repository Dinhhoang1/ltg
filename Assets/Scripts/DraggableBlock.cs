using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableBlock : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
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
            transform.position = Vector3.Lerp(transform.position, targetWorldPos, Time.deltaTime * 32f);

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
