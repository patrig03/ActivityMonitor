#pragma once

struct hard_lock_data
{
    const char* message = nullptr;
    int seconds = 0;

    bool open = false;
};


inline void RenderHardLock(hard_lock_data& data)
{
    
}
