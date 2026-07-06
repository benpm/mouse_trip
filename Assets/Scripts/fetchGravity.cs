using UnityEngine;

public class fetchGravity : MonoBehaviour
{
    [SerializeField] PlayerController playerController;

    // Update is called once per frame

    //For non-player objects
    void Update()
    {
        Physics2D.gravity = playerController.gravityDir * 9.81f;
    }
}
