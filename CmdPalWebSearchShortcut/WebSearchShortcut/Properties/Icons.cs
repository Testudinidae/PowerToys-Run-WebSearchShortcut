// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions.Toolkit;

namespace WebSearchShortcut.Properties;

/// <summary>
/// Provides commonly used icons for the WebSearchShortcut application
/// </summary>
internal static class Icons
{
    /// <summary>
    /// Extension logo icon
    /// </summary>
    public static IconInfo Logo => IconHelpers.FromRelativePath("Assets\\Search.png");

    /// <summary>
    /// "Add Shortcut" icon
    /// </summary>
    public static IconInfo AddShortcut => IconHelpers.FromRelativePath("Assets\\SearchAdd.png");

    /// <summary>
    /// "Edit Shortcut" icon
    /// </summary>
    public static IconInfo EditShortcut => new("\uE70F");

    /// <summary>
    /// Default fallback icon for links
    /// </summary>
    public static IconInfo Link => new("🔗");

    /// <summary>
    /// Edit icon (pencil)
    /// </summary>
    public static IconInfo Edit => new("\uE70F");

    /// <summary>
    /// Delete icon (trash can)
    /// </summary>
    public static IconInfo Delete => new("\uE74D");

    /// <summary>
    /// Homepage icon
    /// </summary>
    public static IconInfo Home => new("\uE80F");

    /// <summary>
    /// Search icon
    /// </summary>
    public static IconInfo Search => new("\uE721");

    /// <summary> 
    /// History icon 
    /// </summary>
    public static IconInfo History => new("\uE81C");
}
