using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Vhacd;
using Debug = UnityEngine.Debug;

public class VhacdDemo : MonoBehaviour
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
            Debug.Log("Start Convex Decomposition");
            RunVhacd();
        }
    }

    private async void RunVhacd()
    {
        using (IUnityVhacd vhacd = new UnityVhacd(VhacdParameters.Default))
        {
            Mesh mesh = meshFilter.sharedMesh;
            bool decompose = await vhacd.ConvexDecomposeAsync(mesh, UserCallback);
            Debug.Log("ConvexDecompose: " + decompose);

            var meshArrays = vhacd.ConvertConvexHullsIntoMesh();
            CreateDecomposedMesh(meshArrays);
        }
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