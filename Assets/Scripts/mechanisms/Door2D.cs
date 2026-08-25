using UnityEngine;

public class Door2D : MonoBehaviour, IActivationReceiver
{
    [Header("Door Move")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);
    [SerializeField] private float moveSpeed = 5f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Vector3 targetPosition;

    private void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
        targetPosition = closedPosition;
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
    }

    public void SetActivated(bool active)
    {
        targetPosition = active ? openPosition : closedPosition;
    }
}