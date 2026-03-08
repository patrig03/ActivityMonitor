
#pragma once

#include <vector>
#include <vulkan/vulkan_core.h>

struct SwapChainSupportDetails;

namespace SwapchainManager {
    inline VkSwapchainKHR swapchain;
    inline VkFormat swapchainImageFormat = VK_FORMAT_UNDEFINED;;
    inline VkExtent2D swapchainExtent = {0, 0};;
    inline std::vector<VkImage> swapchainImages;
    inline std::vector<VkImageView> swapchainImageViews;
    inline std::vector<VkFramebuffer> swapchainFramebuffers;

    void Create();
    void Recreate();
    void Cleanup();
    void CreateImageViews();
    void CreateFramebuffers();

    [[nodiscard]] VkSurfaceFormatKHR chooseSurfaceFormat(const std::vector<VkSurfaceFormatKHR>& availableFormats);
    [[nodiscard]] VkPresentModeKHR choosePresentMode(const std::vector<VkPresentModeKHR>& availablePresentModes);
    [[nodiscard]] VkExtent2D chooseExtent(const VkSurfaceCapabilitiesKHR& capabilities);
    [[nodiscard]] SwapChainSupportDetails querySwapChainSupport();
    [[nodiscard]] VkImageView createImageView(const VkImage &image, const VkFormat &format,
                                              const VkImageAspectFlags &aspectFlags, const uint32_t &mipLevel);
};