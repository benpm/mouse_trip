using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GravityTriggerTests
{
    readonly List<Object> created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created) if (o != null) Object.Destroy(o);
        created.Clear();
    }

    T Track<T>(T o) where T : Object { created.Add(o); return o; }

    PlayerController Player(float turnSpeed)
    {
        var go = Track(new GameObject("player", typeof(Rigidbody2D), typeof(CapsuleCollider2D)));
        go.transform.position = new Vector2(-50f, -50f);
        var pc = go.AddComponent<PlayerController>();
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(pc);
        so.FindProperty("gravityTurnSpeed").floatValue = turnSpeed;
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
        return pc;
    }

    GravityTrigger2D Trigger(PlayerController pc, Vector2 dir, bool restore, GameObject host = null)
    {
        var t = (host ?? Track(new GameObject("trigger"))).AddComponent<GravityTrigger2D>();
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(t);
        so.FindProperty("player").objectReferenceValue = pc;
        so.FindProperty("activeGravityDir").vector2Value = dir;
        so.FindProperty("restoreOnDeactivate").boolValue = restore;
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
        return t;
    }

    static void AssertDir(Vector2 expected, Vector2 actual) =>
        Assert.Less(Vector2.Distance(expected, actual), 1e-3f, $"expected {expected} got {actual}");

    [UnityTest]
    public IEnumerator Snap_SetsAndRestores()
    {
        var pc = Player(0f);
        var t = Trigger(pc, new Vector2(3f, 0f), restore: true);
        yield return null;
        t.SetActivated(true);
        yield return null;
        AssertDir(Vector2.right, pc.gravityDir);
        t.SetActivated(false);
        yield return null;
        AssertDir(Vector2.down, pc.gravityDir);
    }

    [UnityTest]
    public IEnumerator NoRestore_KeepsDirectionOnDeactivate()
    {
        var pc = Player(0f);
        var t = Trigger(pc, Vector2.up, restore: false);
        yield return null;
        t.SetActivated(true);
        t.SetActivated(false);
        yield return null;
        AssertDir(Vector2.up, pc.gravityDir);
    }

    [UnityTest]
    public IEnumerator Smooth_TurnsGraduallyThenConverges()
    {
        var pc = Player(4f);
        var t = Trigger(pc, Vector2.right, restore: false);
        yield return null;
        t.SetActivated(true);
        // Frame time varies (headless editors run very fast): wait until it has visibly turned, and
        // assert it never jumped straight to the target along the way.
        float a = 0f, t0 = Time.time;
        while (a < 0.5f && Time.time - t0 < 2f)
        {
            yield return null;
            a = Vector2.Angle(Vector2.down, pc.gravityDir);
            Assert.Less(a, 89f, "snapped instead of turning");
        }
        Assert.Greater(a, 0.5f, "did not start turning");
        // 90 deg at 4 rad/s takes ~0.4 s
        int frames = 0;
        while (Vector2.Angle(Vector2.right, pc.gravityDir) > 0.01f && Time.time - t0 < 3f) { yield return null; frames++; }
        Assert.Less(Vector2.Distance(Vector2.right, pc.gravityDir), 1e-3f, $"did not converge after {frames} frames, {Time.time - t0:F2}s");
    }

    [UnityTest]
    public IEnumerator Button_FlipsPlayerGravity()
    {
        var pc = Player(0f);
        var pos = new Vector2(-60f, -60f);
        var button = Track(new GameObject("button", typeof(BoxCollider2D), typeof(Button2D)));
        button.transform.position = pos;
        button.GetComponent<BoxCollider2D>().isTrigger = true;
        var t = Trigger(pc, Vector2.up, restore: true, host: button);
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(button.GetComponent<Button2D>());
        so.FindProperty("triggerLayer").intValue = 1;
        var targets = so.FindProperty("targets");
        targets.arraySize = 1;
        targets.GetArrayElementAtIndex(0).objectReferenceValue = t;
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
        var body = Track(new GameObject("body", typeof(Rigidbody2D), typeof(BoxCollider2D)));
        body.transform.position = pos;
        body.GetComponent<Rigidbody2D>().gravityScale = 0f;
        for (int i = 0; i < 5 && pc.gravityDir != Vector2.up; i++) yield return new WaitForFixedUpdate();
        AssertDir(Vector2.up, pc.gravityDir);
    }
}
