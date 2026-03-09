
#include "HardLock.h"
#include "ReminderNotification.h"
#include "SoftLock.h"
#include "Style.h"
#include "WindowManager.h"
#include "../UI-core.h" 



// -n [message] [buttons]
// -s [message] [window_id] [lock]
// -h [message] [window_id] [seconds]
int main(int argc, char** argv) {
    if (argc < 2) {
        std::printf("Usage\n");
        std::printf("Notification: -n [message] [buttons]\n");
        std::printf("Soft lock:    -s [message] [window_id] [lock]\n");
        std::printf("Hard Lock:    -h [message] [window_id] [seconds]\n");
        return 1;
    }
    
    UI::Init(10, 10, "Intervention");
    glfwSetWindowPos(WindowManager::window, 0, 0);
    
    GLFWmonitor* monitor = glfwGetPrimaryMonitor();
    const GLFWvidmode* mode = glfwGetVideoMode(monitor);
    SetupImGuiStyle();

    if (argc > 2 && strcmp(argv[1], "-n") == 0) {
        notification_data data = {
            .message = argv[2],
            .button_count = argc - 3,
            .button_names = argv + 3,
            .monitor_size = ImVec2(static_cast<float>(mode->width), static_cast<float>(mode->height))
        };
        RenderNotification(data);
    }
    else if (argc > 2 && strcmp(argv[1], "-s") == 0) {
        soft_lock_data data = {
            .message = argv[2],
            .password = argv[4],
            .window_id = strtoul(argv[3], nullptr, 0)
        };
        RenderSoftLock(data);
    }
    else if (argc > 2 && strcmp(argv[1], "-h") == 0) {
        hard_lock_data data = {
            .message = argv[2],
            .seconds = atoi(argv[4]),
            .window_id = strtoul(argv[3], nullptr, 0)
        };
        RenderHardLock(data);
    }

    return 0;
}
