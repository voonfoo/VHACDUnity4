using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vhacd
{
    /// <summary>
    /// Vhacd unity wrapper
    /// </summary>
    public class UnityVhacd : IUnityVhacd
    {
#if UNITY_EDITOR
        private const string DLLName = "UnityVHACD";
#elif (UNITY_IOS && !UNITY_EDITOR)
        private const string DLL_NAME = "__Internal";
#endif

        /// <summary>
        /// Vhacd callback
        /// </summary>
        public delegate void UserCallback(double overallProgress, double stageProgress, IntPtr stage, IntPtr operation);

        #region DLLImports

        [DllImport(DLLName)]
        private static extern IntPtr CreateVHACD(IntPtr param);

        [DllImport(DLLName)]
        private static extern unsafe bool Compute(IntPtr iVhacd, float* points, uint pointCount, uint* triangles,
            uint trianglesCount,
            IntPtr param, IntPtr callback);

        [DllImport(DLLName)]
        private static extern uint GetNConvexHulls(IntPtr iVhacd);

        [DllImport(DLLName)]
        private static extern void GetConvexHull(out ConvexHullSafeHandle handle, IntPtr iVhacd, uint index,
            IntPtr convexHull);

        [DllImport(DLLName)]
        public static extern void ReleaseConvexHull(IntPtr handle);

        [DllImport(DLLName)]
        private static extern void ReleaseVHACD(IntPtr iVhacd);

        #endregion

        /// <summary>
        /// vhacd parameter pointer
        /// </summary>
        private readonly IntPtr _paramPtr;

        /// <summary>
        /// ivhacd pointer
        /// </summary>
        private readonly IntPtr _vhacdPtr;

        private bool _result;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parameters">paramaters</param>
        public UnityVhacd(VhacdParameters parameters)
        {
            _paramPtr = Marshal.AllocHGlobal(Marshal.SizeOf(parameters));
            Marshal.StructureToPtr(parameters, _paramPtr, false);
            _vhacdPtr = CreateVHACD(_paramPtr);
        }

        /// <summary>
        /// Decompose mesh into convex hulls synchronously
        /// </summary>
        /// <param name="mesh">unity mesh</param>
        /// <param name="cb">user callback (optional)</param>
        /// <returns></returns>
        public bool ConvexDecompose(Mesh mesh, UserCallback cb = null)
        {
            using var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            var meshData = meshDataArray[0];
            var vertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Persistent);
            meshData.GetVertices(vertices);
            var indices = new NativeArray<int>((int) mesh.GetIndexCount(0), Allocator.Persistent);
            meshData.GetIndices(indices, 0);

            if (cb == null)
                cb = (_, _, _, _) => { };
            var cbPtr = Marshal.GetFunctionPointerForDelegate(cb);

            unsafe
            {
                Vector3* pVerts = (Vector3*) vertices.GetUnsafePtr();
                int* pTris = (int*) indices.GetUnsafePtr();
                _result = Compute(_vhacdPtr, (float*) pVerts, (uint) vertices.Length,
                    (uint*) pTris, (uint) indices.Length / 3,
                    _paramPtr, cbPtr);
            }

            vertices.Dispose();
            indices.Dispose();
            return _result;
        }

        /// <summary>
        /// Decompose mesh into convex hulls asynchronously
        /// </summary>
        /// <param name="mesh">unity mesh</param>
        /// <param name="cb">user callback (optional)</param>
        /// <returns></returns>
        public async Task<bool> ConvexDecomposeAsync(Mesh mesh, UserCallback cb = null)
        {
            using var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            var meshData = meshDataArray[0];
            var vertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Persistent);
            meshData.GetVertices(vertices);
            var indices = new NativeArray<int>((int) mesh.GetIndexCount(0), Allocator.Persistent);
            meshData.GetIndices(indices, 0);

            if (cb == null)
                cb = (_, _, _, _) => { };
            var cbPtr = Marshal.GetFunctionPointerForDelegate(cb);

            await Task.Run(() =>
            {
                unsafe
                {
                    Vector3* pVerts = (Vector3*) vertices.GetUnsafePtr();
                    int* pTris = (int*) indices.GetUnsafePtr();
                    _result = Compute(_vhacdPtr, (float*) pVerts, (uint) vertices.Length,
                        (uint*) pTris, (uint) indices.Length / 3,
                        _paramPtr, cbPtr);
                }
            });

            vertices.Dispose();
            indices.Dispose();
            return _result;
        }

        /// <summary>
        /// Get number of convex hulls
        /// </summary>
        /// <returns></returns>
        public int GetNConvexHulls()
        {
            if (!_result) return 0;
            return (int) GetNConvexHulls(_vhacdPtr);
        }

        /// <summary>
        /// Get Convex Hull at index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public VhacdConvexHull GetConvexHull(int index)
        {
            if (!_result) throw new InvalidOperationException("Decomposition failed. There is no convex hulls.");
            int numConvexHulls = GetNConvexHulls();
            if (index >= numConvexHulls)
            {
                throw new IndexOutOfRangeException(
                    $"Index out of range. There are only {numConvexHulls} convex hulls.");
            }

            IntPtr hullPointer = Marshal.AllocHGlobal(Marshal.SizeOf<VhacdConvexHull>());
            GetConvexHull(out var handle, _vhacdPtr, (uint) index, hullPointer);
            var hull = Marshal.PtrToStructure<VhacdConvexHull>(hullPointer);
            handle.Dispose();
            return hull;
        }

        /// <summary>
        /// Convert all convex hulls into a mesh data array
        /// </summary>
        /// <returns></returns>
        public Mesh.MeshDataArray ConvertConvexHullsIntoMesh()
        {
            if (!_result) throw new InvalidOperationException("Decomposition failed. There is no convex hulls.");
            int nConvexHulls = GetNConvexHulls();
            var meshDataArray = Mesh.AllocateWritableMeshData(nConvexHulls);
            for (int i = 0; i < nConvexHulls; i++)
            {
                unsafe
                {
                    IntPtr hullPointer = Marshal.AllocHGlobal(Marshal.SizeOf<VhacdConvexHull>());
                    GetConvexHull(out var handle, _vhacdPtr, (uint) i, hullPointer);
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
                    meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexData.Length),
                        MeshUpdateFlags.DontValidateIndices);

                    Marshal.FreeHGlobal(hullPointer);
                    handle.Dispose();
                }
            }

            return meshDataArray;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (_paramPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(_paramPtr);
            if (_vhacdPtr != IntPtr.Zero)
                ReleaseVHACD(_vhacdPtr);
        }
    }

    /// <summary>
    /// Safe handle for convex hull disposal
    /// </summary>
    public class ConvexHullSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public ConvexHullSafeHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            UnityVhacd.ReleaseConvexHull(handle);
            return true;
        }
    }
}