using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private readonly List<IInteractable> interactables = new List<IInteractable>();

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            InteractWithClosest();
        }
    }

    private void InteractWithClosest()
    {
        if (interactables.Count <= 0) return;

        interactables[0].Interact(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();

        if (interactable != null && !interactables.Contains(interactable))
        {
            interactables.Add(interactable);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();

        if (interactable != null)
        {
            interactables.Remove(interactable);
        }
    }
}