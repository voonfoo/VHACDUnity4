#ifndef UnityVHACD_H
#define UnityVHACD_H

#if defined(_WIN32)
#ifdef UNITY_VHACD_EXPORTS
        #define UNITYVHACD_API __declspec(dllexport)
    #else
        #define UNITYVHACD_API __declspec(dllimport)
    #endif
#elif defined(__APPLE__) || defined(__linux__)
#if __GNUC__ >= 4
        #define UNITYVHACD_API __attribute__ ((visibility ("default")))
    #else
        #define UNITYVHACD_API
    #endif
#else
#error "Unknown compiler"
#endif

#define ENABLE_VHACD_IMPLEMENTATION 1
#define VHACD_DISABLE_THREADING 0
#include "VHACD.h"
#include "stdlib.h"
typedef intptr_t UnityConvexHullSafetyHandle;

struct UnityConvexHull {
    VHACD::Vertex* points;
    uint32_t n_points;
    VHACD::Triangle* triangles;
    uint32_t n_triangles;
};

#ifdef __cplusplus
extern "C"
{
#endif
    UNITYVHACD_API VHACD::IVHACD* CreateVHACD(VHACD::IVHACD::Parameters* p);
    UNITYVHACD_API bool Compute(VHACD::IVHACD* iface,
                 const float* const points,
                 const uint32_t countPoints,
                 const uint32_t* const triangles,
                 const uint32_t countTriangles,
                 const VHACD::IVHACD::Parameters* params);

    UNITYVHACD_API uint32_t GetNConvexHulls(VHACD::IVHACD* iface);
    UNITYVHACD_API void GetConvexHull(UnityConvexHullSafetyHandle* handle, VHACD::IVHACD* iface, uint32_t index, UnityConvexHull* unityCh);
    UNITYVHACD_API void ReleaseConvexHull(UnityConvexHullSafetyHandle handle);
    UNITYVHACD_API void ReleaseVHACD(VHACD::IVHACD* iface);
#ifdef __cplusplus
}

#endif

#endif //UnityVHACD_H
