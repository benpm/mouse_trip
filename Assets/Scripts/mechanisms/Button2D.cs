using UnityEngine;

public class Button2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private MonoBehaviour[] targets;

    [Header("Layer")]
    [SerializeField] private LayerMask triggerLayer;

    [Header("Mode")]
    [SerializeField] private bool stayActivatedAfterPress = false;

    private int currentPressCount = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!CanPress(other)) return;

        currentPressCount++;

        SendActivation(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!CanPress(other)) return;

        currentPressCount--;

        if (stayActivatedAfterPress) return;

        if (currentPressCount <= 0)
        {
            currentPressCount = 0;
            SendActivation(false);
        }
    }

    private void SendActivation(bool active)
    {
        foreach (MonoBehaviour target in targets)
        {
            if (target is IActivationReceiver receiver)
            {
                receiver.SetActivated(active);
            }
        }
    }

    // Only dynamic bodies press; static/kinematic colliders (e.g. the tilemap the button rests on) also raise trigger events.
    private bool CanPress(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        return rb && rb.bodyType == RigidbodyType2D.Dynamic && (triggerLayer.value & (1 << other.gameObject.layer)) != 0;
    }
}