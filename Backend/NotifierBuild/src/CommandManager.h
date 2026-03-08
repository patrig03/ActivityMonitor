#pragma once
#include <vector>
#include <vulkan/vulkan_core.h>


namespace CommandManager {
    inline VkCommandPool graphicsCommandPool;
    inline VkCommandPool computeCommandPool;
    inline std::vector<VkCommandBuffer> graphicsCommandBuffers;
    inline std::vector<VkCommandBuffer> computeCommandBuffers;
    inline std::vector<VkFence> inFlightFences;
    inline std::vector<VkSemaphore> imageAvailableSemaphores;
    inline std::vector<VkSemaphore> renderFinishedSemaphores;

    inline int maxFramesInFlight = 2;
    inline uint32_t currentFrame = 0;

    void CreateGraphicsCommandPool();
    void CreateGraphicsBuffers();

    void CreateComputeCommandPool();
    void CreateComputeBuffers();

    void Cleanup();
    void CreateSyncObjects();

    VkFence& currentInFlightFence();
    VkSemaphore& currentImageAvailableSemaphore();
    VkSemaphore& currentRenderFinishedSemaphore();
    VkCommandBuffer& currentGraphicsCommandBuffer();

    void changeToNextFrame();
};