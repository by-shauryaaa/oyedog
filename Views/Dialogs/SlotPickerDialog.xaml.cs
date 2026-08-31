using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using PixelDogReminders.Models;

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
        PopulateTimes(initialTime);

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

    private void PopulateTimes(TimeSpan? initialTime)
    {
        // 6:00 AM to 10:00 PM in 15-min increments
        var targetTime = initialTime ?? new TimeSpan(9, 0, 0);
        int selectedIndex = 0;

        for (int h = 6; h <= 22; h++)
        {
            for (int m = 0; m < 60; m += 15)
            {
                if (h == 22 && m > 0) break;
                var ts = new TimeSpan(h, m, 0);
                var display = DateTime.Today.Add(ts).ToString("hh:mm tt (HH:mm)");
                int idx = CmbStartTime.Items.Add(new TimeItem(ts, display));

                if (ts.Hours == targetTime.Hours && Math.Abs(ts.Minutes - targetTime.Minutes) < 15)
                {
                    selectedIndex = idx;
                }
            }
        }

        CmbStartTime.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
    }

    private void CmbInputs_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        PnlConflict.Visibility = Visibility.Collapsed;
        UpdateCalculatedEndTime();
    }

    private void UpdateCalculatedEndTime()
    {
        if (CmbStartTime.SelectedItem is TimeItem item)
        {
            var end = item.Time.Add(TimeSpan.FromMinutes(_durationMinutes));
            TxtCalculatedEndTime.Text = DateTime.Today.Add(end).ToString("hh:mm tt (HH:mm)");
        }
    }

    private void BtnAddSlot_Click(object sender, RoutedEventArgs e)
    {
        if (CmbDayOfWeek.SelectedItem is not DayOfWeek selectedDay ||
            CmbStartTime.SelectedItem is not TimeItem selectedTimeItem)
        {
            return;
        }

        var start = selectedTimeItem.Time;
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

    private record TimeItem(TimeSpan Time, string Display)
    {
        public override string ToString() => Display;
    }
}
