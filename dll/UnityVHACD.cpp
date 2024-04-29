#include "UnityVHACD.h"

VHACD::IVHACD *CreateVHACD(VHACD::IVHACD::Parameters *p) {
    if (p->m_asyncACD)
        return VHACD::CreateVHACD_ASYNC();
    else
        return VHACD::CreateVHACD();
}

bool Compute(VHACD::IVHACD *iface,
             const float *const points,
             const uint32_t countPoints,
             const uint32_t *const triangles,
             const uint32_t countTriangles,
             const VHACD::IVHACD::Parameters *params) {
    iface->Compute(points, countPoints, triangles, countTriangles, *params);
    while (!iface->IsReady()) {
        std::this_thread::sleep_for(std::chrono::nanoseconds(10000)); // s
    }
    return iface->IsReady();
}

uint32_t GetNConvexHulls(VHACD::IVHACD *iface) {
    return iface->GetNConvexHulls();
}

void GetConvexHull(UnityConvexHullSafetyHandle* handle, VHACD::IVHACD *iface, uint32_t index, UnityConvexHull *unityCh) {
    VHACD::IVHACD::ConvexHull *ch = new VHACD::IVHACD::ConvexHull();
    iface->GetConvexHull(index, *ch);

    *handle = reinterpret_cast<UnityConvexHullSafetyHandle>(&ch->m_points);
    unityCh->points = ch->m_points.data();
    unityCh->n_points = ch->m_points.size();
    unityCh->triangles = ch->m_triangles.data();
    unityCh->n_triangles = ch->m_triangles.size();
}

void ReleaseConvexHull(UnityConvexHullSafetyHandle handle) {
    VHACD::IVHACD::ConvexHull* ch = reinterpret_cast<VHACD::IVHACD::ConvexHull*>(handle);
    delete ch;
}

void ReleaseVHACD(VHACD::IVHACD *iface) {
    iface->Release();
}