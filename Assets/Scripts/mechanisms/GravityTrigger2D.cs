using UnityEngine;

// Points the player's gravity at activeGravityDir while activated (e.g. from a Button2D target).
public class GravityTrigger2D : MonoBehaviour, IActivationReceiver
{
    [Header("Target")]
    [Tooltip("Empty = first PlayerController in the scene")]
    [SerializeField] private PlayerController player;

    [Header("Gravity")]
    [SerializeField] private Vector2 activeGravityDir = Vector2.up;
    [SerializeField] private bool restoreOnDeactivate = true;

    private Vector2 previousDir;
    private bool active;

    public void SetActivated(bool activated)
    {
        if (!player) player = FindFirstObjectByType<PlayerController>();
        if (!player || activated == active) return;
        active = activated;
        if (activated)
        {
            previousDir = player.gravityDir;
            player.SetGravityDirection(activeGravityDir);
        }
        else if (restoreOnDeactivate) player.SetGravityDirection(previousDir);
    }
}
