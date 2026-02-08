using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace RunixLauncher.Helpers;

public static class CommonColours
{
    public static ImmutableSolidColorBrush SettingsList_selectedBrush => m_SettingsList_selectedBrush ??= new ImmutableSolidColorBrush(Color.FromRgb(0, 0, 0));
    private static ImmutableSolidColorBrush? m_SettingsList_selectedBrush;

    public static ImmutableSolidColorBrush SettingsList_unselectedBrush => m_SettingsList_unselectedBrush ??= new ImmutableSolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    private static ImmutableSolidColorBrush? m_SettingsList_unselectedBrush;
}
