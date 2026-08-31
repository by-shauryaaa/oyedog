using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PixelDogReminders.Models;
using WpfControl = System.Windows.Controls.UserControl;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfCursors = System.Windows.Input.Cursors;
using WpfColor = System.Windows.Media.Color;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfVerticalAlignment = System.Windows.VerticalAlignment;

namespace PixelDogReminders.Views.Tabs;

public partial class ScheduleListView : WpfControl
{
    private List<Subject> _subjects = new();

    public event EventHandler<Subject>? SubjectSelected;
    public event EventHandler? AddSubjectRequested;

    public ScheduleListView()
    {
        InitializeComponent();
    }

    public void RenderSchedule(List<Subject> subjects)
    {
        _subjects = subjects ?? new List<Subject>();
        PnlScheduleGroups.Children.Clear();

        var totalSlots = _subjects.Sum(s => s.Slots?.Count ?? 0);
        if (totalSlots == 0)
        {
            PnlEmptyState.Visibility = Visibility.Visible;
            return;
        }

        PnlEmptyState.Visibility = Visibility.Collapsed;

        var today = DateTime.Today;
        bool anyRendered = false;

        // Render the next 7 days in chronological sequence
        for (int dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date = today.AddDays(dayOffset);
            var dayOfWeek = date.DayOfWeek;

            // Collect all slots for this day across all subjects
            var daySlots = new List<(Subject Subject, Slot Slot)>();
            foreach (var subj in _subjects)
            {
                if (subj.Slots == null) continue;
                foreach (var slot in subj.Slots)
                {
                    if (slot.DayOfWeek == dayOfWeek)
                    {
                        daySlots.Add((subj, slot));
                    }
                }
            }

            if (daySlots.Count == 0) continue;

            // Sort by start time
            daySlots.Sort((a, b) => a.Slot.StartTime.CompareTo(b.Slot.StartTime));
            anyRendered = true;

            // Day Header
            string headerText = dayOffset switch
            {
                0 => $"📅 TODAY — {date:ddd, dd MMM}",
                1 => $"📅 TOMORROW — {date:ddd, dd MMM}",
                _ => $"📅 {date:dddd, dd MMM}".ToUpper()
            };

            var dayHeader = new Border
            {
                Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFF8E7")!,
                BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#8D6E57")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 10, 0, 6)
            };

            var headerTitle = new TextBlock
            {
                Text = headerText,
                FontFamily = (WpfFontFamily)FindResource("PixelFont"),
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#8D6E57")!
            };
            dayHeader.Child = headerTitle;
            PnlScheduleGroups.Children.Add(dayHeader);

            // Cards for each slot on this day
            foreach (var (subj, slot) in daySlots)
            {
                var card = CreateClassCard(subj, slot);
                PnlScheduleGroups.Children.Add(card);
            }
        }

        if (!anyRendered)
        {
            PnlEmptyState.Visibility = Visibility.Visible;
        }
    }

    private Border CreateClassCard(Subject subject, Slot slot)
    {
        var end = slot.GetEndTime(subject.DurationMinutes);
        var startStr = DateTime.Today.Add(slot.StartTime).ToString("hh:mm tt");
        var endStr = DateTime.Today.Add(end).ToString("hh:mm tt");

        var card = new Border
        {
            Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF")!,
            BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#2D1E14")!,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = WpfCursors.Hand
        };

        // Subtle drop shadow
        card.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = WpfColor.FromRgb(45, 30, 20),
            Direction = 315,
            ShadowDepth = 2,
            BlurRadius = 0,
            Opacity = 0.5
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) }); // Color strip
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) }); // Spacing
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Info
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Time & room

        // Color Strip on left
        var colorStrip = new Border
        {
            CornerRadius = new CornerRadius(2),
            Width = 6
        };
        try
        {
            colorStrip.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(subject.Color)!;
        }
        catch
        {
            colorStrip.Background = new SolidColorBrush(WpfColor.FromRgb(100, 181, 246));
        }
        Grid.SetColumn(colorStrip, 0);
        grid.Children.Add(colorStrip);

        // Subject Info (Column 2)
        var infoStack = new StackPanel { VerticalAlignment = WpfVerticalAlignment.Center };
        var nameBlock = new TextBlock
        {
            Text = subject.Name,
            FontFamily = (WpfFontFamily)FindResource("PixelFont"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#2D1E14")!,
            Margin = new Thickness(0, 0, 0, 4)
        };
        infoStack.Children.Add(nameBlock);

        var durationBadge = new TextBlock
        {
            Text = $"⏱ {subject.DurationMinutes} min class",
            FontSize = 11,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#665544")!
        };
        infoStack.Children.Add(durationBadge);
        Grid.SetColumn(infoStack, 2);
        grid.Children.Add(infoStack);

        // Time & Room (Column 3)
        var rightStack = new StackPanel { HorizontalAlignment = WpfHorizontalAlignment.Right, VerticalAlignment = WpfVerticalAlignment.Center };
        var timeBlock = new TextBlock
        {
            Text = $"{startStr} – {endStr}",
            FontFamily = (WpfFontFamily)FindResource("PixelFont"),
            FontSize = 9,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#2D1E14")!,
            HorizontalAlignment = WpfHorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 4)
        };
        rightStack.Children.Add(timeBlock);

        if (!string.IsNullOrEmpty(subject.Room))
        {
            var roomBlock = new TextBlock
            {
                Text = $"📍 {subject.Room}",
                FontSize = 11,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#8D6E57")!,
                HorizontalAlignment = WpfHorizontalAlignment.Right
            };
            rightStack.Children.Add(roomBlock);
        }
        Grid.SetColumn(rightStack, 3);
        grid.Children.Add(rightStack);

        card.Child = grid;

        var targetSubject = subject;
        card.MouseLeftButtonUp += (s, e) =>
        {
            SubjectSelected?.Invoke(this, targetSubject);
        };

        return card;
    }

    private void BtnAddFirstSubject_Click(object sender, RoutedEventArgs e)
    {
        AddSubjectRequested?.Invoke(this, EventArgs.Empty);
    }
}
