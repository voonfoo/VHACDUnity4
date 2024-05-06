using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Vhacd
{
    public interface IUnityVhacd : IDisposable
    {
        public bool ConvexDecompose(Mesh mesh, UnityVhacd.UserCallback cb = null);
        public Task<bool> ConvexDecomposeAsync(Mesh mesh, UnityVhacd.UserCallback cb = null);
        public int GetNConvexHulls();
        public VhacdConvexHull GetConvexHull(int index);
        public Mesh.MeshDataArray ConvertConvexHullsIntoMesh();
    }
}