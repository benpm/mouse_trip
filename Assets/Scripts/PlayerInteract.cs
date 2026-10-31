using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Carry")]
    [Tooltip("Local offset of a held Pickupable2D; rotates with the player's gravity frame")]
    [SerializeField] private Vector2 holdOffset = new Vector2(0.6f, 0.2f);

    private readonly List<IInteractable> interactables = new List<IInteractable>();
    private Rigidbody2D rb;

    public Pickupable2D Held { get; private set; }

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    private void Update()
    {
        if (Input.GetKeyDown(interactKey)) Interact();
    }

    // Interactables in range win over dropping, so E at a door enters it instead of dropping the key.
    public void Interact()
    {
        interactables.RemoveAll(i => i == null || (i is MonoBehaviour mb && !mb));
        if (interactables.Count > 0) interactables[0].Interact(gameObject);
        else if (Held) Drop();
    }

    public void PickUp(Pickupable2D item)
    {
        if (!item || item == Held) return;
        if (Held) Drop();
        interactables.Remove(item);
        item.AttachTo(this, holdOffset);
        Held = item;
    }

    public void Drop()
    {
        if (!Held) return;
        Held.Detach(rb ? rb.linearVelocity : Vector2.zero);
        Held = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && interactable != (IInteractable)Held && !interactables.Contains(interactable))
            interactables.Add(interactable);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null) interactables.Remove(interactable);
    }
}
