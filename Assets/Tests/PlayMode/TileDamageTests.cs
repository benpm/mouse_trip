using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

// Drops a player onto surfaces and checks PlayerController's tile contact handling end to end.
public class TileDamageTests
{
    readonly List<Object> created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created) if (o != null) Object.Destroy(o);
        created.Clear();
    }

    T Track<T>(T o) where T : Object { created.Add(o); return o; }

    Sprite UnitSprite()
    {
        var tex = Track(new Texture2D(1, 1));
        return Track(Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f));
    }

    Tilemap Floor(GameplayTile tile)
    {
        var grid = Track(new GameObject("grid", typeof(Grid)));
        var tmGo = Track(new GameObject("tilemap", typeof(Tilemap), typeof(TilemapCollider2D)));
        tmGo.transform.SetParent(grid.transform, false);
        var tm = tmGo.GetComponent<Tilemap>();
        for (int x = -3; x <= 3; x++) tm.SetTile(new Vector3Int(x, 0, 0), tile);
        return tm;
    }

    GameplayTile Tile(bool damaging)
    {
        var t = Track(ScriptableObject.CreateInstance<GameplayTile>());
        t.sprite = UnitSprite();
        t.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.Grid;
        t.solid = true;
        t.damagesPlayer = damaging;
        t.damage = 1;
        return t;
    }

    (PlayerHealth health, Rigidbody2D rb) Player(Vector2 pos, DamageProfile profile)
    {
        var go = Track(new GameObject("player"));
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        var col = go.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.8f, 1.6f);
        var health = go.AddComponent<PlayerHealth>();
        var pc = go.AddComponent<PlayerController>();
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(pc);
        so.FindProperty("tileDamageProfile").objectReferenceValue = profile;
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
        return (health, rb);
    }

    IEnumerator WaitForLanding(Rigidbody2D rb, int maxFixedSteps = 300)
    {
        var contacts = new ContactPoint2D[4];
        for (int i = 0; i < maxFixedSteps; i++)
        {
            yield return new WaitForFixedUpdate();
            if (rb.GetContacts(contacts) > 0) { yield return new WaitForFixedUpdate(); yield break; }
        }
        Assert.Fail("player never touched anything");
    }

    [UnityTest]
    public IEnumerator DamagingTile_ReducesHealth_AndKnocksBack()
    {
        var profile = Track(ScriptableObject.CreateInstance<DamageProfile>());
        profile.knockback = new Vector2(4f, 5f);
        Floor(Tile(damaging: true));
        var (health, rb) = Player(new Vector2(0.5f, 3f), profile);
        int reported = -1;
        health.onHealthChanged.AddListener((c, m) => reported = c);

        yield return WaitForLanding(rb);

        Assert.AreEqual(2, reported, "GameplayTile.damage should reach PlayerHealth");
        Assert.Greater(Vector2.Dot(rb.linearVelocity, Vector2.up), 0f, "knockback should push the player up");
    }

    [UnityTest]
    public IEnumerator HarmlessTile_LeavesHealthAlone()
    {
        var profile = Track(ScriptableObject.CreateInstance<DamageProfile>());
        Floor(Tile(damaging: false));
        var (health, rb) = Player(new Vector2(0.5f, 3f), profile);
        int calls = 0;
        health.onHealthChanged.AddListener((c, m) => calls++);

        yield return WaitForLanding(rb);
        yield return new WaitForFixedUpdate();

        Assert.AreEqual(0, calls);
    }

    [UnityTest]
    public IEnumerator NonTilemapCollider_DoesNotThrow()
    {
        var platform = Track(new GameObject("platform", typeof(BoxCollider2D)));
        platform.GetComponent<BoxCollider2D>().size = new Vector2(10f, 1f);
        var (health, rb) = Player(new Vector2(0f, 3f), null);
        int calls = 0;
        health.onHealthChanged.AddListener((c, m) => calls++);

        yield return WaitForLanding(rb);
        yield return new WaitForFixedUpdate();

        Assert.AreEqual(0, calls);
        // An exception inside FixedUpdate would already have failed the test via the log assert.
    }
}
