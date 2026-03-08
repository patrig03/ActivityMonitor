
#include "WindowManager.h"

#include <iostream>
#include <ostream>

#include "InitFlags.h"


void WindowManager::Init(const int &width, const int &height, const std::string& title, const init_flags &flags) {
    glfwInit();
    glfwWindowHint(GLFW_CLIENT_API, GLFW_NO_API);
    
    glfwWindowHint(GLFW_RESIZABLE, (flags & init_flags::NotResizable ? GLFW_FALSE : GLFW_TRUE));
    glfwWindowHint(GLFW_FLOATING,  (flags & init_flags::Floating ? GLFW_TRUE : GLFW_FALSE));
    glfwWindowHint(GLFW_MAXIMIZED, (flags & init_flags::Maximized ? GLFW_TRUE : GLFW_FALSE));
    glfwWindowHint(GLFW_DECORATED, (flags & init_flags::NoDecoration ? GLFW_FALSE: GLFW_TRUE));
    
    glfwWindowHint(GLFW_VISIBLE, GLFW_FALSE);

    window = glfwCreateWindow(width, height, title.data(), nullptr, nullptr);
    window_title = title;
    glfwSetFramebufferSizeCallback(window, FramebufferResizeCallback);
}

void WindowManager::FramebufferResizeCallback(GLFWwindow *window, int width, int height) {
    framebufferResized = true;
}

void WindowManager::Cleanup() {
    glfwDestroyWindow(window);
    glfwTerminate();
}

void WindowManager::GetFramebufferSize(int *width, int *height) {
    glfwGetFramebufferSize(window, width, height);
}

void WindowManager::WaitEvents() {
    glfwWaitEvents();
}
