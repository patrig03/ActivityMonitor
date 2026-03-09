cmake_minimum_required(VERSION 3.29)
project(Notifier)

set(CMAKE_CXX_STANDARD 20)

find_package(X11 REQUIRED)

add_subdirectory(../ ${CMAKE_BINARY_DIR}/UI-core_build)

add_executable(Notifier main.cpp
        Style.h
        ReminderNotification.h
        SoftLock.h
        HardLock.h)
target_link_libraries(Notifier PRIVATE 
        UI_core
        X11::X11)
