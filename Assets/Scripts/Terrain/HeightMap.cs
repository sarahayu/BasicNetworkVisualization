using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;
using TriangleNet.Topology;

public class HeightMap
{
    TerrainGraph _graph;
    float _falloffDistance;
    AnimationCurve _falloffShapeFunc;
    AnimationCurve _peakHeightFunc;
    AnimationCurve _slackFunc;
    bool _slackIsLevel;         // obsolete field, may remove later

    int _graphMaxWeight = -1;
    int _graphMaxSize = -1;

    public HeightMap(TerrainGraph graph, float falloffDistance, 
        AnimationCurve falloffShapeFunc, AnimationCurve peakHeightFunc, AnimationCurve slackFunc, bool slackIsLevel)
    {
        _graph = graph;
        _falloffDistance = falloffDistance;
        _falloffShapeFunc = falloffShapeFunc;
        _peakHeightFunc = peakHeightFunc;
        _slackFunc = slackFunc;
        _slackIsLevel = slackIsLevel;

        calculateMaxes();
    }
    public Texture2D GenerateTextureHeight(int resX, int resY)
    {
        Material glMaterial;
        RenderTexture renderTexture;
    
        var meshPlotter = new TerrainMeshPlotter(
            graph: _graph,
            meshWidth: resX,
            meshLength: resY,
            subdivideSunflower: 40,
            subdivideRidges: 2
        );

        var mesh = meshPlotter.GenerateMesh();

        var heightsCache = new Dictionary<double, float>();
        foreach (var point in meshPlotter.generatedPoints)
        {
            heightsCache[point.y * resX + point.x] 
                = MaxWeightAt((float)point.x / (resX - 1), (float)point.y / (resY - 1));
        }

        // setup

        var texture = new Texture2D(resX, resY);
        texture.wrapMode = TextureWrapMode.Clamp;

        glMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        glMaterial.hideFlags = HideFlags.HideAndDontSave;
        glMaterial.shader.hideFlags = HideFlags.HideAndDontSave;

        renderTexture = RenderTexture.GetTemporary(resX, resY);
        
        RenderTexture.active = renderTexture;

        // end setup

        // draw
       
        GL.Clear( false, true, Color.black );      
        
        glMaterial.SetPass( 0 );
        GL.PushMatrix();
        GL.LoadPixelMatrix( 0, resX, resY, 0 );
        GL.Begin( GL.TRIANGLES );
        // GL.wireframe = true;
        
        foreach (var triangle in mesh.Triangles)
        {
            Vertex p0 = triangle.GetVertex(0), 
                p1 = triangle.GetVertex(1), 
                p2 = triangle.GetVertex(2);
            double x0 = p0.x, y0 = p0.y,
                x1 = p1.x, y1 = p1.y,
                x2 = p2.x, y2 = p2.y;
            double indP1 = y0 * resX + x0, 
                indP2 = y1 * resX + x1, 
                indP3 = y2 * resX + x2;

            GL.Color( Color.Lerp(Color.black, Color.white, heightsCache[indP1]) );
            GL.Vertex3( (float)x0, resY - (float)y0, 0 );
            GL.Color( Color.Lerp(Color.black, Color.white, heightsCache[indP2]) );
            GL.Vertex3( (float)x1, resY - (float)y1, 0 );
            GL.Color( Color.Lerp(Color.black, Color.white, heightsCache[indP3]) );
            GL.Vertex3( (float)x2, resY - (float)y2, 0 );
        }

        GL.End();
        GL.PopMatrix();

        texture.ReadPixels( new Rect( 0, 0, texture.width, texture.height ), 0, 0 );
        texture.Apply();
 
        RenderTexture.active = null;

        // end draw

        return texture;
    }

    public Texture2D GenerateTextureLines(int resX, int resY, float intensity)
    {
        var colorBit = new float[resX * resY];
        float graphWidth = _graph.width, graphHeight = _graph.height;

        for (int y = 0; y < resY; y++)
            for (int x = 0; x < resX; x++)
            {
                colorBit[y * resX + x] = 0f;
            }


        foreach (var link in _graph.links)
        {
            var source = link.source;
            var target = link.target;
            var weight = link.weight;
            var first = _graph.nodes[source];
            var second = _graph.nodes[target];

            int x1 = (int)Mathf.Floor((float)(first.x - 0) / graphWidth * (resX - 1)), 
                y1 = (int)Mathf.Floor((float)(first.y - 0) / graphHeight * (resY - 1)), 
                size1 = first.size,

                x2 = (int)Mathf.Floor((float)(second.x - 0) / graphWidth * (resX - 1)), 
                y2 = (int)Mathf.Floor((float)(second.y - 0) / graphHeight * (resY - 1)), 
                size2 = second.size;
                
            int dist_x = Math.Abs(x1 - x2), dist_y = Math.Abs(y1 - y2);
            var dist = Mathf.Sqrt(dist_x * dist_x + dist_y * dist_y);
            float vx = (x2 - x1) / dist, vy = (y2 - y1) / dist;
            var minheight = Math.Min(size1, size2);
            var minridge = ridgeFunc(minheight, minheight, weight, 0);
            if (dist_x > dist_y)
            {
                for (var i = 0; i < dist_x; i++)
                {
                    int dx = vx > 0 ? i : -i, dy = (int)Mathf.Floor((float)(y2 - y1) / dist_x * i);
                    colorBit[(y1 + dy) * resX + x1 + dx] = intensity;
                }
            }
            else
            {
                for (var i = 0; i < dist_y; i++)
                {
                    int dx = (int)Mathf.Floor((float)(x2 - x1) / dist_y * i), dy = vy > 0 ? i : -i;
                    colorBit[(y1 + dy) * resX + x1 + dx] = intensity;
                }
            }
        }
        
        float[] res = new float[resX * resX];
            
        MathUtil.gaussBlur_4(colorBit, res, resX, resY, 1);
        
        var colors = new Color[resX * resY];

        for (int y = 0; y < resY; y++)
            for (int x = 0; x < resX; x++)
            {
                colors[y * resX + x] = Color.Lerp(Color.black, Color.white, res[y * resY + x]);
            }

        var texture = new Texture2D(resX, resY);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    void calculateMaxes()
    {
        foreach (var link in _graph.links)
            if (link.weight > _graphMaxWeight) _graphMaxWeight = link.weight;

        foreach (var node in _graph.nodes)
            if (node.size > _graphMaxSize) _graphMaxSize = node.size;
    }

    float ridgeFunc(int size1, int size2, int weight, float offset)
    {
        float lerpedHeight1 = _peakHeightFunc.Evaluate((float)(size1 - 1) / (_graphMaxSize - 1));
        float lerpedHeight2 = _peakHeightFunc.Evaluate((float)(size2 - 1) / (_graphMaxSize - 1));
        float relWeight = MathUtil.RelWeight(weight, size1, size2);
        return Mathf.Max(0.01f, Mathf.Lerp(0, 1, (MathUtil.clerp(lerpedHeight1, lerpedHeight2, 1 - _slackFunc.Evaluate(relWeight), offset))));
    }

    public float GetRadiusFromNodeSize(float size)
    {
        return size / _graphMaxSize * _falloffDistance;
    }

    public float MaxWeightAt(float ratioX, float ratioY)
    {
        float x = ratioX * _graph.width, y = ratioY * _graph.height;
        var maxWeight = 0f;
        foreach (var link in _graph.links)
        {
            var source = link.source;
            var target = link.target;
            var weight = link.weight;
            var first = _graph.nodes[source];
            var second = _graph.nodes[target];
            int x1 = first.x, y1 = first.y, size1 = first.size,
                x2 = second.x, y2 = second.y, size2 = second.size;
            int len_x = Math.Abs(x1 - x2), len_y = Math.Abs(y1 - y2);
            var len_sq = len_x * len_x + len_y * len_y;

            var w = MathUtil.proj_factor(x, y, x1, y1, x2, y2);

            if (_slackIsLevel)
            {
                size1 = size2 = Math.Min(size1, size2);
            }

            var heightAtPoint = Math.Max(0, ridgeFunc(size1, size2, weight, Math.Min(1, Math.Max(0, w))));
            var heightAt1 = ridgeFunc(size1, size2, weight, 0);
            var heightAt2 = ridgeFunc(size1, size2, weight, 1);

            var line_dist = MathUtil.LineDistSq(x, y, x1, y1, x2, y2);
            var param = line_dist.param;
            var distline_sq = line_dist.dist_sq;

            float maxHeightPoint = heightAt1;
            float relWeight;
            // TODO divide by zero with sizes = 1
            if (param < 0)
            {
                var dist = Mathf.Pow(MathUtil.distSq(x, y, x1, y1), 0.5f);
                relWeight = _falloffShapeFunc.Evaluate(Math.Max(0, 1 - dist / _falloffDistance * (_graphMaxSize) / (size1))) * heightAt1;
            }
            else if (param > 1)
            {
                var dist = Mathf.Pow(MathUtil.distSq(x, y, x2, y2), 0.5f);
                relWeight = _falloffShapeFunc.Evaluate(Math.Max(0, 1 - dist / _falloffDistance * (_graphMaxSize) / (size2))) * heightAt2;
            }
            else
            {
                var dist = Mathf.Pow(distline_sq, 0.5f);
                relWeight = _falloffShapeFunc.Evaluate(Math.Max(0, 1 - dist / _falloffDistance * (_graphMaxSize) / (Math.Min(size1, size2)))) * heightAtPoint;
            }

            if (relWeight > maxWeight) maxWeight = relWeight;
        }
        
        // TODO optimize, only check isolated nodes (nodes without links since they would have been looped above)
        foreach (var node in _graph.nodes)
        {
            int x2 = node.x, y2 = node.y, size2 = node.size;

            var dist = Mathf.Pow(Mathf.Pow(x2 - x, 2) + Mathf.Pow(y2 - y, 2), 0.5f);
            float maxHeightPoint = ridgeFunc(size2, size2, _graphMaxWeight, 0);
            float relWeight = _falloffShapeFunc.Evaluate(Math.Max(0, 1 - dist / _falloffDistance / maxHeightPoint)) * maxHeightPoint;

            if (relWeight > maxWeight) maxWeight = relWeight;
        }

        return maxWeight;
    }
}
