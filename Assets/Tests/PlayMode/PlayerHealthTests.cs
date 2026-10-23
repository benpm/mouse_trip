using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerHealthTests
{
    GameObject go;
    PlayerHealth health;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        go = new GameObject("player");
        health = go.AddComponent<PlayerHealth>();
        yield return null;
    }

    [TearDown]
    public void TearDown() => Object.Destroy(go);

    // Long invincibility so the second hit in a test lands inside the window. Never lethal (maxHealth = 3).
    static DamageInfo Hit(int amount) => new DamageInfo { amount = amount, invincibleTime = 10f };

    [Test]
    public void Damage_ReducesHealth_AndFiresEvent()
    {
        int cur = -1, max = -1;
        health.onHealthChanged.AddListener((c, m) => { cur = c; max = m; });

        Assert.IsTrue(health.TakeDamage(Hit(1)));
        Assert.AreEqual(2, cur);
        Assert.AreEqual(3, max);
    }

    [Test]
    public void Damage_DuringInvincibility_IsRejected()
    {
        int calls = 0;
        health.onHealthChanged.AddListener((c, m) => calls++);

        Assert.IsTrue(health.TakeDamage(Hit(1)));
        Assert.IsFalse(health.TakeDamage(Hit(1)));
        Assert.AreEqual(1, calls);
    }
}
