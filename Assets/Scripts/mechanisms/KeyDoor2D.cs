using UnityEngine;
using UnityEngine.Events;

// Opens when a Pickupable2D with requiredItemId enters its trigger (carried or dropped);
// once open, interacting enters the door via the attached LevelPortal2D.
public class KeyDoor2D : MonoBehaviour, IInteractable
{
    [Header("Lock")]
    [SerializeField] private string requiredItemId = "key";
    [SerializeField] private bool consumeKey = true;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    [Header("Enter")]
    [Tooltip("Empty = LevelPortal2D on this object, if any")]
    [SerializeField] private LevelPortal2D portal;

    [Header("Events")]
    public UnityEvent onOpened = new();
    public UnityEvent onEntered = new();

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!portal) portal = GetComponent<LevelPortal2D>();
        if (spriteRenderer && closedSprite) spriteRenderer.sprite = closedSprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsOpen) return;
        var rb = other.attachedRigidbody;
        var key = rb ? rb.GetComponentInChildren<Pickupable2D>() : other.GetComponent<Pickupable2D>();
        if (key && key.ItemId == requiredItemId) Open(key);
    }

    public void Open(Pickupable2D key)
    {
        if (IsOpen) return;
        IsOpen = true;
        if (key)
        {
            if (key.IsHeld) key.Holder.Drop();
            if (consumeKey) Destroy(key.gameObject);
        }
        if (spriteRenderer && openSprite) spriteRenderer.sprite = openSprite;
        onOpened.Invoke();
    }

    public void Interact(GameObject interactor)
    {
        if (!IsOpen) return;
        onEntered.Invoke();
        if (portal) portal.LoadNextLevel();
    }
}
