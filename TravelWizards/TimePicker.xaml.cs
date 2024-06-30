/********************************************************************************************************
 * File: TimePicker.cs
 * Author: Obrocea Traian, Curuliuc Cosmin-Ștefan
 * Created: 15.05.2024
 * Last Modified: 15.05.2024
 * Description: This file contains the implementation of the TimePicker class which is responsible for
 *              managing the selection of time values using ComboBoxes for hours, minutes, and AM/PM.
 *
 * Purpose:
 * The purpose of this class is to allow users to select a specific time using a user-friendly interface
 * composed of ComboBoxes for hours, minutes, and AM/PM designations.
 *
 * Usage:
 * - Users select the hour, minute, and AM/PM values from the ComboBoxes.
 * - The selected time is combined into a TimeSpan object representing the selected time.
 * - The TimeChanged event is triggered whenever a selection is changed.
 *
 * Notes:
 * - Ensure the ComboBoxes for hours, minutes, and AM/PM are properly initialized in the XAML file.
 *
 ********************************************************************************************************/

namespace TravelWizards;

using System;
using System.Windows.Controls;

/// <summary>
/// Interaction logic for TimePicker.xaml.
/// This class provides a user interface component for selecting a time value.
/// </summary>
public partial class TimePicker
{
    #region Events

    /// <summary>
    /// Event that is triggered whenever the selected time is changed.
    /// </summary>
    public event EventHandler? TimeChanged;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the TimePicker class.
    /// </summary>
    public TimePicker()
    {
        this.InitializeComponent();
        this.ComboBoxHour.SelectionChanged += this.ComboBox_SelectionChanged;
        this.ComboBoxMinute.SelectionChanged += this.ComboBox_SelectionChanged;
        this.ComboBoxAmPm.SelectionChanged += this.ComboBox_SelectionChanged;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the SelectionChanged event for the ComboBoxes.
    /// Triggers the TimeChanged event whenever a selection is changed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The SelectionChangedEventArgs that contains the event data.</param>
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        this.TimeChanged?.Invoke(this, EventArgs.Empty);

    #endregion

    #region Properties

    /// <summary>
    /// Gets the selected time as a TimeSpan object.
    /// </summary>
    public TimeSpan Time
    {
        get
        {
            if (this.ComboBoxHour.SelectedItem == null || this.ComboBoxMinute.SelectedItem == null ||
                this.ComboBoxAmPm.SelectedItem == null)
            {
                return TimeSpan.Zero;
            }

            var hour = int.Parse((string)((ComboBoxItem)this.ComboBoxHour.SelectedItem).Content,
                System.Globalization.CultureInfo.InvariantCulture);
            var minute = int.Parse((string)((ComboBoxItem)this.ComboBoxMinute.SelectedItem).Content,
                System.Globalization.CultureInfo.InvariantCulture);
            var amPm = (string)((ComboBoxItem)this.ComboBoxAmPm.SelectedItem).Content;

            switch (amPm)
            {
                case "PM" when hour < 12:
                    hour += 12;
                    break;
                case "AM" when hour == 12:
                    hour = 0;
                    break;
            }

            return new TimeSpan(hour, minute, 0);
        }
    }

    #endregion
}
