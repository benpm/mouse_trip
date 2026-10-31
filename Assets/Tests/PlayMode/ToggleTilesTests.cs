using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public class ToggleTilesTests
{
    readonly List<Object> created = new List<Object>();
    GameplayTile outline, filled;

    [SetUp]
    public void SetUp()
    {
        outline = MakeTile(Tile.ColliderType.None);
        filled = MakeTile(Tile.ColliderType.Grid);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created) if (o != null) Object.Destroy(o);
        created.Clear();
    }

    T Track<T>(T o) where T : Object { created.Add(o); return o; }

    GameplayTile MakeTile(Tile.ColliderType ct)
    {
        var tex = Track(new Texture2D(1, 1));
        var t = Track(ScriptableObject.CreateInstance<GameplayTile>());
        t.sprite = Track(Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f));
        t.colliderType = ct;
        t.solid = ct == Tile.ColliderType.Grid;
        return t;
    }

    (Tilemap tm, ToggleTiles2D toggle) Build(bool follow, params (Vector3Int cell, bool filled)[] layout)
    {
        var grid = Track(new GameObject("grid", typeof(Grid)));
        var go = Track(new GameObject("tilemap", typeof(Tilemap), typeof(TilemapCollider2D)));
        go.transform.SetParent(grid.transform, false);
        var tm = go.GetComponent<Tilemap>();
        foreach (var (cell, f) in layout) tm.SetTile(cell, f ? filled : outline);
        var toggle = go.AddComponent<ToggleTiles2D>();
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(toggle);
        so.FindProperty("outlineTile").objectReferenceValue = outline;
        so.FindProperty("filledTile").objectReferenceValue = filled;
        so.FindProperty("followActivation").boolValue = follow;
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
        return (tm, toggle);
    }

    static bool HasCollider(Tilemap tm, Vector3Int cell)
    {
        var hits = new List<Collider2D>();
        Physics2D.OverlapPoint(tm.GetCellCenterWorld(cell), new ContactFilter2D().NoFilter(), hits);
        return hits.Contains(tm.GetComponent<TilemapCollider2D>());
    }

    [UnityTest]
    public IEnumerator Toggle_SwapsTileAndCollider_IgnoresDeactivate()
    {
        var c = new Vector3Int(1, 2, 0);
        var (tm, toggle) = Build(false, (c, false));
        yield return new WaitForFixedUpdate();
        Assert.IsFalse(HasCollider(tm, c));

        toggle.SetActivated(true);
        yield return new WaitForFixedUpdate();
        Assert.AreSame(filled, tm.GetTile(c));
        Assert.IsTrue(HasCollider(tm, c));

        toggle.SetActivated(false);
        yield return new WaitForFixedUpdate();
        Assert.AreSame(filled, tm.GetTile(c), "deactivation is ignored in toggle mode");

        toggle.SetActivated(true);
        yield return new WaitForFixedUpdate();
        Assert.AreSame(outline, tm.GetTile(c));
        Assert.IsFalse(HasCollider(tm, c));
    }

    [UnityTest]
    public IEnumerator MixedStart_InvertsTogether()
    {
        var a = new Vector3Int(0, 0, 0);
        var b = new Vector3Int(1, 0, 0);
        var (tm, toggle) = Build(false, (a, false), (b, true));
        toggle.Toggle();
        yield return new WaitForFixedUpdate();
        Assert.AreSame(filled, tm.GetTile(a));
        Assert.AreSame(outline, tm.GetTile(b));
        Assert.IsTrue(HasCollider(tm, a));
        Assert.IsFalse(HasCollider(tm, b));
    }

    [UnityTest]
    public IEnumerator Follow_MirrorsActivation()
    {
        var c = new Vector3Int(0, 0, 0);
        var (tm, toggle) = Build(true, (c, false));
        toggle.SetActivated(true);
        yield return new WaitForFixedUpdate();
        Assert.AreSame(filled, tm.GetTile(c));
        toggle.SetActivated(true);
        yield return new WaitForFixedUpdate();
        Assert.AreSame(filled, tm.GetTile(c), "repeated activation is idempotent");
        toggle.SetActivated(false);
        yield return new WaitForFixedUpdate();
        Assert.AreSame(outline, tm.GetTile(c));
        Assert.IsFalse(HasCollider(tm, c));
    }

    [UnityTest]
    public IEnumerator Button_PressFillsTiles()
    {
        var c = new Vector3Int(3, 3, 0);
        var (tm, toggle) = Build(false, (c, false));
        var button = Track(new GameObject("button", typeof(BoxCollider2D), typeof(Button2D)));
        button.transform.position = new Vector3(-5f, -5f, 0f);
        button.GetComponent<BoxCollider2D>().isTrigger = true;
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(button.GetComponent<Button2D>());
        so.FindProperty("triggerLayer").intValue = 1;
        var targets = so.FindProperty("targets");
        targets.arraySize = 1;
        targets.GetArrayElementAtIndex(0).objectReferenceValue = toggle;
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
        var body = Track(new GameObject("body", typeof(Rigidbody2D), typeof(BoxCollider2D)));
        body.GetComponent<Rigidbody2D>().gravityScale = 0f;
        body.transform.position = button.transform.position;

        for (int i = 0; i < 5 && tm.GetTile(c) != filled; i++) yield return new WaitForFixedUpdate();
        Assert.AreSame(filled, tm.GetTile(c));
        Assert.IsTrue(HasCollider(tm, c));
    }
}
