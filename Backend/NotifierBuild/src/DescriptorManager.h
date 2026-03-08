
#pragma once
#include <vulkan/vulkan_core.h>

namespace DescriptorManager {
    inline VkDescriptorPool descriptorPool;

    inline VkDescriptorSetLayout graphicsDescriptorSetLayout;
    inline VkDescriptorSetLayout computeDescriptorSetLayout;

    void CreateDescriptorSetLayouts();
    void CreateDescriptorPool();
    void InitImGui();
    void Cleanup();

    void createGraphicsSetLayout();
    void createComputeSetLayout();
    void checkVkResult(VkResult err);
};