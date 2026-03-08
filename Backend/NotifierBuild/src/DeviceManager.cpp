
#include "DeviceManager.h"
#include "VulkanInstance.h"

#include <set>
#include <stdexcept>
#include <vector>


// Helper function inside QueueFamilyIndices for DeviceManager
bool QueueFamilyIndices::isComplete() const {
    return graphicsFamily.has_value() && presentFamily.has_value() && computeFamily.has_value();
}

void DeviceManager::SelectPhysicalDevice() {
    uint32_t deviceCount = 0;
    vkEnumeratePhysicalDevices(VulkanInstance::instance, &deviceCount, nullptr);
    if (deviceCount == 0) {
        throw std::runtime_error("failed to find GPUs with Vulkan support");
    }
    std::vector<VkPhysicalDevice> devices(deviceCount);
    vkEnumeratePhysicalDevices(VulkanInstance::instance, &deviceCount, devices.data());
    for (const auto& device : devices) {
        if (isDeviceSuitable(device)) {
            physicalDevice = device;
            break;
        }
    }
    if (physicalDevice == VK_NULL_HANDLE) {
        throw std::runtime_error("failed to find GPUs with Vulkan support");
    }
}

void DeviceManager::CreateLogicalDevice() {
    std::vector<VkDeviceQueueCreateInfo> queueCreateInfos;
    const std::set uniqueQueueFamilies = { queueFamily.graphicsFamily.value(),
        queueFamily.presentFamily.value(), queueFamily.computeFamily.value() };
    constexpr float queuePriority = 1.0f;

    for (const uint32_t queueFamily : uniqueQueueFamilies) {
        VkDeviceQueueCreateInfo queueCreateInfo{
            .sType               = VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO,
            .queueFamilyIndex    = queueFamily,
            .queueCount          = 1,
            .pQueuePriorities    = &queuePriority,
        };
        queueCreateInfos.push_back(queueCreateInfo);
    }

    VkPhysicalDeviceFeatures deviceFeatures{};
    deviceFeatures.samplerAnisotropy = VK_TRUE;
    deviceFeatures.sampleRateShading = VK_TRUE;

    VkDeviceCreateInfo createInfo{};
    createInfo.sType                    = VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO;
    createInfo.pQueueCreateInfos        = queueCreateInfos.data();
    createInfo.queueCreateInfoCount     = static_cast<uint32_t>(queueCreateInfos.size());
    createInfo.pEnabledFeatures         = &deviceFeatures;
    createInfo.enabledExtensionCount    = static_cast<uint32_t>(deviceExtensions.size());
    createInfo.ppEnabledExtensionNames  = deviceExtensions.data();
    createInfo.enabledLayerCount        = 0;

    if (vkCreateDevice(physicalDevice, &createInfo, nullptr, &device) != VK_SUCCESS) {
        throw std::runtime_error("failed to create logical device");
    }
    vkGetDeviceQueue(device, queueFamily.graphicsFamily.value(), 0, &graphicsQueue);
    vkGetDeviceQueue(device, queueFamily.presentFamily.value(), 0, &presentQueue);
    vkGetDeviceQueue(device, queueFamily.computeFamily.value(), 0, &computeQueue);
}

void DeviceManager::Cleanup() {
    vkDestroyDevice(device, nullptr);
}



bool DeviceManager::isDeviceSuitable(const VkPhysicalDevice &device) {
    VkPhysicalDeviceProperties deviceProperties;
    vkGetPhysicalDeviceProperties(device, &deviceProperties);

    VkPhysicalDeviceFeatures supportedFeatures;
    vkGetPhysicalDeviceFeatures(device, &supportedFeatures);

    queueFamily = findQueueFamilies(device);
    const bool extensionsSupported = checkDeviceExtensionSupport(device);

    bool swapChainAdequate = false;
    if (extensionsSupported) {
        auto [capabilities, formats, presentModes] = querySwapChainSupport(device);
        swapChainAdequate = !formats.empty() && !presentModes.empty();
    }

    if (deviceProperties.deviceType == VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU &&
        queueFamily.isComplete() && extensionsSupported && swapChainAdequate && supportedFeatures.samplerAnisotropy) {
        return true;
        }
    if (queueFamily.isComplete() && extensionsSupported && swapChainAdequate && supportedFeatures.samplerAnisotropy) {
        return true;
    }
    return false;
}

QueueFamilyIndices DeviceManager::findQueueFamilies(const VkPhysicalDevice &device) {
    QueueFamilyIndices indices;
    uint32_t queueFamilyCount = 0;
    vkGetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, nullptr);
    std::vector<VkQueueFamilyProperties> queueFamilies(queueFamilyCount);
    vkGetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilies.data());

    for (uint32_t i = 0; i < queueFamilyCount; i++) {
        VkBool32 presentSupport = false;
        vkGetPhysicalDeviceSurfaceSupportKHR(device, i, VulkanInstance::surface, &presentSupport);

        if (presentSupport) { indices.presentFamily = i; }
        if (queueFamilies[i].queueFlags & VK_QUEUE_GRAPHICS_BIT) { indices.graphicsFamily = i; }
        if (queueFamilies[i].queueFlags & VK_QUEUE_COMPUTE_BIT) { indices.computeFamily = i; }

        if (indices.isComplete()) { break; }
    }
    return indices;
}

bool DeviceManager::checkDeviceExtensionSupport(const VkPhysicalDevice &device) {
    uint32_t extensionCount;
    vkEnumerateDeviceExtensionProperties(device, nullptr, &extensionCount, nullptr);
    std::vector<VkExtensionProperties> availableExtensions(extensionCount);
    vkEnumerateDeviceExtensionProperties(device, nullptr, &extensionCount, availableExtensions.data());

    std::set<std::string> requiredExtensions(deviceExtensions.begin(),deviceExtensions.end());

    for (const auto&[extensionName, specVersion] : availableExtensions) {
        requiredExtensions.erase(extensionName);
    }

    return requiredExtensions.empty();
}

SwapChainSupportDetails DeviceManager::querySwapChainSupport(const VkPhysicalDevice &device) {

    SwapChainSupportDetails details;
    //get surface  capabilities
    vkGetPhysicalDeviceSurfaceCapabilitiesKHR(device, VulkanInstance::surface, &details.capabilities);

    //get surface formats
    uint32_t formatCount;
    vkGetPhysicalDeviceSurfaceFormatsKHR(device, VulkanInstance::surface, &formatCount, nullptr);
    if (formatCount != 0) {
        details.formats.resize(formatCount);
        vkGetPhysicalDeviceSurfaceFormatsKHR(device, VulkanInstance::surface, &formatCount, details.formats.data());
    }

    //get present modes
    uint32_t presentModeCount;
    vkGetPhysicalDeviceSurfacePresentModesKHR(device, VulkanInstance::surface, &presentModeCount, nullptr);
    if (presentModeCount != 0) {
        details.presentModes.resize(presentModeCount);
        vkGetPhysicalDeviceSurfacePresentModesKHR(device, VulkanInstance::surface, &presentModeCount, details.presentModes.data());
    }

    return details;
}
