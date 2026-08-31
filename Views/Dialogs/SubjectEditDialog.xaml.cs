using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using PixelDogReminders.Models;
using WpfMessageBox = System.Windows.MessageBox;
using WpfColor = System.Windows.Media.Color;

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
        _defaultDurationMinutes = defaultDurationMinutes;

        InitializeComponent();

        PopulateDurations();
        LoadSubjectData();

        _isInitializing = false;
        RefreshSlotsList();
    }

    private void PopulateDurations()
    {
        var durations = new[] { 30, 45, 50, 60, 75, 90, 100, 120, 150, 180 };
        foreach (var d in durations)
        {
            CmbDuration.Items.Add(new DurationItem(d, $"{d} minutes"));
        }
    }

    private void LoadSubjectData()
    {
        if (_existingSubject != null)
        {
            TxtDialogTitle.Text = "✏️ EDIT SUBJECT";
            TxtSubjectName.Text = _existingSubject.Name;
            TxtRoom.Text = _existingSubject.Room ?? "";
            _assignedColor = _existingSubject.Color;
            BtnDeleteSubject.Visibility = Visibility.Visible;

            // Select matching duration
            foreach (DurationItem item in CmbDuration.Items)
            {
                if (item.Minutes == _existingSubject.DurationMinutes)
                {
                    CmbDuration.SelectedItem = item;
                    break;
                }
            }
            if (CmbDuration.SelectedItem == null)
            {
                CmbDuration.SelectedIndex = 3; // 60 min default
            }

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
            var usedColors = _allExistingSubjects.Select(s => s.Color);
            _assignedColor = SubjectColorPalette.Next(usedColors);

            // Select default duration
            foreach (DurationItem item in CmbDuration.Items)
            {
                if (item.Minutes == _defaultDurationMinutes)
                {
                    CmbDuration.SelectedItem = item;
                    break;
                }
            }
            if (CmbDuration.SelectedItem == null)
            {
                CmbDuration.SelectedIndex = 3; // 60 min default
            }
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
        if (CmbDuration.SelectedItem is DurationItem item)
        {
            return item.Minutes;
        }
        return _defaultDurationMinutes;
    }

    private void CmbDuration_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        RefreshSlotsList();
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

        var subjectId = _existingSubject?.Id ?? Guid.NewGuid();
        foreach (var s in _pendingSlots)
        {
            s.SubjectId = subjectId;
        }

        ResultSubject = new Subject
        {
            Id = subjectId,
            Name = name,
            DurationMinutes = GetCurrentDuration(),
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

    private record DurationItem(int Minutes, string Display)
    {
        public override string ToString() => Display;
    }

    private record SlotViewModel(Slot Slot, string DisplayText);
}
