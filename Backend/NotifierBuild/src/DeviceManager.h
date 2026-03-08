
#pragma once

#include <optional>
#include <vector>
#include <vulkan/vulkan_core.h>


struct QueueFamilyIndices {
    std::optional<uint32_t> graphicsFamily;
    std::optional<uint32_t> presentFamily;
    std::optional<uint32_t> computeFamily;
    [[nodiscard]] bool isComplete() const;
};

struct SwapChainSupportDetails {
    VkSurfaceCapabilitiesKHR capabilities;
    std::vector<VkSurfaceFormatKHR> formats;
    std::vector<VkPresentModeKHR> presentModes;
};

namespace DeviceManager {
    inline VkPhysicalDevice physicalDevice;
    inline VkDevice device;

    inline VkQueue graphicsQueue;
    inline VkQueue presentQueue;
    inline VkQueue computeQueue;

    inline QueueFamilyIndices queueFamily;

    void SelectPhysicalDevice();
    void CreateLogicalDevice();
    void Cleanup();

    inline std::vector deviceExtensions = {"VK_KHR_swapchain"};;

    [[nodiscard]] bool isDeviceSuitable(const VkPhysicalDevice &device);
    [[nodiscard]] QueueFamilyIndices findQueueFamilies(const VkPhysicalDevice &device) ;
    [[nodiscard]] bool checkDeviceExtensionSupport(const VkPhysicalDevice &device);
    [[nodiscard]] SwapChainSupportDetails querySwapChainSupport(const VkPhysicalDevice &device) ;
};