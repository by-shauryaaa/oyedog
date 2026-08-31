using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using PixelDogReminders.Models;
using WpfButton = System.Windows.Controls.Button;

namespace PixelDogReminders.Views.Dialogs;

public partial class SlotPickerDialog : Window
{
    private readonly int _durationMinutes;
    private readonly List<Subject> _allSubjects;
    private readonly Guid? _currentSubjectId;
    private readonly List<Slot> _pendingSlots;
    private bool _isInitializing = true;

    public Slot? ResultSlot { get; private set; }

    public SlotPickerDialog(
        int durationMinutes,
        List<Subject> allSubjects,
        Guid? currentSubjectId = null,
        List<Slot>? pendingSlots = null,
        DayOfWeek? initialDay = null,
        TimeSpan? initialTime = null)
    {
        _durationMinutes = durationMinutes;
        _allSubjects = allSubjects ?? new List<Subject>();
        _currentSubjectId = currentSubjectId;
        _pendingSlots = pendingSlots ?? new List<Slot>();

        InitializeComponent();

        PopulateDays(initialDay);

        var start = initialTime ?? new TimeSpan(9, 0, 0);
        TxtStartTime.Text = DateTime.Today.Add(start).ToString("hh:mm tt");

        _isInitializing = false;
        UpdateCalculatedEndTime();
    }

    private void PopulateDays(DayOfWeek? initialDay)
    {
        var days = new[]
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday
        };

        foreach (var day in days)
        {
            CmbDayOfWeek.Items.Add(day);
        }

        CmbDayOfWeek.SelectedItem = initialDay ?? DayOfWeek.Monday;
    }

    private void CmbDayOfWeek_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        PnlConflict.Visibility = Visibility.Collapsed;
        UpdateCalculatedEndTime();
    }

    private void TxtStartTime_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing) return;
        PnlConflict.Visibility = Visibility.Collapsed;
        UpdateCalculatedEndTime();
    }

    private void BtnPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton btn && btn.Tag is string timeStr)
        {
            if (TryParseTime(timeStr, out var parsed))
            {
                TxtStartTime.Text = DateTime.Today.Add(parsed).ToString("hh:mm tt");
            }
            else
            {
                TxtStartTime.Text = timeStr;
            }
        }
    }

    private void UpdateCalculatedEndTime()
    {
        if (TryParseTime(TxtStartTime.Text, out var startTime))
        {
            var end = startTime.Add(TimeSpan.FromMinutes(_durationMinutes));
            TxtCalculatedEndTime.Text = DateTime.Today.Add(end).ToString("hh:mm tt (HH:mm)");
        }
        else
        {
            TxtCalculatedEndTime.Text = "--:--";
        }
    }

    private bool TryParseTime(string input, out TimeSpan time)
    {
        time = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(input)) return false;

        input = input.Trim();

        // 1. Try standard DateTime parsing (handles "10:50", "9:40", "10:50 AM", "2:30 PM", "09:40", etc.)
        if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt) ||
            DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            time = dt.TimeOfDay;
            return true;
        }

        // 2. Try TimeSpan parse
        if (TimeSpan.TryParse(input, out var ts))
        {
            time = ts;
            return true;
        }

        // 3. Handle patterns like "9am", "9pm", "10.50", "9 40"
        var clean = input.Replace(".", ":").Replace(" ", ":").Trim();
        if (DateTime.TryParse(clean, out var dtClean))
        {
            time = dtClean.TimeOfDay;
            return true;
        }

        return false;
    }

    private void BtnAddSlot_Click(object sender, RoutedEventArgs e)
    {
        if (CmbDayOfWeek.SelectedItem is not DayOfWeek selectedDay)
        {
            return;
        }

        if (!TryParseTime(TxtStartTime.Text, out var start))
        {
            TxtConflictWarning.Text = "Please enter a valid start time (e.g. 10:50 AM, 9:40, 14:30).";
            PnlConflict.Visibility = Visibility.Visible;
            TxtStartTime.Focus();
            return;
        }

        var end = start.Add(TimeSpan.FromMinutes(_durationMinutes));

        // Overlap validation across all existing subjects and pending slots for the same day
        // 1. Check existing saved subjects (excluding current subject if editing)
        foreach (var subject in _allSubjects)
        {
            if (_currentSubjectId.HasValue && subject.Id == _currentSubjectId.Value) continue;
            if (subject.Slots == null) continue;

            foreach (var slot in subject.Slots)
            {
                if (slot.DayOfWeek != selectedDay) continue;
                var slotEnd = slot.GetEndTime(subject.DurationMinutes);

                // Overlap condition: start < slotEnd && slot.StartTime < end
                if (start < slotEnd && slot.StartTime < end)
                {
                    ShowConflict(subject.Name, slot.StartTime, slotEnd);
                    return;
                }
            }
        }

        // 2. Check pending slots in current edit session
        foreach (var pendingSlot in _pendingSlots)
        {
            if (pendingSlot.DayOfWeek != selectedDay) continue;
            var pendingEnd = pendingSlot.GetEndTime(_durationMinutes);

            if (start < pendingEnd && pendingSlot.StartTime < end)
            {
                ShowConflict("this subject's other slot", pendingSlot.StartTime, pendingEnd);
                return;
            }
        }

        // Success - create slot
        ResultSlot = new Slot
        {
            SubjectId = _currentSubjectId ?? Guid.Empty,
            DayOfWeek = selectedDay,
            StartTime = start
        };

        DialogResult = true;
        Close();
    }

    private void ShowConflict(string subjectName, TimeSpan conflictStart, TimeSpan conflictEnd)
    {
        var startStr = DateTime.Today.Add(conflictStart).ToString("hh:mm tt");
        var endStr = DateTime.Today.Add(conflictEnd).ToString("hh:mm tt");

        TxtConflictWarning.Text = $"Conflicts with {subjectName} ({startStr} - {endStr})";
        PnlConflict.Visibility = Visibility.Visible;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
