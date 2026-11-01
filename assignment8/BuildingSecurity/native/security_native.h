#ifndef SECURITY_NATIVE_H
#define SECURITY_NATIVE_H
#include <stdint.h>
#ifdef _WIN32
#define SECURITY_API __declspec(dllexport)
#define SECURITY_CALL __cdecl
#else
#define SECURITY_API
#define SECURITY_CALL
#endif
SECURITY_API int32_t SECURITY_CALL security_load_percent(int32_t active_guests, int32_t capacity);
#endif
