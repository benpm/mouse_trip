using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class Button2DTests
{
    class Receiver : MonoBehaviour, IActivationReceiver
    {
        public List<bool> calls = new List<bool>();
        public void SetActivated(bool active) => calls.Add(active);
    }

    readonly List<Object> created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created) if (o != null) Object.Destroy(o);
        created.Clear();
    }

    T Track<T>(T o) where T : Object { created.Add(o); return o; }

    Receiver Button(Vector2 pos)
    {
        var go = Track(new GameObject("button", typeof(BoxCollider2D), typeof(Button2D)));
        go.transform.position = pos;
        go.GetComponent<BoxCollider2D>().isTrigger = true;
        var receiver = go.AddComponent<Receiver>();
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(go.GetComponent<Button2D>());
        so.FindProperty("triggerLayer").intValue = 1;
        var targets = so.FindProperty("targets");
        targets.arraySize = 1;
        targets.GetArrayElementAtIndex(0).objectReferenceValue = receiver;
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
        return receiver;
    }

    GameObject Body(Vector2 pos, RigidbodyType2D type)
    {
        var go = Track(new GameObject("body", typeof(Rigidbody2D), typeof(BoxCollider2D)));
        go.transform.position = pos;
        var rb = go.GetComponent<Rigidbody2D>();
        rb.bodyType = type;
        rb.gravityScale = 0f;
        return go;
    }

    [UnityTest]
    public IEnumerator DynamicBody_PressesAndReleases()
    {
        var pos = new Vector2(-20f, -20f);
        var receiver = Button(pos);
        var body = Body(pos, RigidbodyType2D.Dynamic);
        for (int i = 0; i < 5 && receiver.calls.Count == 0; i++) yield return new WaitForFixedUpdate();
        CollectionAssert.AreEqual(new[] { true }, receiver.calls);

        body.GetComponent<Rigidbody2D>().position = pos + new Vector2(5f, 0f);
        for (int i = 0; i < 5 && receiver.calls.Count == 1; i++) yield return new WaitForFixedUpdate();
        CollectionAssert.AreEqual(new[] { true, false }, receiver.calls);
    }

    [UnityTest]
    public IEnumerator KinematicAndStaticColliders_DoNotPress()
    {
        var pos = new Vector2(-30f, -30f);
        var receiver = Button(pos);
        Body(pos, RigidbodyType2D.Kinematic);
        Track(new GameObject("static", typeof(BoxCollider2D))).transform.position = pos;
        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Assert.IsEmpty(receiver.calls);
    }
}
