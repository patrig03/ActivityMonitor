cmake_minimum_required(VERSION 3.29)
project(UI_core)

set(CMAKE_CXX_STANDARD 20)

find_package(PkgConfig REQUIRED)
pkg_check_modules(GLFW REQUIRED glfw3)

find_package(Vulkan REQUIRED)

add_library(UI_core STATIC
        UI-core.cpp UI-core.h
        src/WindowManager.cpp       src/WindowManager.h
        src/VulkanInstance.cpp      src/VulkanInstance.h
        src/DeviceManager.cpp       src/DeviceManager.h
        src/SwapchainManager.cpp    src/SwapchainManager.h
        src/PipelineManager.cpp     src/PipelineManager.h
        src/DescriptorManager.cpp   src/DescriptorManager.h
        src/CommandManager.cpp      src/CommandManager.h
        src/InitFlags.h
        
        imgui/imgui_impl_glfw.cpp    imgui/imgui_impl_glfw.h
        imgui/imgui_impl_vulkan.cpp  imgui/imgui_impl_vulkan.h
        imgui/imgui_widgets.cpp
        imgui/imgui_tables.cpp
        imgui/imgui_draw.cpp
        imgui/imgui.cpp              imgui/imgui.h
        imgui/imconfig.h
        imgui/imgui_internal.h
        imgui/imstb_rectpack.h
        imgui/imstb_textedit.h
        imgui/imstb_truetype.h

        #        interface/Data.h
#        interface/IEngine.h
#        utilities/utils.h
)

target_include_directories(UI_core
        PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_CURRENT_SOURCE_DIR}/imgui/
        ${CMAKE_CURRENT_SOURCE_DIR}/src/
        ${CMAKE_CURRENT_SOURCE_DIR}/interface/
        ${GLFW_INCLUDE_DIRS}
)

target_link_libraries(UI_core
        PRIVATE
        ${GLFW_LIBRARIES}
        Vulkan::Vulkan
)

add_compile_definitions(SHADER_DIR="${CMAKE_CURRENT_SOURCE_DIR}/shaders/")
add_compile_definitions(FONTS_DIR="${CMAKE_CURRENT_SOURCE_DIR}/assets/fonts/")

