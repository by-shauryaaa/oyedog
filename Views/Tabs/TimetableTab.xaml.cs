using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PixelDogReminders.Models;
using PixelDogReminders.Services;
using PixelDogReminders.Views.Dialogs;
using WpfControl = System.Windows.Controls.UserControl;

namespace PixelDogReminders.Views.Tabs;

public partial class TimetableTab : WpfControl
{
    private readonly PersistenceService _persistence;

    public TimetableTab(PersistenceService persistence)
    {
        _persistence = persistence;

        InitializeComponent();

        ViewWeekGrid.SubjectSelected += (s, subj) => OpenSubjectEdit(subj);
        ViewScheduleList.SubjectSelected += (s, subj) => OpenSubjectEdit(subj);
        ViewScheduleList.AddSubjectRequested += (s, e) => AddNewSubject();

        Loaded += (s, e) => Refresh();
    }

    public void Refresh()
    {
        var subjects = _persistence.LoadSubjects();
        ViewWeekGrid.RenderSchedule(subjects);
        ViewScheduleList.RenderSchedule(subjects);
    }

    private void BtnWeekGrid_Click(object sender, RoutedEventArgs e)
    {
        BtnWeekGrid.Tag = "Active";
        BtnScheduleList.Tag = null;
        ViewWeekGrid.Visibility = Visibility.Visible;
        ViewScheduleList.Visibility = Visibility.Collapsed;
    }

    private void BtnScheduleList_Click(object sender, RoutedEventArgs e)
    {
        BtnScheduleList.Tag = "Active";
        BtnWeekGrid.Tag = null;
        ViewScheduleList.Visibility = Visibility.Visible;
        ViewWeekGrid.Visibility = Visibility.Collapsed;
    }

    private void BtnAddSubject_Click(object sender, RoutedEventArgs e)
    {
        AddNewSubject();
    }

    private void AddNewSubject()
    {
        var (settings, _, subjects) = _persistence.LoadAllData();
        var dialog = new SubjectEditDialog(
            existingSubject: null,
            allExistingSubjects: subjects,
            defaultDurationMinutes: settings.DefaultClassDurationMinutes
        )
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true && dialog.ResultSubject != null)
        {
            subjects.Add(dialog.ResultSubject);
            _persistence.SaveSubjects(subjects);
            Refresh();
        }
    }

    private void OpenSubjectEdit(Subject subject)
    {
        var (settings, _, subjects) = _persistence.LoadAllData();
        var dialog = new SubjectEditDialog(
            existingSubject: subject,
            allExistingSubjects: subjects,
            defaultDurationMinutes: settings.DefaultClassDurationMinutes
        )
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            if (dialog.IsDeleted)
            {
                subjects.RemoveAll(s => s.Id == subject.Id);
            }
            else if (dialog.ResultSubject != null)
            {
                var idx = subjects.FindIndex(s => s.Id == subject.Id);
                if (idx >= 0)
                {
                    subjects[idx] = dialog.ResultSubject;
                }
                else
                {
                    subjects.Add(dialog.ResultSubject);
                }
            }

            _persistence.SaveSubjects(subjects);
            Refresh();
        }
    }
}
