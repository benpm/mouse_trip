using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Loads the real level and drops the real player onto the toggle button, centred on the tile seams
// beside it, from many landing phases. Fails while the FG tilemap uses per-tile collider boxes.
public class SceneLandingTests
{
    Scene scratch;

    [UnitySetUp]
    public IEnumerator Load()
    {
        yield return SceneManager.LoadSceneAsync("level_pieces", LoadSceneMode.Single);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Unload()
    {
        scratch = SceneManager.CreateScene("scratch_" + Random.Range(0, 1 << 20));
        SceneManager.SetActiveScene(scratch);
        yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("level_pieces"));
    }

    [UnityTest]
    public IEnumerator Landing_OnButtonSeam_DoesNotFallThrough()
    {
        var pc = Object.FindAnyObjectByType<PlayerController>();
        var toggle = Object.FindAnyObjectByType<ToggleTiles2D>();
        var button = Object.FindAnyObjectByType<Button2D>();
        Assert.IsNotNull(pc); Assert.IsNotNull(toggle); Assert.IsNotNull(button);
        var rb = pc.GetComponent<Rigidbody2D>();
        float bx = button.transform.position.x;
        var log = new System.Text.StringBuilder();
        int falls = 0, n = 0;
        foreach (bool startFilled in new[] { false, true })
        foreach (float dx in new[] { -0.5f, 0.5f })
        for (int k = 0; k < 12; k++)
        {
            float y0 = 0.6f + k * 0.04f;
            toggle.Set(startFilled);
            rb.position = new Vector2(bx + dx, y0); rb.linearVelocity = new Vector2(0f, -22f);
            yield return new WaitForFixedUpdate();
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            n++;
            if (rb.position.y < -1.5f) { falls++; log.Append($" f0={startFilled},dx={dx:F1},y0={y0:F2}->{rb.position}"); }
        }
        Debug.Log($"[SeamLanding] drops={n} falls={falls}{log}");
        Assert.AreEqual(0, falls, "player fell through the floor" + log);
    }

    [UnityTest]
    public IEnumerator FloorShapes_RemainAfterToggle()
    {
        var toggle = Object.FindAnyObjectByType<ToggleTiles2D>();
        var tm = toggle.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        var col = toggle.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
        var hits = new System.Collections.Generic.List<Collider2D>();
        string Audit()
        {
            var missing = new System.Text.StringBuilder();
            for (int x = -7; x <= 7; x++) for (int y = -4; y <= -2; y++)
            {
                var c = new Vector3Int(x, y, 0);
                if (tm.GetTile(c) == null) continue;
                hits.Clear();
                Physics2D.OverlapPoint(tm.GetCellCenterWorld(c), new ContactFilter2D().NoFilter(), hits);
                if (!hits.Contains(col)) missing.Append($" ({x},{y})");
            }
            return $"shapes={col.shapeCount} missing:[{missing}]";
        }
        yield return new WaitForFixedUpdate();
        string a = Audit();
        toggle.Toggle(); yield return new WaitForFixedUpdate();
        string b = Audit();
        toggle.Toggle(); yield return new WaitForFixedUpdate();
        string c = Audit();
        Debug.Log($"[FloorShapes] before: {a} | filled: {b} | outline: {c}");
        StringAssert.Contains("missing:[]", a); StringAssert.Contains("missing:[]", b); StringAssert.Contains("missing:[]", c);
    }
}
