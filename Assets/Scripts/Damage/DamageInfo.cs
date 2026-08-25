using UnityEngine;

public struct DamageInfo
{
    public GameObject source;
    public int amount;
    public bool instantKill;
    public float invincibleTime;
    public Vector2 knockback;
}