#ifndef UnityVHACD_H
#define UnityVHACD_H
#define UNITY_VHACD_EXPORTS

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

typedef intptr_t UnityConvexHullSafetyHandle;

struct UnityConvexHull {
    VHACD::Vertex *points;
    uint32_t n_points;
    VHACD::Triangle *triangles;
    uint32_t n_triangles;
};

typedef void (*UnityVHACDUserCallback)(const double overallProgress,
                                       const double stageProgress,
                                       const char *stage,
                                       const char *operation);

class UnityVhacdUserCallbackImpl : public VHACD::IVHACD::IUserCallback {
    UnityVHACDUserCallback m_callback = nullptr;

public:
    explicit UnityVhacdUserCallbackImpl(UnityVHACDUserCallback callback) : m_callback(callback) {
    }

    void Update(const double overallProgress,
                const double stageProgress,
                const char *const stage,
                const char *operation) override {
        if (m_callback) {
            m_callback(overallProgress, stageProgress, stage, operation);
        }
    }
};

#ifdef __cplusplus
extern "C" {
#endif

UNITYVHACD_API VHACD::IVHACD *CreateVHACD(VHACD::IVHACD::Parameters *p);

UNITYVHACD_API bool Compute(VHACD::IVHACD *iface,
                            const float *const points,
                            const uint32_t countPoints,
                            const uint32_t *const triangles,
                            const uint32_t countTriangles,
                            VHACD::IVHACD::Parameters *params,
                            UnityVHACDUserCallback callback);

UNITYVHACD_API uint32_t GetNConvexHulls(VHACD::IVHACD *iface);

UNITYVHACD_API VHACD::IVHACD::ConvexHull* GetConvexHull(VHACD::IVHACD *iface, uint32_t index, UnityConvexHull *unityCh);

UNITYVHACD_API void DeleteConvexHull(VHACD::IVHACD::ConvexHull *ch);

UNITYVHACD_API void ReleaseVHACD(VHACD::IVHACD *iface);
#ifdef __cplusplus
}

#endif

#endif //UnityVHACD_H
