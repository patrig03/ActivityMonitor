#pragma once
#include <iostream>
#include <chrono>

#include "imgui.h"
#include "ReminderNotification.h"
#include "SoftLock.h"
#include "UI-core.h"

struct hard_lock_data {
    const char* message = nullptr;
    int seconds = 0;
    ImVec2 window_size = {1000, 1000};
    ImVec2 window_position = {500, 500};
    unsigned long window_id = 0;

    bool open = true;
};

inline void RenderHardLock(hard_lock_data& data) {
    Display* display = XOpenDisplay(nullptr);
    if (!display) {
        std::cerr << "Failed to open X display\n";
        return;
    }

    const auto start_time = std::chrono::steady_clock::now();

    while (data.open) {
        const auto now = std::chrono::steady_clock::now();
        const auto elapsed = std::chrono::duration_cast<std::chrono::seconds>(now - start_time).count();
        const int remaining_seconds = std::max(0, data.seconds - static_cast<int>(elapsed));

        if (remaining_seconds <= 0) {
            data.open = false;
            break;
        }

        int x = 0;
        int y = 0;
        unsigned int w = 0;
        unsigned int h = 0;

        if (!getWindowData(display, data.window_id, x, y, w, h)) {
            std::cerr << "Failed to get window geometry\n";
            break;
        }
        data.window_position = ImVec2(static_cast<float>(x), static_cast<float>(y));
        data.window_size = ImVec2(static_cast<float>(w), static_cast<float>(h));

        if (UI::BeginFrame()) {

            ImGui::SetNextWindowPos(data.window_position);
            ImGui::SetNextWindowSize(data.window_size);

            ImGui::Begin("Lock", nullptr, ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoMove |
                ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags_NoTitleBar);

            TextCentered(data.message);

            std::string timer;
            if (remaining_seconds >= 3600) {
                // Format as hours and minutes
                const int hours = remaining_seconds / 3600;
                const int minutes = (remaining_seconds % 3600) / 60;
                const int seconds = remaining_seconds % 60;
            
                timer = std::to_string(hours) + (hours == 1 ? " hour" : " hours");
                if (minutes > 0 || seconds > 0) {
                    timer += ", " + std::to_string(minutes) + (minutes == 1 ? " minute" : " minutes");
                }
                if (seconds > 0) {
                    timer += ", " + std::to_string(seconds) + (seconds == 1 ? " second" : " seconds");
                }
                timer += " left";
            } else if (remaining_seconds >= 60) {
                // Format as minutes and seconds
                const int minutes = remaining_seconds / 60;
                const int seconds = remaining_seconds % 60;
            
                timer = std::to_string(minutes) + (minutes == 1 ? " minute" : " minutes");
                if (seconds > 0) {
                    timer += ", " + std::to_string(seconds) + (seconds == 1 ? " second" : " seconds");
                }
                timer += " left";
            } else {
                // Format as seconds only
                timer = std::to_string(remaining_seconds) +
                    (remaining_seconds == 1 ? " second left" : " seconds left");
            }
            const float text_width = ImGui::CalcTextSize(timer.c_str()).x;

            ImGui::Dummy(ImVec2(0, 10));
            ImGui::SetCursorPosX((data.window_size.x - text_width) * 0.5f);
            ImGui::Text("%s", timer.c_str());

            ImGui::End();
            UI::EndFrame();
        }
    }

    XCloseDisplay(display);
    UI::Destroy();
}