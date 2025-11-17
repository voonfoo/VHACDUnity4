using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
        private const string DLLName = "__Internal";
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

        [DllImport(DLLName, EntryPoint = "GetConvexHull")]
        private static extern IntPtr GetConvexHull(IntPtr iVhacd, uint index, IntPtr convexHull);

        [DllImport(DLLName, EntryPoint = "DeleteConvexHull")]
        private static extern void DeleteConvexHull(IntPtr handle);

        [DllImport(DLLName)]
        private static extern void ReleaseVHACD(IntPtr iVhacd);

        #endregion

        /// <summary>
        /// vhacd parameter pointer
        /// </summary>
        private IntPtr _paramPtr;

        /// <summary>
        /// ivhacd pointer
        /// </summary>
        private IntPtr _vhacdPtr;

        private bool _result;
        private bool _disposed;
        private readonly object _lock = new object();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parameters">paramaters</param>
        public UnityVhacd(VhacdParameters parameters)
        {
            _paramPtr = Marshal.AllocHGlobal(Marshal.SizeOf(parameters));
            try
            {
                Marshal.StructureToPtr(parameters, _paramPtr, false);
                _vhacdPtr = CreateVHACD(_paramPtr);
                if (_vhacdPtr == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to create VHACD instance");
                }
            }
            catch
            {
                if (_paramPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_paramPtr);
                    _paramPtr = IntPtr.Zero;
                }
                throw;
            }
        }

        /// <summary>
        /// Decompose mesh into convex hulls synchronously
        /// </summary>
        /// <param name="mesh">unity mesh</param>
        /// <param name="cb">user callback (optional)</param>
        /// <returns></returns>
        public bool ConvexDecompose(Mesh mesh, UserCallback cb = null)
        {
            ThrowIfDisposed();
            
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            lock (_lock)
            {
                using var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
                var meshData = meshDataArray[0];
                var vertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Persistent);
                var indices = new NativeArray<int>((int)mesh.GetIndexCount(0), Allocator.Persistent);
                
                try
                {
                    meshData.GetVertices(vertices);
                    meshData.GetIndices(indices, 0);

                    // Pin the delegate to prevent GC collection during native call
                    GCHandle callbackHandle = default;
                    IntPtr cbPtr = IntPtr.Zero;
                    
                    if (cb != null)
                    {
                        callbackHandle = GCHandle.Alloc(cb);
                        cbPtr = Marshal.GetFunctionPointerForDelegate(cb);
                    }

                    try
                    {
                        unsafe
                        {
                            Vector3* pVerts = (Vector3*)vertices.GetUnsafePtr();
                            int* pTris = (int*)indices.GetUnsafePtr();
                            _result = Compute(_vhacdPtr, (float*)pVerts, (uint)vertices.Length,
                                (uint*)pTris, (uint)indices.Length / 3,
                                _paramPtr, cbPtr);
                        }
                    }
                    finally
                    {
                        if (callbackHandle.IsAllocated)
                        {
                            callbackHandle.Free();
                        }
                    }
                }
                finally
                {
                    vertices.Dispose();
                    indices.Dispose();
                }

                return _result;
            }
        }

        /// <summary>
        /// Decompose mesh into convex hulls asynchronously
        /// </summary>
        /// <param name="mesh">unity mesh</param>
        /// <param name="cb">user callback (optional)</param>
        /// <returns></returns>
        public async Task<bool> ConvexDecomposeAsync(Mesh mesh, UserCallback cb = null)
        {
            ThrowIfDisposed();
            
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            using var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            var meshData = meshDataArray[0];
            var vertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Persistent);
            var indices = new NativeArray<int>((int)mesh.GetIndexCount(0), Allocator.Persistent);

            try
            {
                meshData.GetVertices(vertices);
                meshData.GetIndices(indices, 0);

                // Pin the delegate to prevent GC collection during native call
                GCHandle callbackHandle = default;
                IntPtr cbPtr = IntPtr.Zero;
                
                if (cb != null)
                {
                    callbackHandle = GCHandle.Alloc(cb);
                    cbPtr = Marshal.GetFunctionPointerForDelegate(cb);
                }

                try
                {
                    await Task.Run(() =>
                    {
                        lock (_lock)
                        {
                            unsafe
                            {
                                Vector3* pVerts = (Vector3*)vertices.GetUnsafePtr();
                                int* pTris = (int*)indices.GetUnsafePtr();
                                _result = Compute(_vhacdPtr, (float*)pVerts, (uint)vertices.Length,
                                    (uint*)pTris, (uint)indices.Length / 3,
                                    _paramPtr, cbPtr);
                            }
                        }
                    });
                }
                finally
                {
                    if (callbackHandle.IsAllocated)
                    {
                        callbackHandle.Free();
                    }
                }
            }
            finally
            {
                vertices.Dispose();
                indices.Dispose();
            }

            return _result;
        }

        /// <summary>
        /// Get number of convex hulls
        /// </summary>
        /// <returns></returns>
        public int GetNConvexHulls()
        {
            ThrowIfDisposed();
            
            if (!_result) return 0;
            
            lock (_lock)
            {
                return (int)GetNConvexHulls(_vhacdPtr);
            }
        }

        /// <summary>
        /// Get Convex Hull at index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public VhacdConvexHull GetConvexHull(int index)
        {
            ThrowIfDisposed();
            
            if (!_result) 
                throw new InvalidOperationException("Decomposition failed. There are no convex hulls.");
            
            int numConvexHulls = GetNConvexHulls();
            if (index < 0 || index >= numConvexHulls)
            {
                throw new IndexOutOfRangeException(
                    $"Index out of range. There are only {numConvexHulls} convex hulls.");
            }

            lock (_lock)
            {
                IntPtr hullPointer = IntPtr.Zero;
                IntPtr convexHullHandle = IntPtr.Zero;
                
                try
                {
                    hullPointer = Marshal.AllocHGlobal(Marshal.SizeOf<VhacdConvexHull>());
                    convexHullHandle = GetConvexHull(_vhacdPtr, (uint)index, hullPointer);
                    
                    if (convexHullHandle == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("Failed to get convex hull from native library");
                    }
                    
                    var hull = Marshal.PtrToStructure<VhacdConvexHull>(hullPointer);
                    
                    // Clean up native convex hull handle
                    DeleteConvexHull(convexHullHandle);
                    
                    return hull;
                }
                finally
                {
                    if (hullPointer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(hullPointer);
                    }
                }
            }
        }

        /// <summary>
        /// Convert all convex hulls into a mesh data array
        /// </summary>
        /// <returns></returns>
        public Mesh.MeshDataArray ConvertConvexHullsIntoMesh()
        {
            ThrowIfDisposed();
            
            if (!_result) 
                throw new InvalidOperationException("Decomposition failed. There are no convex hulls.");
            
            int nConvexHulls = GetNConvexHulls();
            var meshDataArray = Mesh.AllocateWritableMeshData(nConvexHulls);
            
            lock (_lock)
            {
                for (int i = 0; i < nConvexHulls; i++)
                {
                    IntPtr hullPointer = IntPtr.Zero;
                    IntPtr convexHullHandle = IntPtr.Zero;
                    
                    try
                    {
                        unsafe
                        {
                            hullPointer = Marshal.AllocHGlobal(Marshal.SizeOf<VhacdConvexHull>());
                            convexHullHandle = GetConvexHull(_vhacdPtr, (uint)i, hullPointer);
                            
                            if (convexHullHandle == IntPtr.Zero)
                            {
                                throw new InvalidOperationException($"Failed to get convex hull {i} from native library");
                            }
                            
                            var hull = Marshal.PtrToStructure<VhacdConvexHull>(hullPointer);

                            var meshData = meshDataArray[i];
                            meshData.SetVertexBufferParams((int)hull.NPoints,
                                new VertexAttributeDescriptor(VertexAttribute.Position));
                            var vertexData = meshData.GetVertexData<Vector3>();
                            var hullVertex = hull.Points;
                            
                            for (int vertIdx = 0; vertIdx < hull.NPoints; vertIdx++)
                            {
                                vertexData[vertIdx] =
                                    new Vector3((float)hullVertex->X, (float)hullVertex->Y, (float)hullVertex->Z);
                                hullVertex++;
                            }

                            meshData.SetIndexBufferParams((int)hull.NTriangles * 3, IndexFormat.UInt16);
                            var indexData = meshData.GetIndexData<ushort>();
                            var pTriangle = hull.Triangles;
                            
                            for (int triangleCount = 0; triangleCount < hull.NTriangles; triangleCount += 1)
                            {
                                indexData[triangleCount * 3 + 0] = (ushort)(pTriangle->Index0);
                                indexData[triangleCount * 3 + 1] = (ushort)(pTriangle->Index1);
                                indexData[triangleCount * 3 + 2] = (ushort)(pTriangle->Index2);

                                pTriangle++;
                            }

                            meshData.subMeshCount = 1;
                            meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexData.Length),
                                MeshUpdateFlags.DontValidateIndices);
                        }
                    }
                    finally
                    {
                        if (hullPointer != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(hullPointer);
                        }
                        if (convexHullHandle != IntPtr.Zero)
                        {
                            DeleteConvexHull(convexHullHandle);
                        }
                    }
                }
            }

            return meshDataArray;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                if (_vhacdPtr != IntPtr.Zero)
                {
                    ReleaseVHACD(_vhacdPtr);
                    _vhacdPtr = IntPtr.Zero;
                }

                if (_paramPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_paramPtr);
                    _paramPtr = IntPtr.Zero;
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~UnityVhacd()
        {
            Dispose(false);
        }

        /// <summary>
        /// Throw if disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}