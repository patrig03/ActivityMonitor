#pragma once
#include <vulkan/vulkan_core.h>


namespace VulkanInstance {
    inline VkInstance instance;
    inline VkSurfaceKHR surface;

    void Create();
    void Destroy();
    void CreateSurface();
};