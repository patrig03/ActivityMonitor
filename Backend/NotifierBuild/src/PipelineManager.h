
#pragma once

#include <string>
#include <vector>
#include <vulkan/vulkan_core.h>


namespace PipelineManager {
    inline VkPipeline graphicsPipeline;
    inline VkPipeline computePipeline;

    inline VkPipelineLayout graphicsPipelineLayout;
    inline VkPipelineLayout computePipelineLayout;

    inline VkRenderPass graphicsRenderPass;

    void Create();
    void Cleanup();

    void CreateRenderPass();

    void createGraphicsPipeline();
    void createComputePipeline(const std::string &shader_path);
    void createGraphicsPipelineLayout();
    void createComputePipelineLayout();

    [[nodiscard]] std::vector<char> readShaderBinary(const std::string& filename);
    [[nodiscard]] VkShaderModule createShaderModule(const std::vector<char>& code);
};