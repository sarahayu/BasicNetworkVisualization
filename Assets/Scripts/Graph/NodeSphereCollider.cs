using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeSphereCollider : MonoBehaviour
{   
    public NodeDisplay nodeContainer; 
    public float pointerMaxReach = 3f;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GameController"))
        {
            nodeContainer.Hover();
            cutPointer(other.gameObject, other.ClosestPoint(transform.position));
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("GameController"))
        {
            nodeContainer.Unhover();
        }
    }

    static void cutPointer(GameObject pointer, Vector3 collisionPoint)
    {
        var newScale = pointer.transform.localScale;
        var curScale = newScale.z;
        var collider = pointer.GetComponent<BoxCollider>();
        var newColliderScale = collider.size;
        var colliderScaleZ = newColliderScale.z;

        if (colliderScaleZ == 1)
        {
            // print("cutting" + (pointer.transform.InverseTransformVector(collisionPoint) - pointer.transform.localPosition).magnitude);
            // var ogLen = collider.bounds.size.z / curScale;
            // newScale.z = (pointer.transform.InverseTransformVector(collisionPoint) - pointer.transform.localPosition).magnitude * 3 / ogLen;
            // // newColliderScale.z = newScale.z / originalLength;
            // pointer.transform.localScale = newScale;
            // // pointer.GetComponent<BoxCollider>().size = newColliderScale;
        }
    }
}
