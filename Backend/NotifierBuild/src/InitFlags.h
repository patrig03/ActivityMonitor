
#pragma once

#include <cstdint>
#include <type_traits>

enum class init_flags : uint32_t {
    None            = 0,
    NotResizable    = 1 << 0,
    Maximized       = 1 << 1,
    NoDecoration    = 1 << 2,
    Floating        = 1 << 3,
};

inline init_flags operator|(const init_flags &a, const init_flags &b) {
    return static_cast<init_flags>(
        static_cast<std::underlying_type_t<init_flags>>(a) |
        static_cast<std::underlying_type_t<init_flags>>(b));
}

inline init_flags& operator|=(init_flags &a, const init_flags &b) {
    a = a | b;
    return a;
}

inline bool operator&(const init_flags &a, const init_flags &b) {
    using T = std::underlying_type_t<init_flags>;
    return (static_cast<T>(a) & static_cast<T>(b)) != 0;
}
