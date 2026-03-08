
#include <iostream>
#include "imgui.h"
#include "WindowManager.h"
#include "../UI-core.h" 

void SetupImGuiStyle() {
	ImGuiStyle& style = ImGui::GetStyle();
	
	style.Alpha = 1.0f;
	style.DisabledAlpha = 0.6f;
	style.WindowPadding = ImVec2(10.0f, 10.0f);
	style.WindowRounding = 0.0f;
	style.WindowBorderSize = 1.0f;
	style.WindowMinSize = ImVec2(32.0f, 32.0f);
	style.WindowTitleAlign = ImVec2(0.5f, 0.5f);
	style.WindowMenuButtonPosition = ImGuiDir_None;
	style.ChildRounding = 6.0f;
	style.ChildBorderSize = 1.0f;
	style.PopupRounding = 6.0f;
	style.PopupBorderSize = 1.0f;
	style.FramePadding = ImVec2(8.0f, 6.0f);
	style.FrameRounding = 6.0f;
	style.FrameBorderSize = 1.0f;
	style.ItemSpacing = ImVec2(6.0f, 6.0f);
	style.ItemInnerSpacing = ImVec2(4.0f, 4.0f);
	style.CellPadding = ImVec2(4.0f, 2.0f);
	style.IndentSpacing = 11.0f;
	style.ColumnsMinSpacing = 6.0f;
	style.ScrollbarSize = 14.0f;
	style.ScrollbarRounding = 6.0f;
	style.GrabMinSize = 10.0f;
	style.GrabRounding = 6.0f;
	style.TabRounding = 6.0f;
	style.TabBorderSize = 1.0f;
	style.TabMinWidthForCloseButton = 0.0f;
	style.ColorButtonPosition = ImGuiDir_Right;
	style.ButtonTextAlign = ImVec2(0.5f, 0.5f);
	style.SelectableTextAlign = ImVec2(0.0f, 0.0f);
	
	style.Colors[ImGuiCol_Text] = ImVec4(1.0f, 1.0f, 1.0f, 1.0f);
	style.Colors[ImGuiCol_TextDisabled] = ImVec4(0.5019608f, 0.5019608f, 0.5019608f, 1.0f);
	style.Colors[ImGuiCol_Border] = ImVec4(0.101960786f, 0.101960786f, 0.101960786f, 1.0f);
	style.Colors[ImGuiCol_BorderShadow] = ImVec4(0.0f, 0.0f, 0.0f, 0.0f);
	style.Colors[ImGuiCol_FrameBg] = ImVec4(0.05882353f, 0.05882353f, 0.05882353f, 1.0f);
	style.Colors[ImGuiCol_FrameBgHovered] = ImVec4(0.09019608f, 0.09019608f, 0.09019608f, 1.0f);
	style.Colors[ImGuiCol_FrameBgActive] = ImVec4(0.05882353f, 0.05882353f, 0.05882353f, 1.0f);
	style.Colors[ImGuiCol_TitleBg] = ImVec4(0.08235294f, 0.08235294f, 0.08235294f, 1.0f);
	style.Colors[ImGuiCol_TitleBgActive] = ImVec4(0.08235294f, 0.08235294f, 0.08235294f, 1.0f);
	style.Colors[ImGuiCol_TitleBgCollapsed] = ImVec4(0.15294118f, 0.15294118f, 0.15294118f, 1.0f);
	style.Colors[ImGuiCol_MenuBarBg] = ImVec4(0.0f, 0.0f, 0.0f, 0.0f);
	style.Colors[ImGuiCol_ScrollbarBg] = ImVec4(0.019607844f, 0.019607844f, 0.019607844f, 0.53f);
	style.Colors[ImGuiCol_ScrollbarGrab] = ImVec4(0.30980393f, 0.30980393f, 0.30980393f, 1.0f);
	style.Colors[ImGuiCol_ScrollbarGrabHovered] = ImVec4(0.4117647f, 0.4117647f, 0.4117647f, 1.0f);
	style.Colors[ImGuiCol_ScrollbarGrabActive] = ImVec4(0.50980395f, 0.50980395f, 0.50980395f, 1.0f);
	style.Colors[ImGuiCol_CheckMark] = ImVec4(0.7529412f, 0.7529412f, 0.7529412f, 1.0f);
	style.Colors[ImGuiCol_SliderGrab] = ImVec4(0.50980395f, 0.50980395f, 0.50980395f, 0.7f);
	style.Colors[ImGuiCol_SliderGrabActive] = ImVec4(0.65882355f, 0.65882355f, 0.65882355f, 1.0f);
	style.Colors[ImGuiCol_Button] = ImVec4(0.21960784f, 0.21960784f, 0.21960784f, 0.784f);
	style.Colors[ImGuiCol_ButtonHovered] = ImVec4(0.27450982f, 0.27450982f, 0.27450982f, 1.0f);
	style.Colors[ImGuiCol_ButtonActive] = ImVec4(0.21960784f, 0.21960784f, 0.21960784f, 0.588f);
	style.Colors[ImGuiCol_Header] = ImVec4(0.18431373f, 0.18431373f, 0.18431373f, 1.0f);
	style.Colors[ImGuiCol_HeaderHovered] = ImVec4(0.18431373f, 0.18431373f, 0.18431373f, 1.0f);
	style.Colors[ImGuiCol_HeaderActive] = ImVec4(0.18431373f, 0.18431373f, 0.18431373f, 1.0f);
	style.Colors[ImGuiCol_Separator] = ImVec4(0.101960786f, 0.101960786f, 0.101960786f, 1.0f);
	style.Colors[ImGuiCol_SeparatorHovered] = ImVec4(0.15294118f, 0.7254902f, 0.9490196f, 0.588f);
	style.Colors[ImGuiCol_SeparatorActive] = ImVec4(0.15294118f, 0.7254902f, 0.9490196f, 1.0f);
	style.Colors[ImGuiCol_ResizeGrip] = ImVec4(0.9098039f, 0.9098039f, 0.9098039f, 0.25f);
	style.Colors[ImGuiCol_ResizeGripHovered] = ImVec4(0.8117647f, 0.8117647f, 0.8117647f, 0.67f);
	style.Colors[ImGuiCol_ResizeGripActive] = ImVec4(0.45882353f, 0.45882353f, 0.45882353f, 0.95f);
	style.Colors[ImGuiCol_Tab] = ImVec4(0.08235294f, 0.08235294f, 0.08235294f, 1.0f);
	style.Colors[ImGuiCol_TabHovered] = ImVec4(1.0f, 0.88235295f, 0.5294118f, 0.118f);
	style.Colors[ImGuiCol_TabActive] = ImVec4(1.0f, 0.88235295f, 0.5294118f, 0.235f);
	style.Colors[ImGuiCol_TabUnfocused] = ImVec4(0.08235294f, 0.08235294f, 0.08235294f, 1.0f);
	style.Colors[ImGuiCol_TabUnfocusedActive] = ImVec4(1.0f, 0.88235295f, 0.5294118f, 0.118f);
	style.Colors[ImGuiCol_PlotLines] = ImVec4(0.6117647f, 0.6117647f, 0.6117647f, 1.0f);
	style.Colors[ImGuiCol_PlotLinesHovered] = ImVec4(1.0f, 0.43137255f, 0.34901962f, 1.0f);
	style.Colors[ImGuiCol_PlotHistogram] = ImVec4(0.9019608f, 0.7019608f, 0.0f, 1.0f);
	style.Colors[ImGuiCol_PlotHistogramHovered] = ImVec4(1.0f, 0.6f, 0.0f, 1.0f);
	style.Colors[ImGuiCol_TableHeaderBg] = ImVec4(0.18431373f, 0.18431373f, 0.18431373f, 1.0f);
	style.Colors[ImGuiCol_TableBorderStrong] = ImVec4(0.30980393f, 0.30980393f, 0.34901962f, 1.0f);
	style.Colors[ImGuiCol_TableBorderLight] = ImVec4(0.101960786f, 0.101960786f, 0.101960786f, 1.0f);
	style.Colors[ImGuiCol_TableRowBg] = ImVec4(0.0f, 0.0f, 0.0f, 0.0f);
	style.Colors[ImGuiCol_TableRowBgAlt] = ImVec4(1.0f, 1.0f, 1.0f, 0.06f);
	style.Colors[ImGuiCol_TextSelectedBg] = ImVec4(0.15294118f, 0.7254902f, 0.9490196f, 0.35f);
	style.Colors[ImGuiCol_DragDropTarget] = ImVec4(1.0f, 1.0f, 0.0f, 0.9f);
	style.Colors[ImGuiCol_NavHighlight] = ImVec4(0.15294118f, 0.7254902f, 0.9490196f, 0.8f);
	style.Colors[ImGuiCol_NavWindowingHighlight] = ImVec4(1.0f, 1.0f, 1.0f, 0.7f);
	style.Colors[ImGuiCol_NavWindowingDimBg] = ImVec4(0.8f, 0.8f, 0.8f, 0.2f);
	style.Colors[ImGuiCol_ModalWindowDimBg] = ImVec4(0.8f, 0.8f, 0.8f, 0.35f);
}

bool open = true;
constexpr ImVec2 size = {420, 200};

void TextCentered(const char* text) {
    const auto windowWidth = ImGui::GetWindowSize().x;
    const auto textWidth   = ImGui::CalcTextSize(text).x;
    
    if (textWidth < windowWidth) {
        const char* ptr = text;
        while (*ptr) {
            const char* line_end = ptr;
            while (*line_end && *line_end != '\n') { line_end++; }

            std::string line(ptr, line_end);
            const auto textWidth = ImGui::CalcTextSize(line.c_str()).x;
            ImGui::SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui::Text("%s", line.c_str());

            if (*line_end == '\n') { ptr = line_end + 1; }
            else { break; }
        }
    } else {
        ImGui::TextWrapped("%s", text);
    }
}

int main(int argc, char** argv) {
    const char* message = (argc > 1) ? argv[1] : "No message provided";
    UI::Init(10, 10, "Notification");
    glfwSetWindowPos(WindowManager::window, 0, 0);
    
    GLFWmonitor* monitor = glfwGetPrimaryMonitor();
    const GLFWvidmode* mode = glfwGetVideoMode(monitor);
    SetupImGuiStyle();
    
    while (!UI::WindowShouldClose() && open) {
        if (UI::BeginFrame()) {
            
            ImGui::SetNextWindowPos(ImVec2(mode->width / 2 - size.x / 2, mode->height / 2 - size.y / 2), ImGuiCond_Appearing);
            ImGui::SetNextWindowSize(size);
            
            ImGui::Begin("Notification", &open, ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoMove | 
                ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags_NoTitleBar);
            ImGui::Dummy(ImVec2(0, 5));
            ImGui::Dummy(ImVec2(5, 0));
            ImGui::SameLine();
            
            ImGui::BeginChild("child");
            
            TextCentered(message);
            ImGui::Dummy(ImVec2(0, ImGui::GetContentRegionAvail().y - 55));

            if (argc == 1) {
                if (ImGui::Button("Close", ImVec2(ImGui::GetContentRegionAvail().x - 10, 40))) {
                    std::printf("Close");
                    open = false;
                }
            }
            else
            {
                for (int i = 2; i < argc; i++) {
                    if (ImGui::Button(argv[i], ImVec2((ImGui::GetContentRegionAvail().x / (argc - i)) - 10, 40))) {
                        std::printf("%s", argv[i]);
                        open = false;
                    }
                    ImGui::SameLine();
                }
            }
            ImGui::EndChild();
            ImGui::End();
            
            UI::EndFrame();
        }
    }

    UI::Destroy();

    return 0;
}
