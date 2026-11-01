#include "security_native.h"

/* Integer percent, rounded up and capped at 100; -1 for negative inputs. */
int32_t SECURITY_CALL security_load_percent(int32_t active_guests, int32_t capacity)
{
    if (active_guests < 0 || capacity < 0) return -1;
    if (active_guests == 0) return 0;
    if (capacity == 0 || active_guests >= capacity) return 100;
    return (int32_t)(((int64_t)active_guests * 100 + capacity - 1) / capacity);
}
