cmake_minimum_required(VERSION 3.29)
project(Notifier)

set(CMAKE_CXX_STANDARD 20)

add_subdirectory(../ ${CMAKE_BINARY_DIR}/UI-core_build)
add_executable(Notifier main.cpp)
target_link_libraries(Notifier PRIVATE UI_core)
