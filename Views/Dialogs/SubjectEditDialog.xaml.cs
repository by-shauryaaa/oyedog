using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PixelDogReminders.Models;
using WpfMessageBox = System.Windows.MessageBox;
using WpfColor = System.Windows.Media.Color;
using WpfButton = System.Windows.Controls.Button;

namespace PixelDogReminders.Views.Dialogs;

public partial class SubjectEditDialog : Window
{
    private readonly Subject? _existingSubject;
    private readonly List<Subject> _allExistingSubjects;
    private readonly int _defaultDurationMinutes;
    private readonly List<Slot> _pendingSlots = new();
    private string _assignedColor = "#64B5F6";
    private bool _isInitializing = true;

    public Subject? ResultSubject { get; private set; }
    public bool IsDeleted { get; private set; } = false;

    public SubjectEditDialog(
        Subject? existingSubject = null,
        List<Subject>? allExistingSubjects = null,
        int defaultDurationMinutes = 60)
    {
        _existingSubject = existingSubject;
        _allExistingSubjects = allExistingSubjects ?? new List<Subject>();
        _defaultDurationMinutes = defaultDurationMinutes > 0 ? defaultDurationMinutes : 60;

        InitializeComponent();

        LoadSubjectData();

        _isInitializing = false;
        RefreshSlotsList();
    }

    private void LoadSubjectData()
    {
        if (_existingSubject != null)
        {
            TxtDialogTitle.Text = "✏️ EDIT SUBJECT";
            TxtSubjectName.Text = _existingSubject.Name;
            TxtDuration.Text = _existingSubject.DurationMinutes.ToString();
            TxtRoom.Text = _existingSubject.Room ?? "";
            _assignedColor = _existingSubject.Color;
            BtnDeleteSubject.Visibility = Visibility.Visible;

            // Clone existing slots
            if (_existingSubject.Slots != null)
            {
                foreach (var s in _existingSubject.Slots)
                {
                    _pendingSlots.Add(new Slot
                    {
                        Id = s.Id,
                        SubjectId = _existingSubject.Id,
                        DayOfWeek = s.DayOfWeek,
                        StartTime = s.StartTime
                    });
                }
            }
        }
        else
        {
            TxtDialogTitle.Text = "📚 ADD SUBJECT";
            TxtDuration.Text = _defaultDurationMinutes.ToString();
            var usedColors = _allExistingSubjects.Select(s => s.Color);
            _assignedColor = SubjectColorPalette.Next(usedColors);
        }

        UpdateColorChip();
    }

    private void UpdateColorChip()
    {
        try
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(_assignedColor)!;
            ColorChip.Background = brush;
        }
        catch
        {
            ColorChip.Background = new SolidColorBrush(WpfColor.FromRgb(100, 181, 246));
        }
    }

    private void RefreshSlotsList()
    {
        int duration = GetCurrentDuration();
        var items = _pendingSlots.Select(slot =>
        {
            var end = slot.GetEndTime(duration);
            var startStr = DateTime.Today.Add(slot.StartTime).ToString("hh:mm tt");
            var endStr = DateTime.Today.Add(end).ToString("hh:mm tt");
            var display = $"{slot.DayOfWeek.ToString().Substring(0, 3)}  {startStr} – {endStr}";
            return new SlotViewModel(slot, display);
        }).ToList();

        LstSlots.ItemsSource = items;
        TxtNoSlots.Visibility = _pendingSlots.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private int GetCurrentDuration()
    {
        if (int.TryParse(TxtDuration?.Text?.Trim(), out int val) && val > 0 && val <= 480)
        {
            return val;
        }
        return _defaultDurationMinutes;
    }

    private void TxtDuration_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing) return;
        RefreshSlotsList();
    }

    private void BtnDurationPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton btn && btn.Tag is string tag)
        {
            TxtDuration.Text = tag;
        }
    }

    private void BtnAddSlot_Click(object sender, RoutedEventArgs e)
    {
        int duration = GetCurrentDuration();
        var picker = new SlotPickerDialog(
            duration,
            _allExistingSubjects,
            _existingSubject?.Id,
            _pendingSlots
        )
        {
            Owner = this
        };

        if (picker.ShowDialog() == true && picker.ResultSlot != null)
        {
            _pendingSlots.Add(picker.ResultSlot);
            RefreshSlotsList();
        }
    }

    private void BtnDeleteSlot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.Tag is Slot slotToDelete)
        {
            _pendingSlots.Remove(slotToDelete);
            RefreshSlotsList();
        }
    }

    private void BtnDeleteSubject_Click(object sender, RoutedEventArgs e)
    {
        var result = WpfMessageBox.Show(
            $"Delete subject \"{_existingSubject?.Name}\" and all its scheduled slots?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            IsDeleted = true;
            DialogResult = true;
            Close();
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtSubjectName.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            WpfMessageBox.Show("Please enter a subject name.", "Missing Name", MessageBoxButton.OK, MessageBoxImage.Information);
            TxtSubjectName.Focus();
            return;
        }

        if (!int.TryParse(TxtDuration.Text?.Trim(), out int duration) || duration <= 0 || duration > 480)
        {
            WpfMessageBox.Show("Please enter a valid class duration in minutes (e.g. 50, 60, 90).", "Invalid Duration", MessageBoxButton.OK, MessageBoxImage.Information);
            TxtDuration.Focus();
            return;
        }

        var subjectId = _existingSubject?.Id ?? Guid.NewGuid();
        foreach (var s in _pendingSlots)
        {
            s.SubjectId = subjectId;
        }

        ResultSubject = new Subject
        {
            Id = subjectId,
            Name = name,
            DurationMinutes = duration,
            Room = TxtRoom.Text?.Trim() ?? "",
            Color = _assignedColor,
            Slots = _pendingSlots
        };

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private record SlotViewModel(Slot Slot, string DisplayText);
}
