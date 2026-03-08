
#include "UI-core.h"

#include <stdexcept>
#include "imgui_impl_glfw.h"
#include "imgui_impl_vulkan.h"

#include "CommandManager.h"
#include "DescriptorManager.h"
#include "DeviceManager.h"
#include "PipelineManager.h"
#include "SwapchainManager.h"
#include "VulkanInstance.h"
#include "WindowManager.h"

void UI::Init(const int& width, const int& height, const std::string& title, const init_flags& flags) {
    WindowManager::Init(width, height, title, flags);
    VulkanInstance::Create();
    VulkanInstance::CreateSurface();
    DeviceManager::SelectPhysicalDevice();
    DeviceManager::CreateLogicalDevice();
    SwapchainManager::Create();
    SwapchainManager::CreateImageViews();
    PipelineManager::CreateRenderPass();
    DescriptorManager::CreateDescriptorSetLayouts();
    PipelineManager::Create();
    CommandManager::CreateGraphicsCommandPool();
    CommandManager::CreateGraphicsBuffers();
    DescriptorManager::CreateDescriptorPool();
    SwapchainManager::CreateFramebuffers();
    CommandManager::CreateSyncObjects();
    DescriptorManager::InitImGui();
}

void UI::Destroy() {
    vkDeviceWaitIdle(DeviceManager::device);

    DescriptorManager::Cleanup();
    SwapchainManager::Cleanup();
    PipelineManager::Cleanup();
    DeviceManager::Cleanup();
    VulkanInstance::Destroy();
    WindowManager::Cleanup();
}

bool UI::BeginFrame() {
    vkWaitForFences(DeviceManager::device, 1, &CommandManager::currentInFlightFence(), VK_TRUE, UINT64_MAX);

    // poll events, begin new ImGui frame
    glfwPollEvents();
    ImGui_ImplGlfw_NewFrame();
    ImGui_ImplVulkan_NewFrame();
    ImGui::NewFrame();

    // acquire next swapchain image and verify validity
    result = vkAcquireNextImageKHR(DeviceManager::device, SwapchainManager::swapchain, UINT64_MAX,
                                    CommandManager::currentImageAvailableSemaphore(), VK_NULL_HANDLE, &imageIndex);
    if (result == VK_ERROR_OUT_OF_DATE_KHR || result == VK_SUBOPTIMAL_KHR || WindowManager::framebufferResized) {
        ImGui::Render();
        ImGui::EndFrame();
        ImGui::UpdatePlatformWindows();
        ImGui::RenderPlatformWindowsDefault();

        SwapchainManager::Recreate();
        WindowManager::framebufferResized = false;

        return false;
    }
    vkResetFences(DeviceManager::device, 1, &CommandManager::currentInFlightFence());
    return true;
}

void UI::EndFrame() {
    // Render ImGui
    ImGui::Render();

    ImGui::UpdatePlatformWindows();
    ImGui::RenderPlatformWindowsDefault();

    vkResetCommandBuffer(CommandManager::currentGraphicsCommandBuffer(), 0);
    VkCommandBufferBeginInfo beginInfo{};
    beginInfo.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;
    vkBeginCommandBuffer(CommandManager::currentGraphicsCommandBuffer(), &beginInfo);

    VkRenderPassBeginInfo renderPassInfo{};
    renderPassInfo.sType = VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO;
    renderPassInfo.renderPass = PipelineManager::graphicsRenderPass;
    renderPassInfo.framebuffer = SwapchainManager::swapchainFramebuffers[imageIndex];
    renderPassInfo.renderArea.offset = {0, 0};
    renderPassInfo.renderArea.extent = SwapchainManager::swapchainExtent;

    VkClearValue clearColor = {{{0.0f, 0.0f, 0.0f, 1.0f}}};
    renderPassInfo.clearValueCount = 1;
    renderPassInfo.pClearValues = &clearColor;
    vkCmdBeginRenderPass(CommandManager::currentGraphicsCommandBuffer(), &renderPassInfo, VK_SUBPASS_CONTENTS_INLINE);

    ImGui_ImplVulkan_RenderDrawData(ImGui::GetDrawData(), CommandManager::currentGraphicsCommandBuffer());
    vkCmdEndRenderPass(CommandManager::currentGraphicsCommandBuffer());
    vkEndCommandBuffer(CommandManager::currentGraphicsCommandBuffer());

    VkSubmitInfo submitInfo{};
    const VkSemaphore waitSemaphores[]  = { CommandManager::currentImageAvailableSemaphore() };
    const VkSemaphore signalSemaphores[]= { CommandManager::currentRenderFinishedSemaphore() };
    VkPipelineStageFlags waitStages[]   = { VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT };
    submitInfo.sType                    = VK_STRUCTURE_TYPE_SUBMIT_INFO;
    submitInfo.waitSemaphoreCount       = 1;
    submitInfo.pWaitSemaphores          = waitSemaphores;
    submitInfo.pWaitDstStageMask        = waitStages;
    submitInfo.commandBufferCount       = 1;
    submitInfo.pCommandBuffers          = &CommandManager::currentGraphicsCommandBuffer();
    submitInfo.signalSemaphoreCount     = 1;
    submitInfo.pSignalSemaphores        = signalSemaphores;

    if (vkQueueSubmit(DeviceManager::graphicsQueue, 1, &submitInfo, CommandManager::currentInFlightFence()) != VK_SUCCESS) {
        throw std::runtime_error("Engine class::mainloop: failed to submit draw command buffer!");
    }

    VkPresentInfoKHR presentInfo{};
    presentInfo.sType                   = VK_STRUCTURE_TYPE_PRESENT_INFO_KHR;
    presentInfo.waitSemaphoreCount      = 1;
    presentInfo.pWaitSemaphores         = signalSemaphores;
    VkSwapchainKHR swapChains[]         = {SwapchainManager::swapchain};
    presentInfo.swapchainCount          = 1;
    presentInfo.pSwapchains             = swapChains;
    presentInfo.pImageIndices           = &imageIndex;

    result = vkQueuePresentKHR(DeviceManager::presentQueue, &presentInfo);

    if (result == VK_ERROR_OUT_OF_DATE_KHR || result == VK_SUBOPTIMAL_KHR) {
        SwapchainManager::Recreate();
        WindowManager::framebufferResized = false;
    } else if (result != VK_SUCCESS) {
        throw std::runtime_error("Engine class::mainloop: failed to present swap chain image!");
    }

    CommandManager::changeToNextFrame();
}

bool UI::WindowShouldClose(){
    return glfwWindowShouldClose(WindowManager::window) || shouldClose;
}

void UI::CreateDockspace()
{
    static ImGuiDockNodeFlags dockspace_flags = ImGuiDockNodeFlags_None;

    constexpr ImGuiWindowFlags window_flags = ImGuiWindowFlags_NoBringToFrontOnFocus |
        ImGuiWindowFlags_NoTitleBar | ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags_NoResize;
    const ImGuiViewport* viewport = ImGui::GetMainViewport();
    ImGui::SetNextWindowPos(viewport->WorkPos);
    ImGui::SetNextWindowSize(viewport->WorkSize);
    ImGui::SetNextWindowViewport(viewport->ID);
    ImGui::PushStyleVar(ImGuiStyleVar_WindowRounding, 0.0f);
    ImGui::PushStyleVar(ImGuiStyleVar_WindowBorderSize, 0.0f);
    ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(0.0f, 0.0f));
    
    ImGui::Begin("DockSpace", nullptr, window_flags);
    ImGui::PopStyleVar();
    ImGui::PopStyleVar(2);

    const ImGuiID dockspace_id = ImGui::GetID("MyDockSpace");

    ImGui::DockSpace(dockspace_id, ImVec2(0.0f, 0.0f), dockspace_flags);
    ImGui::End();
}
