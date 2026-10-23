using NUnit.Framework;
using UnityEngine;

public class DamageProfileTests
{
    DamageProfile Profile()
    {
        var p = ScriptableObject.CreateInstance<DamageProfile>();
        p.damageAmount = 2;
        p.instantKill = false;
        p.invincibleTimeAfterHit = 0.5f;
        p.knockback = new Vector2(4f, 5f);
        return p;
    }

    [TestCase(1f, 4f)]
    [TestCase(-1f, -4f)]
    public void Knockback_X_FollowsDirection(float dirX, float expectedX)
    {
        var info = Profile().CreateDamageInfo(null, new Vector2(dirX, 1f));
        Assert.AreEqual(expectedX, info.knockback.x);
        Assert.AreEqual(5f, info.knockback.y);
    }

    [Test]
    public void CopiesAmountKillAndInvincibility()
    {
        var src = new GameObject("src");
        var info = Profile().CreateDamageInfo(src, Vector2.one);
        Assert.AreEqual(2, info.amount);
        Assert.IsFalse(info.instantKill);
        Assert.AreEqual(0.5f, info.invincibleTime);
        Assert.AreSame(src, info.source);
        Object.DestroyImmediate(src);
    }
}
