﻿using System;

namespace Kavita.Common.EnvironmentInfo
{
    public interface IRuntimeInfo
    {
        DateTime StartTime { get; }
        bool IsUserInteractive { get; }
        bool IsAdmin { get; }
        bool IsWindowsService { get; }
        bool IsWindowsTray { get; }
        bool IsExiting { get; set; }
        bool IsTray { get; }
        bool RestartPending { get; set; }
        string ExecutingApplication { get; }
    }
}