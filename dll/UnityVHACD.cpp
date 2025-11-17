#include "UnityVHACD.h"
#include <memory>
#include <stdexcept>

VHACD::IVHACD *CreateVHACD(VHACD::IVHACD::Parameters *p) {
    if (!p) {
        return nullptr;
    }
    try {
        if (p->m_asyncACD)
            return VHACD::CreateVHACD_ASYNC();
        else
            return VHACD::CreateVHACD();
    } catch (...) {
        return nullptr;
    }
}

bool Compute(VHACD::IVHACD *iface,
             const float *const points,
             const uint32_t countPoints,
             const uint32_t *const triangles,
             const uint32_t countTriangles,
             VHACD::IVHACD::Parameters *params,
             UnityVHACDUserCallback callback) {
    // Validate input parameters
    if (!iface || !points || !triangles || !params) {
        return false;
    }
    if (countPoints == 0 || countTriangles == 0) {
        return false;
    }
    
    // Use RAII for exception safety
    std::unique_ptr<UnityVhacdUserCallbackImpl> cb;
    try {
        if (callback) {
            cb = std::make_unique<UnityVhacdUserCallbackImpl>(callback);
            params->m_callback = cb.get();
        }
        
        bool result = iface->Compute(points, countPoints, triangles, countTriangles, *params);
        if (!result) {
            return false;
        }
        
        while (!iface->IsReady()) {
            std::this_thread::sleep_for(std::chrono::nanoseconds(10000));
        }
        
        return iface->IsReady();
    } catch (...) {
        return false;
    }
}

uint32_t GetNConvexHulls(VHACD::IVHACD *iface) {
    if (!iface) {
        return 0;
    }
    try {
        return iface->GetNConvexHulls();
    } catch (...) {
        return 0;
    }
}

VHACD::IVHACD::ConvexHull* GetConvexHull(VHACD::IVHACD *iface, uint32_t index, UnityConvexHull *unityCh){
    // Validate input parameters
    if (!iface || !unityCh) {
        return nullptr;
    }
    
    try {
        auto *ch = new VHACD::IVHACD::ConvexHull();
        if (!ch) {
            return nullptr;
        }
        
        iface->GetConvexHull(index, *ch);
        
        // Copy data to Unity-managed memory instead of passing pointers to vector data
        // The vectors in ch will remain valid until DeleteConvexHull is called
        unityCh->points = ch->m_points.data();
        unityCh->n_points = static_cast<uint32_t>(ch->m_points.size());
        unityCh->triangles = ch->m_triangles.data();
        unityCh->n_triangles = static_cast<uint32_t>(ch->m_triangles.size());
        
        return ch;
    } catch (...) {
        return nullptr;
    }
}

void DeleteConvexHull(VHACD::IVHACD::ConvexHull *ch) {
    if (ch) {
        try {
            delete ch;
        } catch (...) {
            // Suppress exceptions in cleanup code
        }
    }
}

void ReleaseVHACD(VHACD::IVHACD *iface) {
    if (iface) {
        try {
            iface->Release();
        } catch (...) {
            // Suppress exceptions in cleanup code
        }
    }
}