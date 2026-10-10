using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.Application.FrontOffice.Housekeeping;

namespace ASMILE.BO.App.Helpers;

/// <summary>
/// Small shared picker for persisted Housekeeping assignees. The picker only
/// selects an active user; the caller still has to execute the real workflow
/// command before changing its view model.
/// </summary>
public static class HousekeepingAssigneePicker
{
    public static HkAssigneeDto? Show(
        Window? owner,
        string title,
        string roomNumber,
        IReadOnlyList<HkAssigneeDto> assignees,
        Guid? selectedUserId = null)
    {
        if (assignees.Count == 0)
            return null;

        var dialog = new Window
        {
            Title = title,
            Width = 460,
            Height = 220,
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        var staffBox = new ComboBox
        {
            ItemsSource = assignees,
            DisplayMemberPath = nameof(HkAssigneeDto.DisplayName),
            SelectedIndex = Math.Max(0, assignees.ToList().FindIndex(x => x.UserId == selectedUserId)),
            Margin = new Thickness(0, 6, 0, 16)
        };
        var save = new Button
        {
            Content = "Lưu phân công",
            IsDefault = true,
            Padding = new Thickness(16, 6, 16, 6),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        save.Click += (_, _) =>
        {
            if (staffBox.SelectedItem is HkAssigneeDto)
            {
                dialog.DialogResult = true;
                dialog.Close();
            }
        };

        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock
        {
            Text = $"Phòng {roomNumber}",
            FontWeight = FontWeights.SemiBold,
            Foreground = System.Windows.Media.Brushes.DarkSlateBlue
        });
        panel.Children.Add(new TextBlock { Text = "Nhân viên Housekeeping", Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(staffBox);
        panel.Children.Add(save);
        dialog.Content = panel;

        return dialog.ShowDialog() == true ? staffBox.SelectedItem as HkAssigneeDto : null;
    }
}
