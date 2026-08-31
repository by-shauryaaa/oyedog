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

public partial class WeekGridView : WpfControl
{
    private const int StartHour = 8;
    private const int EndHour = 18; // 6 PM
    private const double RowHeight = 54.0;
    private const double HeaderHeight = 34.0;

    private List<Subject> _subjects = new();

    public event EventHandler<Subject>? SubjectSelected;

    public WeekGridView()
    {
        InitializeComponent();
    }

    public void RenderSchedule(List<Subject> subjects)
    {
        _subjects = subjects ?? new List<Subject>();
        BuildGridStructure();
        PopulateSubjectBlocks();
    }

    private void BuildGridStructure()
    {
        GridSchedule.Children.Clear();
        GridSchedule.ColumnDefinitions.Clear();
        GridSchedule.RowDefinitions.Clear();

        // 1. Columns: Time (0), Mon (1), Tue (2), Wed (3), Thu (4), Fri (5)
        GridSchedule.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        for (int c = 1; c <= 5; c++)
        {
            GridSchedule.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 110 });
        }

        // 2. Rows: Header (0), 8 AM - 5 PM (1 to 10)
        GridSchedule.RowDefinitions.Add(new RowDefinition { Height = new GridLength(HeaderHeight) });
        for (int h = StartHour; h < EndHour; h++)
        {
            GridSchedule.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowHeight) });
        }

        // 3. Header Cells
        var days = new[] { "MON", "TUE", "WED", "THU", "FRI" };
        for (int d = 0; d < 5; d++)
        {
            var headerBorder = new Border
            {
                Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFF8E7")!,
                BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#2D1E14")!,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(1),
                CornerRadius = new CornerRadius(3)
            };

            var headerText = new TextBlock
            {
                Text = days[d],
                FontFamily = (WpfFontFamily)FindResource("PixelFont"),
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#2D1E14")!,
                HorizontalAlignment = WpfHorizontalAlignment.Center,
                VerticalAlignment = WpfVerticalAlignment.Center
            };

            headerBorder.Child = headerText;
            Grid.SetRow(headerBorder, 0);
            Grid.SetColumn(headerBorder, d + 1);
            GridSchedule.Children.Add(headerBorder);
        }

        // 4. Time Labels & Grid Cell Backgrounds
        for (int h = StartHour; h < EndHour; h++)
        {
            int row = h - StartHour + 1;

            // Time Label
            var timeBorder = new Border
            {
                Padding = new Thickness(2, 4, 4, 0)
            };
            var timeText = new TextBlock
            {
                Text = DateTime.Today.AddHours(h).ToString("h tt"),
                FontFamily = (WpfFontFamily)FindResource("PixelFont"),
                FontSize = 7,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#8D6E57")!,
                HorizontalAlignment = WpfHorizontalAlignment.Right,
                VerticalAlignment = WpfVerticalAlignment.Top
            };
            timeBorder.Child = timeText;
            Grid.SetRow(timeBorder, row);
            Grid.SetColumn(timeBorder, 0);
            GridSchedule.Children.Add(timeBorder);

            // Day Slots background grid lines
            for (int d = 1; d <= 5; d++)
            {
                var cellBg = new Border
                {
                    BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#E8DFD5")!,
                    BorderThickness = new Thickness(0.5),
                    Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF")!,
                    Margin = new Thickness(1)
                };
                Grid.SetRow(cellBg, row);
                Grid.SetColumn(cellBg, d);
                GridSchedule.Children.Add(cellBg);
            }
        }
    }

    private void PopulateSubjectBlocks()
    {
        foreach (var subject in _subjects)
        {
            if (subject.Slots == null) continue;

            foreach (var slot in subject.Slots)
            {
                // Day of Week to Column mapping (Monday = 1 ... Friday = 5)
                int col = (int)slot.DayOfWeek;
                if (col < 1 || col > 5) continue; // Mon-Fri only in Week Grid

                int startHour = slot.StartTime.Hours;
                if (startHour < StartHour || startHour >= EndHour) continue; // 8 AM to 6 PM only

                int row = startHour - StartHour + 1;
                double topOffset = (slot.StartTime.Minutes / 60.0) * RowHeight;
                double blockHeight = Math.Max(28.0, (subject.DurationMinutes / 60.0) * RowHeight - 2);

                int rowSpan = Math.Max(1, (int)Math.Ceiling((slot.StartTime.Minutes + subject.DurationMinutes) / 60.0));

                var blockBorder = new Border
                {
                    BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#2D1E14")!,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(2, topOffset, 2, 2),
                    Height = blockHeight,
                    VerticalAlignment = WpfVerticalAlignment.Top,
                    Cursor = WpfCursors.Hand,
                    ToolTip = $"{subject.Name}\n{slot.DayOfWeek} {DateTime.Today.Add(slot.StartTime):hh:mm tt} - {DateTime.Today.Add(slot.GetEndTime(subject.DurationMinutes)):hh:mm tt}\nRoom: {(string.IsNullOrEmpty(subject.Room) ? "None" : subject.Room)}"
                };

                // Parse subject color
                try
                {
                    var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(subject.Color)!;
                    blockBorder.Background = brush;
                }
                catch
                {
                    blockBorder.Background = new SolidColorBrush(WpfColor.FromRgb(100, 181, 246));
                }

                // Drop shadow
                blockBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = WpfColor.FromRgb(45, 30, 20),
                    Direction = 315,
                    ShadowDepth = 2,
                    BlurRadius = 0,
                    Opacity = 0.7
                };

                // Inner content
                var stack = new StackPanel
                {
                    Margin = new Thickness(4, 2, 4, 2),
                    VerticalAlignment = WpfVerticalAlignment.Center
                };

                var titleBlock = new TextBlock
                {
                    Text = subject.Name,
                    FontFamily = (WpfFontFamily)FindResource("PixelFont"),
                    FontSize = 8,
                    FontWeight = FontWeights.Bold,
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#2D1E14")!,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                stack.Children.Add(titleBlock);

                if (!string.IsNullOrEmpty(subject.Room) && blockHeight >= 40)
                {
                    var roomBlock = new TextBlock
                    {
                        Text = $"📍 {subject.Room}",
                        FontSize = 9,
                        Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#443322")!,
                        Margin = new Thickness(0, 2, 0, 0),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    stack.Children.Add(roomBlock);
                }

                blockBorder.Child = stack;

                var targetSubject = subject;
                blockBorder.MouseLeftButtonUp += (s, e) =>
                {
                    SubjectSelected?.Invoke(this, targetSubject);
                };

                Grid.SetRow(blockBorder, row);
                Grid.SetRowSpan(blockBorder, rowSpan);
                Grid.SetColumn(blockBorder, col);
                GridSchedule.Children.Add(blockBorder);
            }
        }
    }
}
