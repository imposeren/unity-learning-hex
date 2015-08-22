using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System;

internal class TextFloaterBehaviour : MonoBehaviour {
    private int duration;
    private float alreadyMovedRange;
    private float speed;
    private string text;
    private float moveRange;
    private Camera referenceCamera;

    internal static GameObject Spawn(string text, Vector3 position, int duration, Color color, float moveRange) {
        var floater = Instantiate(Resources.Load<GameObject>("Prefabs/textFloater"));
        var floaterBehaveior = floater.AddComponent<TextFloaterBehaviour>();
        var textMesh = floater.GetComponent<TextMesh>();
        
        textMesh.text = text;
        textMesh.color = color;

        floater.transform.position = position;

        floaterBehaveior.text = text;
        if (duration == 0) {
            throw new DivideByZeroException();
        }
        floaterBehaveior.duration = duration;
        floaterBehaveior.speed = moveRange / duration;
        floaterBehaveior.moveRange = moveRange;
        floaterBehaveior.alreadyMovedRange = 0f;

        return floater;
    }

    void Awake() {
        // if no camera referenced, grab the main camera
        if (!referenceCamera)
            referenceCamera = Camera.main;
    }

    void Update() {
        if (this.alreadyMovedRange >= this.moveRange || !this.gameObject) {
            return;
        }
        this.gameObject.transform.LookAt(
            -(this.gameObject.transform.position + referenceCamera.transform.rotation * Vector3.back),
            referenceCamera.transform.rotation * Vector3.up
        );
        var moveDist = Time.deltaTime * 1000 * this.speed;
        this.alreadyMovedRange += moveDist;
        this.gameObject.transform.position = this.transform.position + new Vector3(0, moveDist, 0);
        if (this.alreadyMovedRange >= this.moveRange) {
            UnityEngine.Object.Destroy(this.gameObject);
        };
    }
}