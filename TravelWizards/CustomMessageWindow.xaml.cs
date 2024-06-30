/********************************************************************************************************
 * File: CustomMessageWindow.cs
 * Author: Obrocea Traian
 * Created: 21.05.2024
 * Last Modified: 22.05.2024
 * Description: This file contains the implementation of the CustomMessageWindow class which is used to display
 *              customized message boxes to the user.
 *
 * Purpose:
 * The purpose of this class is to provide a flexible way to display informational or error messages to the user,
 * with custom visuals that align with the application's theme and branding.
 *
 * Usage:
 * - Instances of this class can be created to show either informational or error messages depending on the context.
 * - This class allows for more visual customization compared to standard message box dialogs.
 *
 * Example:
 * var messageWindow = new CustomMessageWindow("info", "Operation Successful", "Your transaction has been processed successfully.");
 * messageWindow.ShowDialog();
 *
 ********************************************************************************************************/

namespace TravelWizards;
using System;
using System.Windows.Media.Imaging;

/// <summary>
/// Interaction logic for CustomMessageWindow.xaml
/// </summary>
public partial class CustomMessageWindow
{
    /// <summary>
    /// Create a new CustomMessageWindow, which represents a message box to be displayed to the user with ShowDialog
    /// </summary>
    /// <param name="type">The type of the message box, which is one of "info" or "error"</param>
    /// <param name="title">The title of the message box</param>
    /// <param name="message">The content of the message box</param>
    /// <exception cref="ArgumentException">Thrown if type is not one of "info" or "error"</exception>
    public CustomMessageWindow(string type, string title, string message)
    {
        this.InitializeComponent();
        this.Illustration.Source = type switch
        {
            "info" => new BitmapImage(new Uri("assets/informational_messagebox_illustration.png", UriKind.Relative)),
            "error" => new BitmapImage(new Uri("assets/error_messagebox_illustration.png", UriKind.Relative)),
            _ => throw new ArgumentException("Type must be one of \"info\" or \"error\"")
        };
        this.Title = title;
        this.TitleLabel.Content = title;
        this.MessageTextBlock.Text = message;
    }

    /// <summary>
    /// Handles the Click event of the OK button to close the popup window.
    /// </summary>
    private void OKButton_Click(object sender, System.Windows.RoutedEventArgs e) => this.Close();
}
