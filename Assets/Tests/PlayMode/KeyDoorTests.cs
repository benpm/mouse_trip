using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class KeyDoorTests
{
    readonly List<Object> created = new List<Object>();
    static readonly Vector2 Far = new Vector2(-200f, -200f);

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created) if (o != null) Object.Destroy(o);
        created.Clear();
    }

    T Track<T>(T o) where T : Object { created.Add(o); return o; }

    static void SetField(Object o, string name, object value)
    {
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(o);
        var p = so.FindProperty(name);
        switch (value)
        {
            case string s: p.stringValue = s; break;
            case bool b: p.boolValue = b; break;
            case Object u: p.objectReferenceValue = u; break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
    }

    PlayerInteract Player(Vector2 pos)
    {
        var go = Track(new GameObject("player", typeof(Rigidbody2D), typeof(CapsuleCollider2D)));
        go.transform.position = pos;
        var rb = go.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        go.GetComponent<CapsuleCollider2D>().size = new Vector2(0.5f, 1f);
        return go.AddComponent<PlayerInteract>();
    }

    Pickupable2D Key(Vector2 pos, string id = "key")
    {
        var go = Track(new GameObject("key", typeof(Rigidbody2D)));
        go.transform.position = pos;
        var rb = go.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        go.AddComponent<CircleCollider2D>().radius = 0.3f;
        var range = go.AddComponent<CircleCollider2D>();
        range.radius = 0.7f;
        range.isTrigger = true;
        var key = go.AddComponent<Pickupable2D>();
        SetField(key, "itemId", id);
        return key;
    }

    KeyDoor2D Door(Vector2 pos)
    {
        var go = Track(new GameObject("door", typeof(BoxCollider2D)));
        go.transform.position = pos;
        var box = go.GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1f, 1.6f);
        return go.AddComponent<KeyDoor2D>();
    }

    static IEnumerator Steps(int n = 3) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

    IEnumerator PickedUp(PlayerInteract player, Pickupable2D key)
    {
        yield return Steps();
        player.Interact();
        yield return null;
        Assert.AreEqual(key, player.Held, "key not picked up");
    }

    [UnityTest]
    public IEnumerator Interact_PicksUpKey_ParentsAndStopsSimulating()
    {
        var player = Player(Far);
        var key = Key(Far + new Vector2(0.5f, 0f));
        yield return PickedUp(player, key);
        Assert.AreEqual(player.transform, key.transform.parent);
        Assert.IsFalse(key.GetComponent<Rigidbody2D>().simulated);
        Assert.IsTrue(key.IsHeld);
    }

    [UnityTest]
    public IEnumerator Interact_WithNothingInRange_DropsKey()
    {
        var player = Player(Far);
        var key = Key(Far + new Vector2(0.5f, 0f));
        yield return PickedUp(player, key);

        var rb = player.GetComponent<Rigidbody2D>();
        rb.position = Far + new Vector2(10f, 0f);
        rb.linearVelocity = new Vector2(3f, 0f);
        yield return Steps();
        player.Interact();
        yield return null;
        Assert.IsNull(player.Held);
        Assert.IsNull(key.transform.parent);
        var krb = key.GetComponent<Rigidbody2D>();
        Assert.IsTrue(krb.simulated);
        Assert.Less(Vector2.Distance(krb.position, rb.position), 1.5f, "key dropped far from player");
        Assert.Greater(krb.linearVelocity.x, 1f, "key did not inherit velocity");
    }

    [UnityTest]
    public IEnumerator CarriedKey_EnteringDoor_OpensAndConsumes()
    {
        var player = Player(Far);
        var key = Key(Far + new Vector2(0.5f, 0f));
        var door = Door(Far + new Vector2(10f, 0f));
        int opened = 0;
        door.onOpened.AddListener(() => opened++);
        yield return PickedUp(player, key);

        player.GetComponent<Rigidbody2D>().position = door.transform.position;
        for (int i = 0; i < 5 && !door.IsOpen; i++) yield return new WaitForFixedUpdate();
        yield return null;
        Assert.IsTrue(door.IsOpen);
        Assert.AreEqual(1, opened);
        Assert.IsNull(player.Held);
        Assert.IsTrue(key == null, "key not consumed");
    }

    [UnityTest]
    public IEnumerator DroppedKey_EnteringDoor_Opens()
    {
        var door = Door(Far);
        var key = Key(Far + new Vector2(5f, 0f));
        yield return Steps();
        key.GetComponent<Rigidbody2D>().position = door.transform.position;
        for (int i = 0; i < 5 && !door.IsOpen; i++) yield return new WaitForFixedUpdate();
        Assert.IsTrue(door.IsOpen);
    }

    [UnityTest]
    public IEnumerator WrongItemId_DoesNotOpen()
    {
        var door = Door(Far);
        var key = Key(door.transform.position, "red");
        yield return Steps(5);
        Assert.IsFalse(door.IsOpen);
        Assert.IsTrue(key != null);
    }

    [UnityTest]
    public IEnumerator Interact_Door_OnlyEntersWhenOpen()
    {
        var door = Door(Far);
        int entered = 0;
        door.onEntered.AddListener(() => entered++);
        var player = Player(door.transform.position);
        yield return Steps();
        player.Interact();
        Assert.AreEqual(0, entered, "entered a locked door");

        door.Open(null);
        player.Interact();
        Assert.AreEqual(1, entered);
    }

    [UnityTest]
    public IEnumerator Interact_InOpenDoorWhileHolding_EntersInsteadOfDropping()
    {
        var player = Player(Far);
        var key = Key(Far + new Vector2(0.5f, 0f), "other");
        var door = Door(Far + new Vector2(10f, 0f));
        int entered = 0;
        door.onEntered.AddListener(() => entered++);
        door.Open(null);
        yield return PickedUp(player, key);

        player.GetComponent<Rigidbody2D>().position = door.transform.position;
        yield return Steps();
        player.Interact();
        Assert.AreEqual(1, entered);
        Assert.AreEqual(key, player.Held, "key was dropped instead of entering");
    }
}
