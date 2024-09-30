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
             VHACD::IVHACD::Parameters *params,
             UnityVHACDUserCallback callback) {
    UnityVhacdUserCallbackImpl *cb = nullptr;
    if (callback) {
        cb = new UnityVhacdUserCallbackImpl(callback);
        params->m_callback = cb;
    }
    iface->Compute(points, countPoints, triangles, countTriangles, *params);
    while (!iface->IsReady()) {
        std::this_thread::sleep_for(std::chrono::nanoseconds(10000)); // s
    }
    if (callback)
        delete cb;
    return iface->IsReady();
}

uint32_t GetNConvexHulls(VHACD::IVHACD *iface) {
    return iface->GetNConvexHulls();
}

VHACD::IVHACD::ConvexHull* GetConvexHull(VHACD::IVHACD *iface, uint32_t index, UnityConvexHull *unityCh){
    auto *ch = new VHACD::IVHACD::ConvexHull();
    iface->GetConvexHull(index, *ch);

    unityCh->points = ch->m_points.data();
    unityCh->n_points = static_cast<int>(ch->m_points.size());
    unityCh->triangles = ch->m_triangles.data();
    unityCh->n_triangles = static_cast<int>(ch->m_triangles.size());
    return ch;
}

void DeleteConvexHull(VHACD::IVHACD::ConvexHull *ch) {
    delete ch;
}

void ReleaseVHACD(VHACD::IVHACD *iface) {
    iface->Release();
}