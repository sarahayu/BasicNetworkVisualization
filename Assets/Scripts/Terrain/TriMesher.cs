using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;
using TriangleNet.Topology;

public class TriMesher : MonoBehaviour
{
    public static TriMesher Instance { get; private set; }

    public GenericMesher mesher = new GenericMesher(new Dwyer());

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }
}
