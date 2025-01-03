using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Handles drag-and-drop functionality for UI elements.
/// Requires RectTransform and CanvasGroup components.
/// Implements Unity's event interfaces for drag-and-drop.
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class DragDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    /// <summary>The currently dragged object instance.</summary>
    public static DragDrop Instance { get; private set; }

    [Header("Movement")]
    [Tooltip("Allows or restricts movement during drag.")]
    public bool canMove = true; // Determines if dragging is enabled.
    [SerializeField] private bool canMoveX = true; // Determines if movement is allowed along the X-axis.
    [SerializeField] private bool canMoveY = true; // Determines if movement is allowed along the Y-axis.

    [Header("Alpha")]
    [Tooltip("Enable or disable alpha changes during drag for feedback.")]
    public bool useAlpha = true; // Enables alpha transparency during dragging.
    [Range(0, 1)] public float dragAlpha = 0.6f; // Transparency value when dragging.
    private float realAlpha; // Stores the original alpha value.

    [Header("Events")]
    public Events events; // Custom UnityEvents triggered during drag-and-drop actions.

    private Canvas canvas; // Reference to the parent Canvas.
    private RectTransform rect; // Reference to this object's RectTransform.
    private CanvasGroup group; // Reference to the CanvasGroup for controlling interactions and alpha.

    /// <summary>
    /// Initializes component references on Awake.
    /// </summary>
    private void Awake()
    {
        rect = gameObject.GetComponent<RectTransform>();
        canvas = gameObject.GetComponentInParent<Canvas>();
        group = gameObject.GetComponentInParent<CanvasGroup>();
    }

    /// <summary>
    /// Triggered when the pointer is pressed down on the object.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanMove()) 
            return;
            
        events.OnPointerDown?.Invoke();
    }

    /// <summary>
    /// Triggered at the start of a drag action.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanMove()) 
            return;

        Instance = this; // Set the current instance as the active dragged object.
        events.OnBeginDrag?.Invoke();
        group.blocksRaycasts = false; // Disable raycasts to allow dragging over other elements.

        if (useAlpha)
        {
            realAlpha = group.alpha; // Store the original alpha.
            group.alpha = dragAlpha; // Set the alpha to the drag value for feedback.
        }
    }

    /// <summary>
    /// Triggered at the end of a drag action.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!CanMove()) 
            return;

        events.OnEndDrag?.Invoke();
        Instance = null; // Clear the active dragged object.
        group.blocksRaycasts = true; // Re-enable raycasts.

        if (useAlpha)
            group.alpha = realAlpha; // Restore the original alpha.
    }

    /// <summary>
    /// Triggered during the drag action.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnDrag(PointerEventData eventData)
    {
        if (!CanMove()) 
            return;

        // Calculate the movement delta, which is adjusted for the canvas scale.
        Vector2 delta = eventData.delta / canvas.scaleFactor;

        // Update the object's anchored position based on allowed movement directions.
        rect.anchoredPosition += new Vector2(canMoveX ? delta.x : 0, canMoveY ? delta.y : 0);
    }

    /// <summary>
    /// Checks if the object is allowed to move during drag.
    /// </summary>
    /// <returns>True if movement is allowed; otherwise, false.</returns>
    public bool CanMove()
        => canMove && (canMoveX || canMoveY);

    /// <summary>
    /// Container for UnityEvents triggered during drag-and-drop actions.
    /// </summary>
    [System.Serializable]
    public class Events
    {
        public UnityEvent OnPointerDown; // Triggered when the pointer presses down.
        public UnityEvent OnBeginDrag; // Triggered at the start of a drag.
        public UnityEvent OnEndDrag; // Triggered at the end of a drag.

        /// <summary>
        /// Removes all listeners from the events.
        /// </summary>
        public void RemoveAllListeners()
        {
            OnPointerDown.RemoveAllListeners();
            OnBeginDrag.RemoveAllListeners();
            OnEndDrag.RemoveAllListeners();
        }
    }
}
