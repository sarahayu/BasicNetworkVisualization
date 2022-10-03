using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;
using TriangleNet.Topology;

public class TerrainOutline
{
    Texture2D _outlineTexture;
    List<Vertex> _outlineVertices = new List<Vertex>();      // of type Vertex to make Delauney convenient
    Material _glMaterial;
    RenderTexture _renderTexture;

    public Texture2D OutlineTexture
    {
        get { return _outlineTexture; }
    }

    public TerrainOutline(Texture2D outlineTex)
    {
        _outlineTexture = outlineTex;
        _outlineTexture.wrapMode = TextureWrapMode.Clamp;       // make sure tex is set to clamp

        _glMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        _glMaterial.hideFlags = HideFlags.HideAndDontSave;
        _glMaterial.shader.hideFlags = HideFlags.HideAndDontSave;

        // set these blending modes so we can paint on transparent colors (we'll use alpha channel to distinguish between highlight and outline. See lambert shader)
        _glMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        _glMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);

        _renderTexture = RenderTexture.GetTemporary(_outlineTexture.width, _outlineTexture.height);

        ClearPoints();
    }
    
    // https://alessandrotironigamedev.com/2019/04/02/using-unity-low-level-graphics-api-to-draw-custom-charts/
    public void AddPointAndUpdate(Vector2 pos)
    {
        int pixX = (int)(pos.x * _outlineTexture.width), pixY = (int)(pos.y * _outlineTexture.height);
        _outlineVertices.Add(new Vertex(pixX, pixY));
        
        if (_outlineVertices.Count == 1)
        {
            _outlineTexture.SetPixel(pixX, _outlineTexture.height - pixY, new Color(1, 1, 1, 0));
            _outlineTexture.Apply();
            return;
        }

        RenderTexture.active = _renderTexture; 
       
        GL.Clear( false, true, new Color(0, 0, 0, 0) );    
        
        _glMaterial.SetPass( 0 );
        GL.PushMatrix();
        GL.LoadPixelMatrix( 0, _outlineTexture.width, _outlineTexture.height, 0 );
        GL.Begin( GL.LINE_STRIP );
        GL.Color( new Color( 1f, 1f, 1f, 0f ) );
        foreach (var vert in _outlineVertices)
            GL.Vertex3( (float)vert.x, (float)vert.y, 0 );
        GL.End();
        GL.PopMatrix();
 
        _outlineTexture.ReadPixels( new Rect( 0, 0, _outlineTexture.width, _outlineTexture.height ), 0, 0 );
        _outlineTexture.Apply();
 
        RenderTexture.active = null;
    }

    // returns true if valid selection area has been made
    public bool ConnectAndUpdate()
    {
        // not even points for an outline, reset
        if (_outlineVertices.Count < 3)
        {
            ClearPoints();
            return false;
        }

        // have to copy to new List???? Triangle.NET is caching list somehow...?
        List<Vertex> copy = new List<Vertex>();
        foreach (var vert in _outlineVertices)
            copy.Add(new Vertex(vert.x, vert.y));

        var polygon = new Polygon(copy.Count);
        polygon.Add(new Contour(copy));
        var mesh = polygon.Triangulate();
       
        RenderTexture.active = _renderTexture;
       
        // render GL stuff //
        GL.Clear( false, true, Color.gray );      
        
        _glMaterial.SetPass( 0 );
        GL.PushMatrix();
        GL.LoadPixelMatrix( 0, _outlineTexture.width, _outlineTexture.height, 0 );
        GL.Begin( GL.TRIANGLES );
        GL.Color( Color.white );
        foreach (var triangle in mesh.Triangles)
        {
            Vertex p0 = triangle.GetVertex(0), p1 = triangle.GetVertex(1), p2 = triangle.GetVertex(2);
            float x0 = (float)p0.x, y0 = (float)p0.y,
                x1 = (float)p1.x, y1 = (float)p1.y,
                x2 = (float)p2.x, y2 = (float)p2.y;
            GL.Vertex3( x0, y0, 0 );
            GL.Vertex3( x1, y1, 0 );
            GL.Vertex3( x2, y2, 0 );
        }
        GL.End();
        GL.PopMatrix();

        ////////////////
 
        _outlineTexture.ReadPixels( new Rect( 0, 0, _outlineTexture.width, _outlineTexture.height ), 0, 0 );
        _outlineTexture.Apply();
 
        RenderTexture.active = null;

        return true;
    }

    public void ClearPoints()
    {
        _outlineVertices.Clear();
        ClearColor(new Color(0, 0, 0, 0));
    }

    // not being used right now
    public bool PointInOutline(Vector2 texPos)
    {
        return TextureUtil.IsWhite(_outlineTexture, texPos);
    }

    void ClearColor(Color color)
    {
        RenderTexture.active = _renderTexture;
       
        GL.Clear( false, true, color );
 
        _outlineTexture.ReadPixels( new Rect( 0, 0, _outlineTexture.width, _outlineTexture.height ), 0, 0 );
        _outlineTexture.Apply();
 
        RenderTexture.active = null;
    }
}
