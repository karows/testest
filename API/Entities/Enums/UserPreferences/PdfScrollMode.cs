﻿using System.ComponentModel;

namespace API.Entities.Enums.UserPreferences;

/// <summary>
/// Enum values match PdfViewer's enums
/// </summary>
public enum PdfScrollMode
{
    [Description("Vertical")]
    Vertical = 0,
    [Description("Horizontal")]
    Horizontal = 1,
    // [Description("Wrapped")]
    // Wrapped = 2,
    /// <summary>
    /// Single page view (not supported, doesn't make sense for Kavita)
    /// </summary>
    // [Description("Page")]
    // Page = 3
}
