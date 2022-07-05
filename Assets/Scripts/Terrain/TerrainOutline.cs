using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;
using TriangleNet.Topology;

public class TerrainOutline
{
    Texture2D _outlineTex;
    List<Vertex> _outlineVertices = new List<Vertex>();      // of type Vertex to make Delauney convenient
    List<int> _outlineVerticesHash = new List<int>();       // ensure no duplicates... bookkeeping is fun /j
    GenericMesher _mesher = new GenericMesher(new Dwyer());
    Material _glMaterial;
    RenderTexture _renderTexture;

    public TerrainOutline(Texture2D outlineTex)
    {
        _outlineTex = outlineTex;
        _glMaterial = new Material(Shader.Find("Hidden/Internal-GUITexture"));
        _glMaterial.hideFlags = HideFlags.HideAndDontSave;
        _glMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
        _glMaterial.SetTexture("_MainTex", _outlineTex);
        _renderTexture = RenderTexture.GetTemporary(_outlineTex.width, _outlineTex.height, 16, RenderTextureFormat.ARGB32);
    }
    
    // https://alessandrotironigamedev.com/2019/04/02/using-unity-low-level-graphics-api-to-draw-custom-charts/
    public void AddPointAndUpdate(Vector2 pos)
    {
        int pixX = (int)(pos.x * _outlineTex.width), pixY = (int)(pos.y * _outlineTex.height);
        _outlineVertices.Add(new Vertex(pixX, pixY));

        if (_outlineVertices.Count < 3) return;

        // have to copy to new List???? Triangle.NET is caching list somehow...?
        List<Vertex> copy = new List<Vertex>();
        foreach (var vert in _outlineVertices)
            copy.Add(new Vertex(vert.x, vert.y));

        // Generate mesh.
        var mesh = _mesher.Triangulate(copy);
       
        RenderTexture.active = _renderTexture;
       
        // clear GL //
        GL.Clear( false, true, Color.white );
       
        // render GL immediately to the active render texture //
        
        _glMaterial.SetPass( 0 );
        GL.PushMatrix();
        GL.LoadPixelMatrix( 0, _outlineTex.width, _outlineTex.height, 0 );
        GL.Begin( GL.TRIANGLES );
        GL.Color( new Color( 0, 0, 0, 1f ) );
        foreach (var triangle in mesh.Triangles)
        {
            Vertex p0 = triangle.GetVertex(0), p1 = triangle.GetVertex(1), p2 = triangle.GetVertex(2);
            float x0 = (float)p0.x, y0 = (float)p0.y,
                x1 = (float)p1.x, y1 = (float)p1.y,
                x2 = (float)p2.x, y2 = (float)p2.y;
            GL.Vertex3( x0 * _outlineTex.width, y0 * _outlineTex.height, 0 );
            GL.Vertex3( x1 * _outlineTex.width, y1 * _outlineTex.height, 0 );
            GL.Vertex3( x2 * _outlineTex.width, y2 * _outlineTex.height, 0 );
        }
        GL.End();
        GL.PopMatrix();

        ////////////////
 
        // read the active RenderTexture into a new Texture2D //
        // var t = new Texture2D(_renderTexture.width, _renderTexture.height);
        // t.ReadPixels( new Rect( 0, 0, t.width, t.height ), 0, 0 );
        // Graphics.CopyTexture(t, _outlineTex);

        Graphics.CopyTexture(_renderTexture, _outlineTex);
 
        // apply pixels and compress //
        bool applyMipsmaps = false;
        _outlineTex.Apply( applyMipsmaps );
        bool highQuality = true;
        _outlineTex.Compress( highQuality );
       
        // clean up after the party //
        RenderTexture.active = null;

        // _outlineTex.SetPixel((int)(pos.x * _outlineTex.width), (int)(pos.y * _outlineTex.height), Color.black);
        // _outlineTex.Apply();
    }

    public void ClearPoints()
    {
        _outlineVertices.Clear();
        _outlineTex = new Texture2D(_outlineTex.width, _outlineTex.height);
    }
}
