using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

// Jumps a player off a tilemap floor and checks the arc is a jump, not a launch into orbit.
public class PlayerJumpTests
{
    readonly List<Object> created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created) if (o != null) Object.Destroy(o);
        created.Clear();
    }

    T Track<T>(T o) where T : Object { created.Add(o); return o; }

    void Floor()
    {
        var tex = Track(new Texture2D(1, 1));
        var t = Track(ScriptableObject.CreateInstance<GameplayTile>());
        t.sprite = Track(Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f));
        t.colliderType = Tile.ColliderType.Grid;
        t.solid = true;
        var grid = Track(new GameObject("grid", typeof(Grid)));
        var tmGo = Track(new GameObject("tilemap", typeof(Tilemap), typeof(TilemapCollider2D)));
        tmGo.transform.SetParent(grid.transform, false);
        var tm = tmGo.GetComponent<Tilemap>();
        for (int x = -3; x <= 3; x++) tm.SetTile(new Vector3Int(x, 0, 0), t);
    }

    Rigidbody2D Player(float jumpForce, float rbGravityScale)
    {
        var go = Track(new GameObject("player"));
        go.transform.position = new Vector2(0.5f, 3f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = rbGravityScale;
        var col = go.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.5f, 1f);
        var pc = go.AddComponent<PlayerController>();
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(pc);
        so.FindProperty("jumpForce").floatValue = jumpForce;
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
        return rb;
    }

    IEnumerator WaitUntilResting(Rigidbody2D rb, int maxFixedSteps = 300)
    {
        var contacts = new ContactPoint2D[4];
        for (int i = 0; i < maxFixedSteps; i++)
        {
            yield return new WaitForFixedUpdate();
            if (rb.GetContacts(contacts) > 0 && Mathf.Abs(rb.linearVelocity.y) < 1.5f) yield break;
        }
        Assert.Fail("player never came to rest on the floor");
    }

    [UnityTest]
    public IEnumerator Jump_RisesAFewUnits_ThenLands()
    {
        Floor();
        var rb = Player(jumpForce: 18f, rbGravityScale: 0f);
        yield return WaitUntilResting(rb);
        yield return new WaitForFixedUpdate();
        float restY = rb.position.y;

        // Legacy Input can't be faked from a test: inject the buffered jump directly.
        var pc = rb.GetComponent<PlayerController>();
        typeof(PlayerController).GetField("jumpBufferTimer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(pc, 1f);

        float peak = restY;
        bool landed = false;
        for (int i = 0; i < 200; i++)
        {
            yield return new WaitForFixedUpdate();
            peak = Mathf.Max(peak, rb.position.y);
            if (i > 5 && rb.position.y - restY < 0.05f && Mathf.Abs(rb.linearVelocity.y) < 1.5f) { landed = true; break; }
        }

        float height = peak - restY;
        Assert.Greater(height, 1f, "jump too low");
        Assert.Less(height, 8f, "jump launched the player off screen");
        Assert.IsTrue(landed, "player never came back down");
    }
}
