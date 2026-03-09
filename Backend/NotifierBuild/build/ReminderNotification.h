#pragma once

#include <string>
#include "imgui.h"
#include "UI-core.h"

struct notification_data
{
    const char* message = nullptr;
    int button_count = 0;
    char** button_names = nullptr;
    
    const ImVec2 size = {420, 200};
    ImVec2 monitor_size = {1920, 1080};
    bool open = true;
};

inline void TextCentered(const char* text)
{
    const auto windowWidth = ImGui::GetContentRegionAvail().x;
    const auto windowHeight = ImGui::GetContentRegionAvail().y;

    // First pass: count lines and calculate total height
    int line_count = 0;
    const char* temp_ptr = text;
    while (*temp_ptr)
    {
        const char* line_end = temp_ptr;
        while (*line_end && !(*line_end == '\\' && *(line_end + 1) == 'n')) { line_end++; }
        line_count++;
        if (*line_end == '\\' && *(line_end + 1) == 'n') { temp_ptr = line_end + 2; }
        else { break; }
    }

    const auto lineHeight = ImGui::GetTextLineHeightWithSpacing();
    const auto totalTextHeight = lineHeight * line_count;
    const auto startY = (windowHeight - totalTextHeight) * 0.5f;

    ImGui::SetCursorPosY(startY);

    const char* ptr = text;
    while (*ptr)
    {
        const char* line_end = ptr;
        while (*line_end && !(*line_end == '\\' && *(line_end + 1) == 'n')) { line_end++; }

        std::string line(ptr, line_end);
        const auto textWidth = ImGui::CalcTextSize(line.c_str()).x;

        if (textWidth < windowWidth)
        {
            ImGui::SetCursorPosX((windowWidth - textWidth) * 0.5f);
        }
        ImGui::Text("%s", line.c_str());

        if (*line_end == '\\' && *(line_end + 1) == 'n') { ptr = line_end + 2; }
        else { break; }
    }
}
inline void RenderNotification(notification_data& data){
    while (!UI::WindowShouldClose() && data.open) {
        if (UI::BeginFrame()) {
                
            ImGui::SetNextWindowPos(ImVec2(data.monitor_size.x / 2 - data.size.x / 2, 
                data.monitor_size.y / 2 - data.size.y / 2), ImGuiCond_Appearing);
            ImGui::SetNextWindowSize(data.size);
            
            ImGui::Begin("Notification", &data.open, ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoMove | 
                ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags_NoTitleBar);
            ImGui::Dummy(ImVec2(0, 5));
            ImGui::Dummy(ImVec2(5, 0));
            ImGui::SameLine();
            
            ImGui::BeginChild("child1");
            
            ImGui::BeginChild("child", ImVec2(data.size.x, ImGui::GetContentRegionAvail().y - 60));
            TextCentered(data.message);
            ImGui::EndChild();
            
            ImGui::Dummy(ImVec2(0, ImGui::GetContentRegionAvail().y - 65));
            
            for (int i = 0; i < data.button_count; i++) {
                if (ImGui::Button(data.button_names[i], ImVec2((ImGui::GetContentRegionAvail().x / (data.button_count - i)) - 10, 40))) {
                    std::printf("%s", data.button_names[i]);
                    data.open = false;
                }
                ImGui::SameLine();
            }
            
            ImGui::EndChild();
            ImGui::End();
            UI::EndFrame();
        }
    }

    UI::Destroy();
}

