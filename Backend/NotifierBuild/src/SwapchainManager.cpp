
#include "SwapchainManager.h"

#include <array>
#include <stdexcept>
#include <bits/stl_algo.h>

#include "DeviceManager.h"
#include "PipelineManager.h"
#include "VulkanInstance.h"
#include "WindowManager.h"


void SwapchainManager::Create() {
        // find swapchain details
    auto [capabilities, formats, presentModes] = querySwapChainSupport();
    auto [format, colorSpace] = chooseSurfaceFormat(formats);
    const VkPresentModeKHR presentMode = choosePresentMode(presentModes);
    const VkExtent2D extent = chooseExtent(capabilities);

    // get image count (at least 1)
    uint32_t imageCount = capabilities.minImageCount + 1;
    if (capabilities.maxImageCount > 0 && imageCount > capabilities.maxImageCount) {
        imageCount = capabilities.maxImageCount;
    }
    VkSwapchainCreateInfoKHR createInfo{
        .sType            = VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR,
        .surface          = VulkanInstance::surface,
        .minImageCount    = imageCount,
        .imageFormat      = format,
        .imageColorSpace  = colorSpace,
        .imageExtent      = extent,
        .imageArrayLayers = 1,
        .imageUsage       = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
    };
    const uint32_t queueFamilyIndices[] = {DeviceManager::queueFamily.graphicsFamily.value(),
        DeviceManager::queueFamily.presentFamily.value()};

    if (DeviceManager::queueFamily.graphicsFamily == DeviceManager::queueFamily.presentFamily &&
        DeviceManager::queueFamily.graphicsFamily == DeviceManager::queueFamily.computeFamily) {
        createInfo.imageSharingMode         = VK_SHARING_MODE_EXCLUSIVE;
        createInfo.queueFamilyIndexCount    = 0;
        createInfo.pQueueFamilyIndices      = nullptr;
    } else {
        createInfo.imageSharingMode         = VK_SHARING_MODE_CONCURRENT;
        createInfo.queueFamilyIndexCount    = 3;
        createInfo.pQueueFamilyIndices      = queueFamilyIndices;
    }
    createInfo.preTransform     = capabilities.currentTransform;
    createInfo.compositeAlpha   = VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR;
    createInfo.presentMode      = presentMode;
    createInfo.clipped          = VK_TRUE;
    createInfo.oldSwapchain     = VK_NULL_HANDLE;

    // create swapchain
    if (vkCreateSwapchainKHR(DeviceManager::device, &createInfo, nullptr, &swapchain) != VK_SUCCESS) {
        throw std::runtime_error("SwapChainManager: failed to create swap chain!");
    }
    // get swapchain images, image format and extent
    vkGetSwapchainImagesKHR(DeviceManager::device, swapchain, &imageCount, nullptr);
    swapchainImages.resize(imageCount);
    vkGetSwapchainImagesKHR(DeviceManager::device, swapchain, &imageCount, swapchainImages.data());

    swapchainImageFormat    = format;
    swapchainExtent         = extent;
}

void SwapchainManager::Recreate() {
    int width = 0, height = 0;
    while (width == 0 || height == 0) {
        WindowManager::GetFramebufferSize(&width, &height);
        WindowManager::WaitEvents();
    }

    vkDeviceWaitIdle(DeviceManager::device);

    Cleanup();

    Create();
    CreateImageViews();
    CreateFramebuffers();
}

void SwapchainManager::Cleanup() {
    for (const auto framebuffer : swapchainFramebuffers) { vkDestroyFramebuffer(DeviceManager::device, framebuffer, nullptr); }
    for (const auto imageView : swapchainImageViews) { vkDestroyImageView(DeviceManager::device, imageView, nullptr); }

    vkDestroySwapchainKHR(DeviceManager::device, swapchain, nullptr);
}

void SwapchainManager::CreateImageViews() {
    swapchainImageViews.resize(swapchainImages.size());

    for (uint32_t i = 0; i < swapchainImages.size(); i++) {
        swapchainImageViews[i] = createImageView(swapchainImages[i], swapchainImageFormat, VK_IMAGE_ASPECT_COLOR_BIT, 1);
    }
}

void SwapchainManager::CreateFramebuffers() {
    swapchainFramebuffers.resize(swapchainImageViews.size());
    for (size_t i = 0; i < swapchainImageViews.size(); i++) {
        std::array attachments = { swapchainImageViews[i] };
        VkFramebufferCreateInfo framebufferInfo{};
        framebufferInfo.sType           = VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO;
        framebufferInfo.renderPass      = PipelineManager::graphicsRenderPass;
        framebufferInfo.attachmentCount = attachments.size();
        framebufferInfo.pAttachments    = attachments.data();
        framebufferInfo.width           = swapchainExtent.width;
        framebufferInfo.height          = swapchainExtent.height;
        framebufferInfo.layers          = 1;

        if (vkCreateFramebuffer(DeviceManager::device, &framebufferInfo, nullptr, &swapchainFramebuffers[i]) != VK_SUCCESS) {
            throw std::runtime_error("failed to create framebuffer!");
        }
    }
}

VkSurfaceFormatKHR SwapchainManager::chooseSurfaceFormat(const std::vector<VkSurfaceFormatKHR> &availableFormats) {
    for (const auto& availableFormat : availableFormats) {
        if (availableFormat.format == VK_FORMAT_B8G8R8A8_UNORM &&
            availableFormat.colorSpace == VK_COLOR_SPACE_SRGB_NONLINEAR_KHR) {
            return availableFormat;
            }
    }
    return availableFormats[0];
}

VkPresentModeKHR SwapchainManager::choosePresentMode(const std::vector<VkPresentModeKHR> &availablePresentModes) {
    for (const auto& availablePresentMode : availablePresentModes) {
        if (availablePresentMode == VK_PRESENT_MODE_MAILBOX_KHR) {
            return availablePresentMode;
        }
    }
    return VK_PRESENT_MODE_FIFO_KHR;
}

VkExtent2D SwapchainManager::chooseExtent(const VkSurfaceCapabilitiesKHR &capabilities) {
    if (capabilities.currentExtent.width != UINT32_MAX) {
        return capabilities.currentExtent;
    }
    int width, height;
    WindowManager::GetFramebufferSize(&width, &height);
    VkExtent2D actualExtent = { static_cast<uint32_t>(width), static_cast<uint32_t>(height)};
    actualExtent.width  = std::clamp(actualExtent.width,  capabilities.minImageExtent.width,  capabilities.maxImageExtent.width);
    actualExtent.height = std::clamp(actualExtent.height, capabilities.minImageExtent.height, capabilities.maxImageExtent.height);
    return actualExtent;
}

SwapChainSupportDetails SwapchainManager::querySwapChainSupport() {
    SwapChainSupportDetails details;
    //get surface  capabilities
    vkGetPhysicalDeviceSurfaceCapabilitiesKHR(DeviceManager::physicalDevice, VulkanInstance::surface, &details.capabilities);

    //get surface formats
    uint32_t formatCount;
    vkGetPhysicalDeviceSurfaceFormatsKHR(DeviceManager::physicalDevice, VulkanInstance::surface, &formatCount, nullptr);
    if (formatCount != 0) {
        details.formats.resize(formatCount);
        vkGetPhysicalDeviceSurfaceFormatsKHR(DeviceManager::physicalDevice, VulkanInstance::surface, &formatCount, details.formats.data());
    }

    //get present modes
    uint32_t presentModeCount;
    vkGetPhysicalDeviceSurfacePresentModesKHR(DeviceManager::physicalDevice, VulkanInstance::surface, &presentModeCount, nullptr);
    if (presentModeCount != 0) {
        details.presentModes.resize(presentModeCount);
        vkGetPhysicalDeviceSurfacePresentModesKHR(DeviceManager::physicalDevice, VulkanInstance::surface, &presentModeCount, details.presentModes.data());
    }

    return details;
}

VkImageView SwapchainManager::createImageView(const VkImage &image, const VkFormat &format,
    const VkImageAspectFlags &aspectFlags, const uint32_t &mipLevel) {

    VkImageViewCreateInfo viewInfo{};
    viewInfo.sType      = VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO;
    viewInfo.image      = image;
    viewInfo.viewType   = VK_IMAGE_VIEW_TYPE_2D;
    viewInfo.format     = format;

    viewInfo.subresourceRange.aspectMask        = aspectFlags;
    viewInfo.subresourceRange.baseMipLevel      = 0;
    viewInfo.subresourceRange.levelCount        = mipLevel;
    viewInfo.subresourceRange.baseArrayLayer    = 0;
    viewInfo.subresourceRange.layerCount        = 1;

    VkImageView imageView;
    if (vkCreateImageView(DeviceManager::device, &viewInfo, nullptr, &imageView) != VK_SUCCESS) {
        throw std::runtime_error("failed to create texture image view!");
    }

    return imageView;
}
