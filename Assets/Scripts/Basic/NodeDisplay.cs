using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NodeDisplay : MonoBehaviour
{
    public Transform renderedObject;
    public float textFadeOut = 0.35f;
    public Collider grabCollider;
    GameObject sphere;
    GameObject label;
    TextMeshPro TMP;
    GameObject worldCamera;
    NetworkObject network;

    Color color;
    string id;
    int hash;
    bool rearrangingAroundThis = false;

    public void Init(NetworkObject network, Color color, string id, int hash)
    {
        this.network = network;
        this.id = id;
        this.color = color;
        this.hash = hash;
    }

    void Start()
    {
        worldCamera = GameObject.Find("HeadAnchor");
        sphere = renderedObject.Find("Sphere").gameObject;
        label = renderedObject.Find("Label").gameObject;
        TMP = label.GetComponent<TextMeshPro>();

        sphere.GetComponent<Renderer>().material.color = color;
        TMP.text = id;
        TMP.faceColor = WashoutColor(TMP.faceColor, textFadeOut);
    }
    void Update()
    {
        label.transform.forward = worldCamera.transform.forward;
    }

    void FixedUpdate()
    {
        
        if (rearrangingAroundThis)
        {
            network.Rearrange(hash);
        }
    }

    public void Hover()
    {
        TMP.faceColor = WashoutColor(TMP.faceColor, 1f);
        sphere.GetComponent<Renderer>().material.color = color * 1.5f;
    }

    public void Unhover()
    {
        TMP.faceColor = WashoutColor(TMP.faceColor, textFadeOut);
        sphere.GetComponent<Renderer>().material.color = color;
    }

    public void Grab()
    {
        rearrangingAroundThis = true;
    }

    public void Ungrab()
    {
        rearrangingAroundThis = false;
    }

    Color WashoutColor(Color color, float gamma)
    {
        return new Color(color.r, color.g, color.b, gamma);
    }
}
