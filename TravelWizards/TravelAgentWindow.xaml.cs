/********************************************************************************************************
 * File: TravelAgentWindow.cs
 * Author: Curuliuc Cosmin-Stefan
 * Created: 21.05.2024
 * Last Modified: 21.05.2024
 * Description: This file contains the implementation of the TravelAgentWindow class which is responsible for
 *              managing travel route reservations for travel agents.
 *
 * Purpose:
 * The purpose of this class is to allow travel agents to search for routes, make reservations, and view existing
 * reservations by interacting with a PostgreSQL database.
 *
 * Usage:
 * - Users can search for routes by selecting departure and arrival locations.
 * - Users can reserve a selected route for a specified user.
 * - Users can view all reservations made by a specified user.
 *
 ********************************************************************************************************/

namespace TravelWizards;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using TravelWizards.Data;

/// <summary>
/// Interaction logic for TravelAgentWindow.xaml.
/// This window manages travel route reservations for travel agents.
/// </summary>
public partial class TravelAgentWindow
{
    private readonly DatabaseService databaseService;
    private readonly List<string> allLocations;
    private bool isSearchForBooking; // Flag to track the search mode
    public event PropertyChangedEventHandler PropertyChanged = delegate { };

    /// <summary>
    /// Initializes a new instance of the TravelAgentWindow class.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    public TravelAgentWindow(string connectionString)
    {
        this.InitializeComponent();
        this.databaseService = new DatabaseService(connectionString);
        try
        {
            this.allLocations = this.databaseService.LoadLocations();
        }
        catch (NpgsqlException ex)
        {
            Window messageBox = new CustomMessageWindow("error", "Database Error", $"Failed to load locations: {ex.Message}");
            _ = messageBox.ShowDialog();

            this.Close();
        }

        this.DataContext = this;
        this.LoadLocations();
        this.FetchCompanyName();
        this.Closed += this.TravelAgentWindow_Closed;
    }

    #region Location Management

    /// <summary>
    /// Loads location names from the database and initializes dropdowns.
    /// </summary>
    private void LoadLocations()
    {
        this.ComboBoxDeparture.ItemsSource = new List<string>(this.allLocations);
        this.ComboBoxArrival.ItemsSource = new List<string>(this.allLocations);
    }

    /// <summary>
    /// Handles the SelectionChanged event for the departure location ComboBox.
    /// </summary>
    private void ComboBoxDeparture_SelectionChanged(object sender, SelectionChangedEventArgs e) => this.UpdateArrivalComboBox();

    /// <summary>
    /// Handles the SelectionChanged event for the arrival location ComboBox.
    /// </summary>
    private void ComboBoxArrival_SelectionChanged(object sender, SelectionChangedEventArgs e) => this.UpdateDepartureComboBox();

    /// <summary>
    /// Updates the Arrival ComboBox based on the selected Departure location.
    /// </summary>
    private void UpdateArrivalComboBox()
    {
        if (this.ComboBoxDeparture.SelectedItem == null)
        {
            return;
        }

        var selectedLocation = this.ComboBoxDeparture.SelectedItem.ToString();
        this.ComboBoxArrival.ItemsSource = this.allLocations.FindAll(location => location != selectedLocation);
    }

    /// <summary>
    /// Updates the Departure ComboBox based on the selected Arrival location.
    /// </summary>
    private void UpdateDepartureComboBox()
    {
        if (this.ComboBoxArrival.SelectedItem == null)
        {
            return;
        }

        var selectedLocation = this.ComboBoxArrival.SelectedItem.ToString();
        this.ComboBoxDeparture.ItemsSource = this.allLocations.FindAll(location => location != selectedLocation);
    }

    #endregion

    #region Company Name Management

    /// <summary>
    /// Fetches and displays the company name associated with the user.
    /// </summary>
    private void FetchCompanyName()
    {
        var userEmail = ExtractEmailFromConnectionString(this.databaseService.ConnectionString);
        var companyName = this.databaseService.FetchCompanyName(userEmail);
        this.Dispatcher.Invoke(() => this.LabelCompanyName.Content = companyName);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the Click event of the Search button.
    /// </summary>
    private void ButtonSearch_Click(object sender, RoutedEventArgs e)
    {
        this.isSearchForBooking = true;
        this.LoadIdealRoutes();
    }

    /// <summary>
    /// Handles the Click event of the Display button.
    /// </summary>
    private void ButtonReservationsDisplay_Click(object sender, RoutedEventArgs e)
    {
        this.isSearchForBooking = false;
        this.LoadUserReservations();
    }

    /// <summary>
    /// Handles the DoubleClick event to display a PopUp.
    /// </summary>
    private void DataGridRoutes_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (this.DataGridRoutes.SelectedItem is RouteInfo selectedRoute)
        {
            var routeDetails = GetRouteDetails(selectedRoute);
            var popup = new TravelAgentWindowRoutePopup(selectedRoute.DepartureTime, selectedRoute.ArrivalTime, routeDetails);
            popup.ShowDialog();
        }
    }

    /// <summary>
    /// Handles the Cancelation of a route.
    /// </summary>
    private void CancelReservation_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not RouteInfo selectedReservation)
        {
            return;
        }

        var userName = this.TextBoxReservationsName.Text;
        var nameParts = userName.Split(' ');
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        try
        {
            this.databaseService.CancelReservation(selectedReservation, firstName, lastName);
            Window messageBox = new CustomMessageWindow("info", "Success", "Reservation canceled.");
            _ = messageBox.ShowDialog();
            this.LoadUserReservations(); // Reload user reservations to reflect changes
        }
        catch (Exception ex)
        {
            Window messageBox = new CustomMessageWindow("error", "Error", "Failed to cancel reservation: " + ex.Message);
            _ = messageBox.ShowDialog();
        }
    }

    #endregion

    #region Route Management

    /// <summary>
    /// Loads optimal routes from the database based on the selected departure and arrival locations.
    /// </summary>
    private void LoadIdealRoutes()
    {
        var departure = this.ComboBoxDeparture.SelectedItem?.ToString();
        var arrival = this.ComboBoxArrival.SelectedItem?.ToString();

        if (string.IsNullOrEmpty(departure) || string.IsNullOrEmpty(arrival))
        {
            Window messageBox = new CustomMessageWindow("error", "Input Error", "Please select both departure and arrival locations.");
            _ = messageBox.ShowDialog();
            return;
        }

        try
        {
            var routes = this.databaseService.LoadIdealRoutes(departure, arrival);
            if (routes.Count == 0)
            {
                Window messageBox = new CustomMessageWindow("info", "Information", "No optimal routes found.");
                _ = messageBox.ShowDialog();
            }

            this.DataGridRoutes.ItemsSource = routes;
        }
        catch (Exception ex)
        {
            Window messageBox = new CustomMessageWindow("error", "Error", "Failed to load routes: " + ex.Message);
            _ = messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Reserves the selected route for the user.
    /// </summary>
    /// <param name="sender">The sender object.</param>
    private void ReserveSelectedRoute(object sender)
    {
        if (((FrameworkElement)sender).DataContext is not RouteInfo selectedRoute)
        {
            return;
        }

        var userName = this.TextBoxName.Text;
        if (string.IsNullOrWhiteSpace(userName))
        {
            Window messageBox = new CustomMessageWindow("info", "Missing Information", "Please enter a name before reserving.");
            _ = messageBox.ShowDialog();
            return;
        }

        try
        {
            this.databaseService.ReserveRoute(selectedRoute, userName);
            Window messageBox = new CustomMessageWindow("info", "Success", "Reservation successful.");
            _ = messageBox.ShowDialog();
        }
        catch (Exception ex)
        {
            Window messageBox = new CustomMessageWindow("error", "Error", "Failed to reserve: " + ex.Message);
            _ = messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Displays the reservations for the specified user.
    /// </summary>
    private void LoadUserReservations()
    {
        var userName = this.TextBoxReservationsName.Text;
        if (string.IsNullOrWhiteSpace(userName))
        {
            Window messageBox = new CustomMessageWindow("info", "Missing Information", "Please enter a name to display reservations.");
            _ = messageBox.ShowDialog();
            return;
        }

        var nameParts = userName.Split(' ');
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        try
        {
            var reservations = this.databaseService.LoadUserReservations(firstName, lastName);
            this.DataGridRoutes.ItemsSource = reservations;
        }
        catch (Exception ex)
        {
            Window messageBox = new CustomMessageWindow("error", "Error", "Failed to load reservations: " + ex.Message);
            _ = messageBox.ShowDialog();
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Handles the Closed event of the TravelAgentWindow.
    /// Opens the LoginWindow when the TravelAgentWindow is closed.
    /// </summary>
    private void TravelAgentWindow_Closed(object? sender, EventArgs e)
    {
        var loginWindow = new LoginWindow();
        loginWindow.Show();
    }

    /// <summary>
    /// Handles the Generic Button Click of the TravelAgentWindow
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void GenericButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.isSearchForBooking)
        {
            this.ReserveSelectedRoute(sender);
        }
        else
        {
            this.CancelReservation_Click(sender, e);
        }
    }

    /// <summary>
    /// Extracts the email from the connection string.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>The extracted email.</returns>
    private static string ExtractEmailFromConnectionString(string connectionString)
    {
        var parameters = connectionString.Split(';');
        return Array.Find(parameters, param => param.StartsWith("Username=", StringComparison.InvariantCulture))?["Username=".Length..] ?? "";
    }

    /// <summary>
    /// Static method to build a string for the Travel Agent PopUp
    /// </summary>
    /// <param name="routeInfo"></param>
    /// <returns></returns>
    private static string GetRouteDetails(RouteInfo routeInfo) =>
        // Build a detailed string about the route
        $"{routeInfo.DepartureLocation} -- {routeInfo.DepartureTime:HH:mm} --> {routeInfo.ArrivalLocation}";

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether the current mode is search for booking.
    /// </summary>
    public bool IsSearchForBooking
    {
        get => this.isSearchForBooking;
        set
        {
            if (this.isSearchForBooking == value)
            {
                return;
            }

            this.isSearchForBooking = value;
            this.OnPropertyChanged(nameof(this.IsSearchForBooking));
            this.OnPropertyChanged(nameof(this.ActionButtonText));
        }
    }

    /// <summary>
    /// Gets the text to be displayed on the action button based on the current mode.
    /// </summary>
    public string ActionButtonText => this.IsSearchForBooking ? "Book" : "Cancel";

    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName) => this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

    #endregion
}
