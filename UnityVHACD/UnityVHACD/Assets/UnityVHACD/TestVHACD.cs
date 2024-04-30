using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using Vhacd;
using Debug = UnityEngine.Debug;
using Task = System.Threading.Tasks.Task;

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
        using (var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh))
        {
            for (int i = 0; i < meshDataArray.Length; i++)
            {
                var meshData = meshDataArray[i];
                var vertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Persistent);
                meshData.GetVertices(vertices);
                var indices = new NativeArray<int>((int) mesh.GetIndexCount(0), Allocator.Persistent);
                meshData.GetIndices(indices, 0);

                var convexHulls = await RunConvexDecomposition(vertices, indices);
                CreateDecomposedMesh(convexHulls);

                vertices.Dispose();
                indices.Dispose();
            }
        }
    }

    private async Task<Mesh.MeshDataArray> RunConvexDecomposition(NativeArray<Vector3> vertices,
        NativeArray<int> indices)
    {
        var parameters = VhacdParameters.Default;
        IntPtr paramPtr = Marshal.AllocHGlobal(Marshal.SizeOf(parameters));
        Marshal.StructureToPtr(parameters, paramPtr, false);
        var iVhacd = UnityVhacd.CreateVHACD(paramPtr);
        var cb = new UnityVhacd.UserCallback(UserCallback);
        var cbPtr = Marshal.GetFunctionPointerForDelegate(cb);

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        bool computeVhacd = false;
        await Task.Run(() =>
        {
            unsafe
            {
                Vector3* pVerts = (Vector3*) vertices.GetUnsafePtr();
                int* pTris = (int*) indices.GetUnsafePtr();

                computeVhacd = UnityVhacd.Compute(
                    iVhacd,
                    (float*) pVerts, (uint) vertices.Length,
                    (uint*) pTris, (uint) indices.Length / 3,
                    paramPtr, cbPtr);
                Debug.Log("compute vhacd: " + computeVhacd);
            }
        });
        stopwatch.Stop();

        if (!computeVhacd)
        {
            Debug.LogError("Failed to compute VHACD decomposition");
            return new Mesh.MeshDataArray();
        }
        Debug.Log("VHACD took: " + stopwatch.ElapsedMilliseconds + "ms");

        int nConvexHulls = (int) UnityVhacd.GetNConvexHulls(iVhacd);
        Debug.Log("n convex hulls: " + nConvexHulls);

        var meshDataArray = Mesh.AllocateWritableMeshData(nConvexHulls);
        for (int i = 0; i < nConvexHulls; i++)
        {
            unsafe
            {
                IntPtr hullPointer = Marshal.AllocHGlobal(Marshal.SizeOf<VhacdConvexHull>());
                UnityVhacd.GetConvexHull(out var handle, iVhacd, (uint) i, hullPointer);
                var hull = Marshal.PtrToStructure<VhacdConvexHull>(hullPointer);

                var meshData = meshDataArray[i];
                meshData.SetVertexBufferParams((int) hull.NPoints,
                    new VertexAttributeDescriptor(VertexAttribute.Position));
                var vertexData = meshData.GetVertexData<Vector3>();
                var hullVertex = hull.Points;
                for (int vertIdx = 0; vertIdx < hull.NPoints; vertIdx++)
                {
                    vertexData[vertIdx] =
                        new Vector3((float) hullVertex->X, (float) hullVertex->Y, (float) hullVertex->Z);
                    hullVertex++;
                }

                meshData.SetIndexBufferParams((int) hull.NTriangles * 3, IndexFormat.UInt16);
                var indexData = meshData.GetIndexData<ushort>();
                var pTriangle = hull.Triangles;
                for (int triangleCount = 0; triangleCount < hull.NTriangles; triangleCount += 1)
                {
                    indexData[triangleCount * 3 + 0] = (ushort) (pTriangle->Index0);
                    indexData[triangleCount * 3 + 1] = (ushort) (pTriangle->Index1);
                    indexData[triangleCount * 3 + 2] = (ushort) (pTriangle->Index2);

                    pTriangle++;
                }

                meshData.subMeshCount = 1;
                meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexData.Length), MeshUpdateFlags.DontValidateIndices);

                Marshal.FreeHGlobal(hullPointer);
                handle.Dispose();
            }
        }

        Marshal.FreeHGlobal(paramPtr);
        UnityVhacd.ReleaseVHACD(iVhacd);
        return meshDataArray;
    }

    private void CreateDecomposedMesh(Mesh.MeshDataArray convexHulls)
    {
        int colorCycleLen = _colorCycle.Length;
        GameObject decomposedMesh = new GameObject("Decomposed Mesh");
        Mesh[] meshArray = new Mesh[convexHulls.Length];
        for (int i = 0; i < convexHulls.Length; i++)
        {
            Mesh m = new Mesh();
            meshArray[i] = m;
            
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

        Mesh.ApplyAndDisposeWritableMeshData(convexHulls, meshArray);
        foreach (var mesh in meshArray)
        {
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();
        }
    }

    private void UserCallback(double overallProgress, double stageProgress, IntPtr stage, IntPtr operation)
    {
        Debug.Log("overall progress: " + overallProgress + ", stage progress: " + stageProgress + ", stage: " +
                  Marshal.PtrToStringAnsi(stage) + ", operation: " +
                  Marshal.PtrToStringAnsi(operation));
    }
}