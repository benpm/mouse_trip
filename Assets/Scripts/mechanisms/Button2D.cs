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
        if (!IsInTriggerLayer(other.gameObject)) return;

        currentPressCount++;

        SendActivation(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsInTriggerLayer(other.gameObject)) return;

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

    private bool IsInTriggerLayer(GameObject target)
    {
        return (triggerLayer.value & (1 << target.layer)) != 0;
    }
}