using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct OrientedPoint
{

    public Vector3 position;
    public Quaternion rotation;

    public OrientedPoint(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }

    public Vector3 LocalToWorld(Vector3 point)
    {
        return position + rotation * point;
    }

    public Vector3 WorldToLocal(Vector3 point)
    {
        return Quaternion.Inverse(rotation) * (point - position);
    }

    public Vector3 LocalToWorldDirection(Vector3 dir)
    {
        return rotation * dir;
    }

}

public class ExtrudeShape
{
    public Vector2[] verts2Ds = new Vector2[]
    {
        new Vector2(0,0),
        new Vector2(0,1),
        new Vector2(0,1),
        new Vector2(0,2),
        new Vector2(0,3),
        new Vector2(0,4)

    };
    public Vector2[] normals = new Vector2[]
    {
        new Vector2(0,0),
        new Vector2(0,0),
        new Vector2(0,0),
        new Vector2(0,0),
        new Vector2(0,0),
        new Vector2(0,0)
    };
    public float[] us = new float[]
        {
            1,
            1,
            1,
            1,
            1,
            1
        };
    public int[] lines = new int[]
    {
        0, 1,
        2, 3,
        3, 4,
        4, 5
    };

}


public class Road : UniqueMesh
{
    Vector3[] vertices = new Vector3[]
    {
            new Vector3(  1, 0,  1 ),
            new Vector3( -1, 0,  1 ),
            new Vector3(  1, 0, -1 ),
            new Vector3( -1, 0, -1 )
    };

    Vector3[] normals = new Vector3[]
    {
            new Vector3( 0, 1, 0 ),
            new Vector3( 0, 1, 0 ),
            new Vector3( 0, 1, 0 ),
            new Vector3( 0, 1, 0 )
    };

    Vector2[] uvs = new Vector2[]
    {
            new Vector2( 0, 1 ),
            new Vector2( 0, 0 ),
            new Vector2( 1, 1 ),
            new Vector2( 1, 0 )
    };

    int[] triangleIndices = new int[]
    {
        0, 2, 3,
        3, 1, 0
    };


    // Start is called before the first frame update
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh == null)
            mf.sharedMesh = new Mesh();
        Mesh mesh = mf.sharedMesh;

        Extrude(mesh, new ExtrudeShape(),new OrientedPoint[6]);
    }

    public void Extrude(Mesh mesh, ExtrudeShape shape, OrientedPoint[] path)
    {
        int vertsInShape = shape.verts2Ds.Length;
        int segments = path.Length - 1;
        int edgeLoops = path.Length;
        int vertCount = vertsInShape * edgeLoops;
        int triCount = shape.lines.Length * segments;
        int triIndexCount = triCount * 3;

        int[] triangleIndices = new int[triIndexCount];
        vertices = new Vector3[vertCount];
        normals = new Vector3[vertCount];
        uvs = new Vector2[vertCount];


        for (int i = 0; i < path.Length; i++)
        {
            int offset = i * vertsInShape;
            for (int j = 0; j < vertsInShape; j++)
            {
                int id = offset + j;
                vertices[id] = path[i].LocalToWorld(shape.verts2Ds[j]);
                normals[id] = path[i].LocalToWorldDirection(shape.normals[j]);
                uvs[id] = new Vector2(shape.us[j], i / ((float)edgeLoops));
            }
        }
        int ti = 0;
        for (int i = 0; i < segments; i++)
        {
            int offset = i * vertsInShape;
            for (int l = 0; l < shape.lines.Length; l += 2)
            {
                int a = offset + shape.lines[l] + vertsInShape;
                int b = offset + shape.lines[l];
                int c = offset + shape.lines[l + 1];
                int d = offset + shape.lines[l + 1] + vertsInShape;
                triangleIndices[ti] = a; ti++;
                triangleIndices[ti] = b; ti++;
                triangleIndices[ti] = c; ti++;
                triangleIndices[ti] = c; ti++;
                triangleIndices[ti] = d; ti++;
                triangleIndices[ti] = a; ti++;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangleIndices;
    }


    //Curve Functions
    Vector3 GetPoint(Vector3[] pts, float t)
    {
        float omt = 1f - t;
        float omt2 = omt * omt;
        float t2 = t * t;
        return pts[0] * (omt2 * omt) +
                pts[1] * (3f * omt2 * t) +
                pts[2] * (3f * omt * t2) +
                pts[3] * (t2 * t);
    }

    Vector3 GetTangent(Vector3[] pts, float t)
    {
        float omt = 1f - t;
        float omt2 = omt * omt;
        float t2 = t * t;
        Vector3 tangent =
                    pts[0] * (-omt2) +
                    pts[1] * (3 * omt2 - 2 * omt) +
                    pts[2] * (-3 * t2 + 2 * t) +
                    pts[3] * (t2);
        return tangent.normalized;
    }

    Vector3 GetNormal3D(Vector3[] pts, float t, Vector3 up)
    {
        Vector3 tng = GetTangent(pts, t);
        Vector3 binormal = Vector3.Cross(up, tng).normalized;
        return Vector3.Cross(tng, binormal);
    }


    Quaternion GetOrientation3D(Vector3[] pts, float t, Vector3 up)
    {
        Vector3 tng = GetTangent(pts, t);
        Vector3 nrm = GetNormal3D(pts, t, up);
        return Quaternion.LookRotation(tng, nrm);
    }
}

