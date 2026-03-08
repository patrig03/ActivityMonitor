#include "VulkanInstance.h"

#include <stdexcept>
#include <GLFW/glfw3.h>

#include "WindowManager.h"


void VulkanInstance::Create() {
    VkApplicationInfo appInfo{};
    appInfo.sType               = VK_STRUCTURE_TYPE_APPLICATION_INFO;
    appInfo.pApplicationName    = WindowManager::window_title.data();
    appInfo.applicationVersion  = VK_MAKE_VERSION(1, 0, 0);
    appInfo.pEngineName         = "No Engine";
    appInfo.engineVersion       = VK_MAKE_VERSION(1, 0, 0);
    appInfo.apiVersion          = VK_API_VERSION_1_0;

    uint32_t glfwExtensionCount = 0;
    const char** glfwExtensions = glfwGetRequiredInstanceExtensions(&glfwExtensionCount);

    VkInstanceCreateInfo createInfo{};
    createInfo.sType                    = VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO;
    createInfo.pApplicationInfo         = &appInfo;
    createInfo.enabledExtensionCount    = glfwExtensionCount;
    createInfo.ppEnabledExtensionNames  = glfwExtensions;

    if (vkCreateInstance(&createInfo, nullptr, &instance) != VK_SUCCESS) {
        throw std::runtime_error("VulkanInstance class: failed to create instance");
    }
}

void VulkanInstance::CreateSurface() {
    if (glfwCreateWindowSurface(instance, WindowManager::window, nullptr, &surface) != VK_SUCCESS) {
        throw std::runtime_error("VulkanInstance class: failed to create window surface");
    }
}

void VulkanInstance::Destroy() {
    vkDestroySurfaceKHR(instance, surface, nullptr);
    vkDestroyInstance(instance, nullptr);
}