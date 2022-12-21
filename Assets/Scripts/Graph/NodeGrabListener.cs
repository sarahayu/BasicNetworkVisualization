using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
UNUSED????
*/

public class NodeGrabListener : MonoBehaviour
{
    public NodeDisplay nodeContainer;

    public void OnGrab()
    {
        nodeContainer.Grab();
    }
}
