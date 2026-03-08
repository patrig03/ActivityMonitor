
#pragma once

#include <string>
#include <GLFW/glfw3.h>

#include "InitFlags.h"

namespace WindowManager {
    inline GLFWwindow* window = nullptr;
    inline bool framebufferResized = false;
    inline std::string window_title;

    void Init(const int& width, const int& height, const std::string& title, const init_flags& flags = init_flags::None);
    void FramebufferResizeCallback(GLFWwindow* window, int width, int height);
    void Cleanup();

    void GetFramebufferSize(int *width, int *height);
    void WaitEvents();
}
