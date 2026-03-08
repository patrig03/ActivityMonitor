#pragma once
#include <cstdint>
#include <string>
#include <vulkan/vulkan_core.h>

#include "InitFlags.h"


namespace UI {
    inline uint32_t imageIndex;
    inline VkResult result;
    inline bool shouldClose = false;

    void Init(const int& width, const int& height, const std::string& title = "no title yet", const init_flags& flags = init_flags::None);
    void Destroy();

    // returns true if frame is ready to be used
    // if frame expired will return false and recreate frame
    bool BeginFrame();
    void EndFrame();

    [[nodiscard]] bool WindowShouldClose();
    
    void CreateDockspace();
}
