using UnityEngine;

// Carried item (e.g. a key). Needs a Rigidbody2D, a physical collider and a trigger collider for pickup range.
[RequireComponent(typeof(Rigidbody2D))]
public class Pickupable2D : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [SerializeField] private string itemId = "key";

    private Rigidbody2D rb;

    public string ItemId => itemId;
    public PlayerInteract Holder { get; private set; }
    public bool IsHeld => Holder;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Interact(GameObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInteract player)) player.PickUp(this);
    }

    public void AttachTo(PlayerInteract holder, Vector2 localOffset)
    {
        Holder = holder;
        rb.simulated = false;
        rb.linearVelocity = Vector2.zero;
        transform.SetParent(holder.transform, false);
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;
    }

    public void Detach(Vector2 inheritVelocity)
    {
        Holder = null;
        transform.SetParent(null, true);
        rb.simulated = true;
        rb.linearVelocity = inheritVelocity;
    }
}
