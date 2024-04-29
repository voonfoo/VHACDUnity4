using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Vhacd;

public class TestVHACD : MonoBehaviour
{
    public MeshFilter meshFilter;
    public Material meshMaterial;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ComputeVhacd();
        }
    }

    private void ComputeVhacd()
    {
        unsafe
        {
            var parameters = VhacdParameters.Default;
            IntPtr paramPtr = Marshal.AllocHGlobal(Marshal.SizeOf(parameters));
            Marshal.StructureToPtr(parameters, paramPtr, false);
            var iVhacd = UnityVhacd.CreateVHACD(paramPtr);

            Mesh mesh = meshFilter.sharedMesh;
            var verts = mesh.vertices;
            var tris = mesh.triangles;
            fixed (Vector3* pVerts = verts)
            fixed (int* pTris = tris)
            {
                bool res = UnityVhacd.Compute(
                    iVhacd,
                    (float*) pVerts, (uint) verts.Length,
                    (uint*) pTris, (uint) tris.Length / 3,
                    paramPtr);
                Debug.Log("compute vhacd: " + res);
            }

            uint nConvexHulls = UnityVhacd.GetNConvexHulls(iVhacd);
            Debug.Log("n convex hulls: " + nConvexHulls);
            for (uint i = 0; i < nConvexHulls; i++)
            {
                IntPtr hullPointer = Marshal.AllocHGlobal(Marshal.SizeOf<VhacdConvexHull>());
                UnityVhacd.GetConvexHull(out var handle, iVhacd, i, hullPointer);
                var hull = Marshal.PtrToStructure<VhacdConvexHull>(hullPointer);
                var hullMesh = new Mesh();
                var hullVerts = new Vector3[hull.NPoints];
                fixed (Vector3* pHullVerts = hullVerts)
                {
                    var pComponents = hull.Points;
                    var pVerts = pHullVerts;

                    for (var pointCount = hull.NPoints; pointCount != 0; --pointCount)
                    {
                        pVerts->x = (float) pComponents->X;
                        pVerts->y = (float) pComponents->Y;
                        pVerts->z = (float) pComponents->Z;

                        pVerts += 1;
                        pComponents += 1;
                    }
                }

                hullMesh.SetVertices(hullVerts);

                var indices = new int[hull.NTriangles * 3];
                var pTriangle = hull.Triangles;
                for (var triangleCount = 0; triangleCount < hull.NTriangles; triangleCount += 1)
                {
                    indices[triangleCount * 3 + 0] = (int) (pTriangle->Index0);
                    indices[triangleCount * 3 + 1] = (int) (pTriangle->Index1);
                    indices[triangleCount * 3 + 2] = (int) (pTriangle->Index2);

                    pTriangle++;
                }

                hullMesh.SetTriangles(indices, 0);
                hullMesh.RecalculateNormals();
                hullMesh.RecalculateBounds();
                
                GameObject go = new GameObject("Convex Hull " + i);
                go.AddComponent<MeshFilter>().mesh = hullMesh;
                go.AddComponent<MeshRenderer>().material = meshMaterial;

                Marshal.FreeHGlobal(hullPointer);
                handle.Dispose();
            }


            Marshal.FreeHGlobal(paramPtr);
            UnityVhacd.ReleaseVHACD(iVhacd);
        }
    }
}