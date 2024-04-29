using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vhacd;

public class TestVHACD : MonoBehaviour
{
    public MeshFilter meshFilter;
    public Material meshMaterial;

    private readonly int[][] _colorCycle =
    {
        new[] {31, 119, 180},
        new[] {255, 127, 14},
        new[] {44, 160, 44},
        new[] {214, 39, 40},
        new[] {148, 103, 189},
        new[] {140, 86, 75},
        new[] {227, 119, 194},
        new[] {127, 127, 127},
        new[] {188, 189, 34},
        new[] {23, 190, 207}
    };

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ComputeVhacd();
        }
    }

    private async void ComputeVhacd()
    {
        Mesh mesh = meshFilter.sharedMesh;
        var verts = mesh.vertices;
        var tris = mesh.triangles;
        List<(Vector3[], int[])> convexHulls = new();
        await UniTask.RunOnThreadPool(() =>
        {
            unsafe
            {
                var parameters = VhacdParameters.Default;
                IntPtr paramPtr = Marshal.AllocHGlobal(Marshal.SizeOf(parameters));
                Marshal.StructureToPtr(parameters, paramPtr, false);
                var iVhacd = UnityVhacd.CreateVHACD(paramPtr);

                var cb = new UnityVhacd.UserCallback(UserCallback);
                var cbPtr = Marshal.GetFunctionPointerForDelegate(cb);


                fixed (Vector3* pVerts = verts)
                fixed (int* pTris = tris)
                {
                    bool res = UnityVhacd.Compute(
                        iVhacd,
                        (float*) pVerts, (uint) verts.Length,
                        (uint*) pTris, (uint) tris.Length / 3,
                        paramPtr, cbPtr);
                    Debug.Log("compute vhacd: " + res);
                }

                uint nConvexHulls = UnityVhacd.GetNConvexHulls(iVhacd);
                Debug.Log("n convex hulls: " + nConvexHulls);
                for (uint i = 0; i < nConvexHulls; i++)
                {
                    IntPtr hullPointer = Marshal.AllocHGlobal(Marshal.SizeOf<VhacdConvexHull>());
                    UnityVhacd.GetConvexHull(out var handle, iVhacd, i, hullPointer);
                    var hull = Marshal.PtrToStructure<VhacdConvexHull>(hullPointer);
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

                    var indices = new int[hull.NTriangles * 3];
                    var pTriangle = hull.Triangles;
                    for (var triangleCount = 0; triangleCount < hull.NTriangles; triangleCount += 1)
                    {
                        indices[triangleCount * 3 + 0] = (int) (pTriangle->Index0);
                        indices[triangleCount * 3 + 1] = (int) (pTriangle->Index1);
                        indices[triangleCount * 3 + 2] = (int) (pTriangle->Index2);

                        pTriangle++;
                    }

                    convexHulls.Add((hullVerts, indices));

                    Marshal.FreeHGlobal(hullPointer);
                    handle.Dispose();
                }


                Marshal.FreeHGlobal(paramPtr);
                UnityVhacd.ReleaseVHACD(iVhacd);
            }
        });
        await UniTask.SwitchToMainThread();
        CreateDecomposedMesh(convexHulls);
    }

    private void CreateDecomposedMesh(List<(Vector3[], int[])> convexHulls)
    {
        int colorCycleLen = _colorCycle.Length;
        GameObject decomposedMesh = new GameObject("Decomposed Mesh");
        for (int i = 0; i < convexHulls.Count; i++)
        {
            Mesh m = new Mesh();
            m.SetVertices(convexHulls[i].Item1);
            m.SetTriangles(convexHulls[i].Item2, 0);
            m.RecalculateBounds();
            m.RecalculateNormals();
            m.Optimize();

            GameObject go = new GameObject("Convex Hull " + i);
            go.transform.SetParent(decomposedMesh.transform);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            Material mat = new Material(meshMaterial);
            mat.color = new Color(_colorCycle[i % colorCycleLen][0] / 255f, _colorCycle[i % colorCycleLen][1] / 255f,
                _colorCycle[i % colorCycleLen][2] / 255f);
            go.AddComponent<MeshRenderer>().material = mat;
            var meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = m;
            meshCollider.convex = true;
        }
    }

    private void UserCallback(double overallProgress, double stageProgress, IntPtr stage, IntPtr operation)
    {
        Debug.Log("overall progress: " + overallProgress + ", stage progress: " + stageProgress + ", stage: " +
                  Marshal.PtrToStringAnsi(stage) + ", operation: " +
                  Marshal.PtrToStringAnsi(operation));
    }
}