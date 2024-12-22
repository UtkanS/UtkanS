using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;


[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class DragDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{

    /// <summary>The currently dragged object.</summary>
    public static DragDrop Instance { get; private set; }

    [Header("Movement")]
    [Tooltip("Restricts movement")]
    public bool canMove = true;
    [SerializeField] private bool canMoveX = true;
    [SerializeField] private bool canMoveY = true;

    [Header("Alpha")]
    [Tooltip("By lowering the alpha, enhance the feedback and improve visibility.")]
    public bool useAlpha = true;
    [Range(0, 1)] public float dragAlpha = 0.6f;
    private float realAlpha;

    [Header("Events")]
    public Events events;

    private Canvas canvas;
    private RectTransform rect;
    private CanvasGroup group;


    private void Awake()
    {
        rect = gameObject.GetComponent<RectTransform>();
        canvas = gameObject.GetComponentInParent<Canvas>();
        group = gameObject.GetComponentInParent<CanvasGroup>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanMove()) return;
        events.OnPointerDown?.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanMove()) return;

        Instance = this;
        events.OnBeginDrag?.Invoke();
        group.blocksRaycasts = false;

        if (useAlpha)
        {
            realAlpha = group.alpha;
            group.alpha = dragAlpha;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!CanMove()) return;

        events.OnEndDrag?.Invoke();
        Instance = null;
        group.blocksRaycasts = true;

        if (useAlpha)
            group.alpha = realAlpha;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanMove()) return;
        Vector2 delta = eventData.delta / canvas.scaleFactor;
        rect.anchoredPosition += new Vector2(canMoveX ? delta.x : 0, canMoveY ? delta.y : 0);
    }

    public bool CanMove()
        => canMove && (canMoveX || canMoveY);

    [System.Serializable] public class Events
    {
        public UnityEvent OnPointerDown;
        public UnityEvent OnBeginDrag;
        public UnityEvent OnEndDrag;

        public void RemoveAllListeners()
        {
            OnPointerDown.RemoveAllListeners();
            OnBeginDrag.RemoveAllListeners();
            OnEndDrag.RemoveAllListeners();
        }
    }

}

