#pragma once
#include <iostream>

#include "imgui.h"
#include <X11/Xlib.h>


struct soft_lock_data
{
    const char* message = nullptr;
    char* password = nullptr;
    ImVec2 window_size = {1000, 1000};
    ImVec2 window_position = {500, 500};
    unsigned long window_id = 0;
    
    bool open = true;
};


inline int g_x11WindowError = 0;

inline int SafeX11ErrorHandler(Display*, XErrorEvent* errorEvent) {
    g_x11WindowError = errorEvent->error_code;
    return 0;
}

inline bool getWindowData(Display* display, Window window, int& absX, int& absY, unsigned int& width, unsigned int& height) {
    if (!display || window == 0) { return false; }

    absX = 0;
    absY = 0;
    width = 0;
    height = 0;

    g_x11WindowError = 0;
    const auto previousHandler = XSetErrorHandler(SafeX11ErrorHandler);

    XWindowAttributes attrs;
    const Status attrStatus = XGetWindowAttributes(display, window, &attrs);
    XSync(display, False);

    if (g_x11WindowError != 0 || attrStatus == 0) {
        XSetErrorHandler(previousHandler);
        return false;
    }

    width = static_cast<unsigned int>(attrs.width);
    height = static_cast<unsigned int>(attrs.height);

    Window child = 0;
    int x = 0;
    int y = 0;

    g_x11WindowError = 0;
    const Bool translateStatus = XTranslateCoordinates(
        display,
        window,
        DefaultRootWindow(display),
        0,
        0,
        &x,
        &y,
        &child
    );
    XSync(display, False);

    XSetErrorHandler(previousHandler);

    if (g_x11WindowError != 0 || translateStatus == False) {
        return false;
    }

    absX = x;
    absY = y;
    return true;
}

inline void RenderSoftLock(soft_lock_data& data) {
    constexpr int buffer_size = 128;
    auto password_buffer = new char[buffer_size];
    memset(password_buffer, 0, buffer_size);
    
    Display* display = XOpenDisplay(nullptr);
    if (!display) {
        std::cerr << "Failed to open X display\n";
        delete[] password_buffer;
        return;
    }
    
    while (data.open) {
        int x, y;
        unsigned int w, h;

        if (!getWindowData(display, data.window_id, x, y, w, h)) {
            std::cerr << "Failed to get window geometry\n";
            break;
        }
        data.window_position = ImVec2(x, y);
        data.window_size = ImVec2(w, h);
        
        if (UI::BeginFrame()) {
                
            ImGui::SetNextWindowPos(data.window_position);
            ImGui::SetNextWindowSize(data.window_size);
            
            ImGui::Begin("Lock", nullptr, ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoMove | 
                ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags_NoTitleBar);
            
            TextCentered(data.message);

            float buttonWidth = ImGui::CalcTextSize("Unlock").x + ImGui::GetStyle().FramePadding.x * 2.0f + 20.0f;
            float totalWidth = data.window_size.x* 0.5f + ImGui::GetStyle().ItemSpacing.x + buttonWidth;

            ImGui::Dummy(ImVec2(0, 10));
            ImGui::SetCursorPosX((data.window_size.x - totalWidth) * 0.5f);

            ImGui::SetNextItemWidth(data.window_size.x* 0.5f);
            const bool enter = ImGui::InputTextWithHint(" ", "Password", password_buffer, buffer_size, 
                ImGuiInputTextFlags_EnterReturnsTrue);
            ImGui::SameLine();
            if (ImGui::Button("Unlock") || enter) {
                if (strcmp(password_buffer, data.password) == 0) {
                    data.open = false;
                }
            }

            ImGui::End();
            UI::EndFrame();
        }
    }
    delete[] password_buffer;
    UI::Destroy();
}
