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

    Color normalColor;
    Color desatColor;
    string id;
    int hash;
    bool rearrangingAroundThis = false;

    public void Init(NetworkObject network, HSV color, string id, int hash)
    {
        this.network = network;
        this.id = id;
        this.hash = hash;
        
        this.normalColor = color.ToRGB();
        this.desatColor = color.CloneAndDesat(0.5f).ToRGB();
    }

    void Start()
    {
        worldCamera = GameObject.Find("HeadAnchor");
        sphere = renderedObject.Find("Sphere").gameObject;
        label = renderedObject.Find("Label").gameObject;
        TMP = label.GetComponent<TextMeshPro>();

        sphere.GetComponent<Renderer>().material.color = desatColor;
        TMP.text = id;
        TMP.faceColor = ColorUtil.DuplicateWithGamma(TMP.faceColor, textFadeOut);
    }
    void Update()
    {
        label.transform.forward = worldCamera.transform.forward;
    }

    void FixedUpdate()
    {
        
        if (rearrangingAroundThis)
        {
            network.RearrangeLinks(hash);
        }
    }

    public void Hover()
    {
        TMP.faceColor = ColorUtil.DuplicateWithGamma(TMP.faceColor, 1f);
        sphere.GetComponent<Renderer>().material.color = normalColor;
    }

    public void Unhover()
    {
        TMP.faceColor = ColorUtil.DuplicateWithGamma(TMP.faceColor, textFadeOut);
        sphere.GetComponent<Renderer>().material.color = desatColor;
    }

    public void Grab()
    {
        rearrangingAroundThis = true;
    }

    public void Ungrab()
    {
        rearrangingAroundThis = false;
    }
}
