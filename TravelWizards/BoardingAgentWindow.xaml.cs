/********************************************************************************************************
 * File: BoardingAgentWindow.cs
 * Author: Curuliuc Cosmin-Stefan
 * Created: 22.05.2024
 * Last Modified: 22.05.2024
 * Description: This file contains the implementation of the BoardingAgentWindow class which is responsible for
 *              managing the boarding process of passengers for specific trips.
 *
 * Purpose:
 * The purpose of this class is to allow boarding agents to manage passenger boarding by
 * interacting with a PostgreSQL database to retrieve and update boarding status.
 *
 * Usage:
 * - Users select the current location and trip.
 * - The application displays passengers assigned to the selected trip.
 * - Users can mark passengers as boarded/not boarded and generate a report.
 *
 * Notes:
 * - Ensure the PostgreSQL server is running and accessible.
 * - Database schema should include 'bookings', 'booking_details', and related tables.
 *
 ********************************************************************************************************/

namespace TravelWizards;

using Microsoft.Win32;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using Npgsql;
using TravelWizards.Data;

/// <summary>
/// Interaction logic for BoardingAgentWindow.xaml.
/// This window manages the boarding process for passengers.
/// </summary>
public partial class BoardingAgentWindow
{
    private readonly DatabaseService databaseService;
    private List<Passenger> passengers = [];

    /// <summary>
    /// Initializes a new instance of the BoardingAgentWindow class.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    public BoardingAgentWindow(string connectionString)
    {
        this.InitializeComponent();
        this.databaseService = new DatabaseService(connectionString);
        this.BoardingManifestElementsStackPanel.Visibility = Visibility.Collapsed;
        this.BoardingSelectRouteElementsStackPanel.Visibility = Visibility.Visible;
        this.Closed += this.BoardingAgentWindow_Closed;
        this.LoadLocations();
    }

    #region Event Handlers

    /// <summary>
    /// Handles the Closed event of the BoardingAgentWindow.
    /// Opens the LoginWindow when the BoardingAgentWindow is closed.
    /// </summary>
    private void BoardingAgentWindow_Closed(object? sender, EventArgs e)
    {
        var loginWindow = new LoginWindow();
        loginWindow.Show();
    }

    /// <summary>
    /// Handles the Click event of the Start Boarding button.
    /// Shows the boarding manifest and loads the passengers.
    /// </summary>
    private void ButtonStartBoarding_Click(object sender, RoutedEventArgs e)
    {
        this.BoardingSelectRouteElementsStackPanel.Visibility = Visibility.Collapsed;
        this.BoardingManifestElementsStackPanel.Visibility = Visibility.Visible;
        this.LoadPassengers();
    }

    /// <summary>
    /// Handles the SelectionChanged event for the current location ComboBox.
    /// Loads the available trips for the selected location.
    /// </summary>
    private void ComboBoxCurrentLocation_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        this.LoadTrips(this.ComboBoxCurrentLocation.SelectedItem?.ToString() ?? string.Empty);

    /// <summary>
    /// Handles the Click event of the Finish Boarding button.
    /// Generates a boarding report as a text file.
    /// </summary>
    private void FinishBoardingButton_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt",
            FileName = "BoardingReport.txt"
        };

        if (saveFileDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            using (var writer = new StreamWriter(saveFileDialog.FileName))
            {
                writer.WriteLine("Boarding Report");
                writer.WriteLine($"Current Location: {this.ComboBoxCurrentLocation.SelectedItem}");
                writer.WriteLine($"Trip: {this.ComboBoxCurrentTrip.SelectedItem}");
                writer.WriteLine();

                writer.WriteLine("Name\tHas Boarded?");
                writer.WriteLine("----\t-----------");

                foreach (var passenger in this.passengers)
                {
                    writer.WriteLine($"{passenger.Name}\t{passenger.HasBoarded}");
                }
            }

            Window messageBox = new CustomMessageWindow("info", "Success", "Boarding report generated successfully.");
            _ = messageBox.ShowDialog();
        }
        catch (Exception ex)
        {
            Window messageBox = new CustomMessageWindow("error", "Error", $"Failed to generate report: {ex.Message}");
            _ = messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Handles the Click event of the ToggleBoardedStatus button.
    /// Toggles the boarding status of the selected passenger.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void ToggleBoardedStatus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: Passenger passenger })
        {
            return;
        }

        try
        {
            this.databaseService.ToggleBoardedStatus(passenger);
        }
        catch (NpgsqlException ex)
        {
            Window messageBox = new CustomMessageWindow("error", "Database Error", $"Failed to toggle boarding status: {ex.Message}");
            _ = messageBox.ShowDialog();
        }

        // Refresh the DataGrid

        this.BoardingViewDataGrid.Items.Refresh();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads location names from the database and initializes the current location ComboBox.
    /// </summary>
    private void LoadLocations()
    {
        try
        {
            var locations = this.databaseService.LoadLocations();
            foreach (var location in locations)
            {
                this.ComboBoxCurrentLocation.Items.Add(location);
            }
        }
        catch (Exception ex)
        {
            Window messageBox = new CustomMessageWindow("error", "Database Error", $"Failed to load locations: {ex.Message}");
            _ = messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Loads available trips for the selected location from the database and initializes the current trip ComboBox.
    /// </summary>
    /// <param name="location">The selected location.</param>
    private void LoadTrips(string location)
    {
        this.ComboBoxCurrentTrip.Items.Clear();
        try
        {
            var trips = this.databaseService.LoadTrips(location);
            foreach (var trip in trips)
            {
                this.ComboBoxCurrentTrip.Items.Add(trip);
            }
        }
        catch (Exception ex)
        {
            Window messageBox = new CustomMessageWindow("error", "Database Error", $"Failed to load trips: {ex.Message}");
            _ = messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Loads passengers assigned to the selected trip from the database and displays them in the DataGrid.
    /// </summary>
    private void LoadPassengers()
    {
        try
        {
            var currentLocation = this.ComboBoxCurrentLocation.SelectedItem.ToString();
            if (currentLocation is null)
            {
                Window messageBox = new CustomMessageWindow("error", "Error", $"Please select a location.");
                _ = messageBox.ShowDialog();
                return;
            }
            this.passengers = this.databaseService.LoadPassengers(currentLocation);
            this.BoardingViewDataGrid.ItemsSource = this.passengers;
        }
        catch (Exception ex)
        {
            Window messageBox = new CustomMessageWindow("error", "Database Error", $"Failed to load passengers: {ex.Message}");
            _ = messageBox.ShowDialog();
        }
    }

    #endregion
}
