
#include "DescriptorManager.h"

#include <array>
#include <stdexcept>

#include "DeviceManager.h"
#include "PipelineManager.h"
#include "VulkanInstance.h"
#include "WindowManager.h"

#include "imgui.h"
#include "imgui_internal.h"
#include "imgui_impl_vulkan.h"
#include "imgui_impl_glfw.h"


void DescriptorManager::CreateDescriptorSetLayouts() {
    createGraphicsSetLayout();
    createComputeSetLayout();
}

void DescriptorManager::createGraphicsSetLayout() {
    VkDescriptorSetLayoutBinding samplerLayoutBinding{};
    samplerLayoutBinding.binding            = 0;
    samplerLayoutBinding.descriptorCount    = 1;
    samplerLayoutBinding.descriptorType     = VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
    samplerLayoutBinding.pImmutableSamplers = nullptr;
    samplerLayoutBinding.stageFlags         = VK_SHADER_STAGE_FRAGMENT_BIT;

    const std::array bindings = { samplerLayoutBinding };
    VkDescriptorSetLayoutCreateInfo layoutInfo{};
    layoutInfo.sType            = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO;
    layoutInfo.bindingCount     = bindings.size();
    layoutInfo.pBindings        = bindings.data();

    if (vkCreateDescriptorSetLayout(DeviceManager::device, &layoutInfo, nullptr, &graphicsDescriptorSetLayout) != VK_SUCCESS) {
        throw std::runtime_error("DescriptorManager class: failed to create descriptor set layout!");
    }
}

void DescriptorManager::createComputeSetLayout() {
    VkDescriptorSetLayoutBinding imageBinding{};
    imageBinding.binding             = 0;
    imageBinding.descriptorType      = VK_DESCRIPTOR_TYPE_STORAGE_IMAGE;
    imageBinding.descriptorCount     = 1;
    imageBinding.stageFlags          = VK_SHADER_STAGE_COMPUTE_BIT;
    imageBinding.pImmutableSamplers  = nullptr;

    const std::array bindings = { imageBinding };
    VkDescriptorSetLayoutCreateInfo layoutInfo{};
    layoutInfo.sType        = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO;
    layoutInfo.bindingCount = bindings.size();
    layoutInfo.pBindings    = bindings.data();

    if (vkCreateDescriptorSetLayout(DeviceManager::device, &layoutInfo, nullptr, &computeDescriptorSetLayout) != VK_SUCCESS) {
        throw std::runtime_error("Failed to create descriptor set layout for compute pipeline.");
    }
}

void DescriptorManager::CreateDescriptorPool() {
    const VkDescriptorPoolSize pool_sizes[] = {
        { VK_DESCRIPTOR_TYPE_SAMPLER, 1000 },
        { VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, 1000 },
        { VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE, 1000 },
        { VK_DESCRIPTOR_TYPE_STORAGE_IMAGE, 1000 },
        { VK_DESCRIPTOR_TYPE_UNIFORM_TEXEL_BUFFER, 1000 },
        { VK_DESCRIPTOR_TYPE_STORAGE_TEXEL_BUFFER, 1000 },
        { VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, 1000 },
        { VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, 1000 },
        { VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC, 1000 },
        { VK_DESCRIPTOR_TYPE_STORAGE_BUFFER_DYNAMIC, 1000 },
        { VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT, 1000 },
    };
    VkDescriptorPoolCreateInfo pool_info = {};
    pool_info.sType         = VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO;
    pool_info.flags         = VK_DESCRIPTOR_POOL_CREATE_FREE_DESCRIPTOR_SET_BIT;
    pool_info.maxSets       = 1000 * IM_ARRAYSIZE(pool_sizes);
    pool_info.poolSizeCount = static_cast<uint32_t>(IM_ARRAYSIZE(pool_sizes));
    pool_info.pPoolSizes    = pool_sizes;

    if (vkCreateDescriptorPool(DeviceManager::device, &pool_info, nullptr, &descriptorPool) != VK_SUCCESS) {
        throw std::runtime_error("DescriptorManager class: failed to create descriptor pool!");
    }
}

void DescriptorManager::InitImGui() {
    // Setup Dear ImGui context
    IMGUI_CHECKVERSION();
    ImGui::CreateContext();

    ImGui::StyleColorsDark();

    ImGuiIO& io = ImGui::GetIO();
    io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard;     // Enable Keyboard Controls
    io.ConfigFlags |= ImGuiConfigFlags_NavEnableGamepad;      // Enable Gamepad Controls
    io.ConfigFlags |= ImGuiConfigFlags_DockingEnable;         // IF using Docking Branch
    io.ConfigFlags |= ImGuiConfigFlags_ViewportsEnable;

    io.Fonts->AddFontFromFileTTF(FONTS_DIR "FiraCodeNerdFont-Light.ttf", 24);

    // Setup Platform/Renderer backends
    ImGui_ImplGlfw_InitForVulkan(WindowManager::window, true);
    ImGui_ImplVulkan_InitInfo init_info = {};
    init_info.Instance                  = VulkanInstance::instance;
    init_info.PhysicalDevice            = DeviceManager::physicalDevice;
    init_info.Device                    = DeviceManager::device;
    init_info.QueueFamily               = DeviceManager::queueFamily.graphicsFamily.value();
    init_info.Queue                     = DeviceManager::graphicsQueue;
    init_info.PipelineCache             = VK_NULL_HANDLE;
    init_info.DescriptorPool            = descriptorPool;
    init_info.RenderPass                = PipelineManager::graphicsRenderPass;
    init_info.Subpass                   = 0;
    init_info.MinImageCount             = 2;
    init_info.ImageCount                = 2;
    init_info.MSAASamples               = VK_SAMPLE_COUNT_1_BIT;
    init_info.Allocator                 = VK_NULL_HANDLE;
    init_info.CheckVkResultFn           = checkVkResult;
    ImGui_ImplVulkan_Init(&init_info);
    ImGui_ImplVulkan_CreateFontsTexture();
}

void DescriptorManager::Cleanup() {
    ImGui_ImplVulkan_DestroyFontsTexture();
    ImGui_ImplVulkan_Shutdown();
    ImGui_ImplGlfw_Shutdown();
    ImGui::DestroyContext();
}

void DescriptorManager::checkVkResult(VkResult err) {
    if (err == 0) { return; }
    fprintf(stderr, "DescriptorManager class: failed result check, VkResult = %d\n", err);
    if (err < 0) { abort(); }
}
