
#include "CommandManager.h"
#include "DeviceManager.h"

#include <stdexcept>


void CommandManager::CreateGraphicsCommandPool() {
    VkCommandPoolCreateInfo poolInfo{};
    poolInfo.sType              = VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO;
    poolInfo.flags              = VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT;
    poolInfo.queueFamilyIndex   = DeviceManager::queueFamily.graphicsFamily.value();

    if (vkCreateCommandPool(DeviceManager::device, &poolInfo, nullptr, &graphicsCommandPool)) {
        throw std::runtime_error("CommandSystem class: failed to create command pool");
    }
}
void CommandManager::CreateComputeCommandPool() {
    VkCommandPoolCreateInfo poolInfo{};
    poolInfo.sType              = VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO;
    poolInfo.flags              = VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT;
    poolInfo.queueFamilyIndex   = DeviceManager::queueFamily.computeFamily.value();

    if (vkCreateCommandPool(DeviceManager::device, &poolInfo, nullptr, &computeCommandPool)) {
        throw std::runtime_error("CommandSystem class: failed to create command pool");
    }
}
void CommandManager::CreateGraphicsBuffers() {
    graphicsCommandBuffers.resize(maxFramesInFlight);

    VkCommandBufferAllocateInfo allocInfo{};
    allocInfo.sType                 = VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO;
    allocInfo.commandPool           = graphicsCommandPool;
    allocInfo.level                 = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
    allocInfo.commandBufferCount    = static_cast<uint32_t>(graphicsCommandBuffers.size());

    if (vkAllocateCommandBuffers(DeviceManager::device, &allocInfo, graphicsCommandBuffers.data()) != VK_SUCCESS) {
        throw std::runtime_error("CommandSystem class: failed to create command buffers");
    }
}
void CommandManager::CreateComputeBuffers() {
    computeCommandBuffers.resize(maxFramesInFlight);

    VkCommandBufferAllocateInfo allocInfo{};
    allocInfo.sType                 = VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO;
    allocInfo.commandPool           = graphicsCommandPool;
    allocInfo.level                 = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
    allocInfo.commandBufferCount    = static_cast<uint32_t>(computeCommandBuffers.size());

    if (vkAllocateCommandBuffers(DeviceManager::device, &allocInfo, computeCommandBuffers.data()) != VK_SUCCESS) {
        throw std::runtime_error("CommandSystem class: failed to create command buffers");
    }
}

void CommandManager::Cleanup() {

}

void CommandManager::CreateSyncObjects() {
    imageAvailableSemaphores.resize(maxFramesInFlight);
    renderFinishedSemaphores.resize(maxFramesInFlight);
    inFlightFences.resize(maxFramesInFlight);

    VkSemaphoreCreateInfo semaphoreInfo{};
    semaphoreInfo.sType = VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO;

    VkFenceCreateInfo fenceInfo{};
    fenceInfo.sType = VK_STRUCTURE_TYPE_FENCE_CREATE_INFO;
    fenceInfo.flags = VK_FENCE_CREATE_SIGNALED_BIT;

    for (size_t i = 0; i < maxFramesInFlight; i++) {
        if (vkCreateSemaphore(DeviceManager::device, &semaphoreInfo, nullptr, &imageAvailableSemaphores[i]) != VK_SUCCESS ||
            vkCreateSemaphore(DeviceManager::device, &semaphoreInfo, nullptr, &renderFinishedSemaphores[i]) != VK_SUCCESS ||
            vkCreateFence(DeviceManager::device, &fenceInfo, nullptr, &inFlightFences[i]) != VK_SUCCESS) {

            throw std::runtime_error("CommandSystem class: failed to create synchronization objects for a frame");
        }
    }
}

VkFence& CommandManager::currentInFlightFence() { return inFlightFences[currentFrame]; }
VkSemaphore& CommandManager::currentImageAvailableSemaphore() { return imageAvailableSemaphores[currentFrame]; }
VkSemaphore& CommandManager::currentRenderFinishedSemaphore() { return renderFinishedSemaphores[currentFrame]; }
VkCommandBuffer& CommandManager::currentGraphicsCommandBuffer() { return graphicsCommandBuffers[currentFrame]; }

void CommandManager::changeToNextFrame() { currentFrame = (currentFrame + 1) % maxFramesInFlight; }


