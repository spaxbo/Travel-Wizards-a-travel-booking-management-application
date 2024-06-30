/********************************************************************************************************
 * File: CompanyWindow.cs
 * Author: Cosmin-Ștefan Curuliuc
 * Created: 10.05.2024
 * Last Modified: 15.05.2024
 * Description: This file contains the implementation of the CompanyWindow class which is responsible for
 *              managing service additions for transportation companies.
 *
 * Purpose:
 * The purpose of this class is to allow transportation companies to add and manage their services by
 * interacting with a PostgreSQL database to store and retrieve service-related information.
 *
 * Usage:
 * - Users select locations, dates, and other service details.
 * - The application inserts the service details into the database.
 * - The user is notified of the successful addition of the service.
 *
 * Notes:
 * - Ensure the PostgreSQL server is running and accessible.
 * - Database schema should include 'locations', 'travel_routes', and 'route_schedules' tables.
 *
 ********************************************************************************************************/

namespace TravelWizards;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using TravelWizards.Data;

/// <summary>
/// Interaction logic for CompanyWindow.xaml.
/// This window manages service additions for transportation companies.
/// </summary>
public partial class CompanyWindow
{
    private readonly DatabaseService databaseService;
    private readonly List<string> allLocations = [];

    /// <summary>
    /// Initializes a new instance of the CompanyWindow class.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    public CompanyWindow(string connectionString)
    {
        this.InitializeComponent();
        this.databaseService = new DatabaseService(connectionString);
        this.LoadLocations();
        this.SetupEventHandlers();
        this.FetchCompanyName();
        this.FetchCompanyNameForRoutesTab();
        this.LoadRoutes();
        this.Closed += this.CompanyWindow_Closed;
    }

    #region Location Management

    /// <summary>
    /// Loads location names from the database and initializes dropdowns.
    /// </summary>
    private void LoadLocations()
    {
        try
        {
            var locations = this.databaseService.LoadLocations();
            foreach (var location in locations)
            {
                this.allLocations.Add(location);
            }

            this.ComboBoxDepartureLocation.ItemsSource = new List<string>(this.allLocations);
            this.ComboBoxArrivalLocation.ItemsSource = new List<string>(this.allLocations);
        }
        catch (Exception ex)
        {
            Window messageBox =
                new CustomMessageWindow("error", "Database Error", $"Failed to load locations: {ex.Message}");
            _ = messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Gets the ID of a location based on its name.
    /// </summary>
    /// <param name="locationName">The name of the location.</param>
    /// <returns>The ID of the location.</returns>
    private int GetLocationId(string locationName)
    {
        try
        {
            return this.databaseService.GetLocationId(locationName);
        }
        catch (Exception ex)
        {
            Window messageBox =
                new CustomMessageWindow("error", "Database Error", $"Failed to get location ID: {ex.Message}");
            _ = messageBox.ShowDialog();
            return 0;
        }
    }

    /// <summary>
    /// Handles the SelectionChanged event for the departure location ComboBox.
    /// </summary>
    private void DepartureLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this.ComboBoxDepartureLocation.SelectedItem == null)
        {
            return;
        }

        var selectedLocation = this.ComboBoxDepartureLocation.SelectedItem.ToString();
        this.ComboBoxArrivalLocation.ItemsSource = this.allLocations.Where(l => l != selectedLocation).ToList();
    }

    /// <summary>
    /// Handles the SelectionChanged event for the arrival location ComboBox.
    /// </summary>
    private void ArrivalLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this.ComboBoxArrivalLocation.SelectedItem == null)
        {
            return;
        }

        var selectedLocation = this.ComboBoxArrivalLocation.SelectedItem.ToString();
        this.ComboBoxDepartureLocation.ItemsSource = this.allLocations.Where(l => l != selectedLocation).ToList();
    }

    #endregion

    #region Company Name Management

    /// <summary>
    /// Fetches and displays the company name associated with the user.
    /// </summary>
    private void FetchCompanyName()
    {
        try
        {
            var userEmail = ExtractEmailFromConnectionString(this.databaseService.ConnectionString);
            var companyName = this.databaseService.FetchCompanyName(userEmail);
            this.Dispatcher.Invoke(() => this.LabelCompanyName.Content = companyName);
        }
        catch (Exception ex)
        {
            this.Dispatcher.Invoke(() => this.LabelCompanyName.Content = "Company name not found");
            Window messageBox =
                new CustomMessageWindow("error", "Database Error", $"Failed to fetch company name: {ex.Message}");
            _ = messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Fetches and displays the company name associated with the user in the Routes tab.
    /// </summary>
    private void FetchCompanyNameForRoutesTab()
    {
        try
        {
            var userEmail = ExtractEmailFromConnectionString(this.databaseService.ConnectionString);
            var companyName = this.databaseService.FetchCompanyName(userEmail);
            this.Dispatcher.Invoke(() => this.LabelCompanyNameRoutesTab.Content = companyName);
        }
        catch (Exception ex)
        {
            this.Dispatcher.Invoke(() => this.LabelCompanyNameRoutesTab.Content = "Company name not found");
            Window messageBox =
                new CustomMessageWindow("error", "Database Error", $"Failed to fetch company name: {ex.Message}");
            _ = messageBox.ShowDialog();
        }
    }

    #endregion

    #region Event Handlers Setup

    /// <summary>
    /// Sets up event handlers for user interactions.
    /// </summary>
    private void SetupEventHandlers()
    {
        this.TimePickerDepartureTime.TimeChanged += this.OnDateTimeChanged;
        this.TimePickerArrivalTime.TimeChanged += this.OnDateTimeChanged;
        this.DatePickerStartDate.SelectedDateChanged += this.OnDateTimeChanged;
        this.DatePickerEndDate.SelectedDateChanged += this.OnDateTimeChanged;
        this.ComboBoxDepartureLocation.SelectionChanged += this.DepartureLocation_SelectionChanged;
        this.ComboBoxArrivalLocation.SelectionChanged += this.ArrivalLocation_SelectionChanged;
    }

    #endregion

    #region Service Management

    /// <summary>
    /// Inserts a new service into the database based on user inputs.
    /// </summary>
    private void InsertService()
    {
        Window messageBox;
        if (this.ComboBoxDepartureLocation.SelectedItem == null || this.ComboBoxArrivalLocation.SelectedItem == null ||
            !this.DatePickerStartDate.SelectedDate.HasValue || !this.DatePickerEndDate.SelectedDate.HasValue ||
            this.TimePickerDepartureTime.Time == TimeSpan.Zero || this.TimePickerArrivalTime.Time == TimeSpan.Zero)
        {
            messageBox = new CustomMessageWindow("info", "Input Error", "Please fill all required fields.");
            _ = messageBox.ShowDialog();
            return;
        }

        var departureLocation = this.ComboBoxDepartureLocation.SelectedItem.ToString();
        var arrivalLocation = this.ComboBoxArrivalLocation.SelectedItem.ToString();
        if (departureLocation is null)
        {
            Window errorMessageBox = new CustomMessageWindow("error", "Error", "Please select a departure location");
            _ = errorMessageBox.ShowDialog();
            return;
        }

        if (arrivalLocation is null)
        {
            Window errorMessageBox = new CustomMessageWindow("error", "Error", "Please select an arrival location");
            _ = errorMessageBox.ShowDialog();
            return;
        }

        var departureLocationId = this.GetLocationId(departureLocation);
        var arrivalLocationId = this.GetLocationId(arrivalLocation);
        var transportType = this.GetSelectedTransportType();
        var frequency = this.GetSelectedFrequency();

        var departureDateTime = this.DatePickerStartDate.SelectedDate.Value.Date + this.TimePickerDepartureTime.Time;
        var arrivalDateTime = this.DatePickerEndDate.SelectedDate.Value.Date + this.TimePickerArrivalTime.Time;

        if (arrivalDateTime < departureDateTime)
        {
            arrivalDateTime = arrivalDateTime.AddDays(1);
        }

        var validFrom = this.DatePickerStartDate.SelectedDate.Value.Date;
        var validUntil = this.DatePickerEndDate.SelectedDate.Value.Date;

        var price = int.Parse(this.TextBoxPrice.Text, System.Globalization.CultureInfo.InvariantCulture);

        try
        {
            using (var conn = new NpgsqlConnection(this.databaseService.ConnectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    var routeId =
                        this.databaseService.InsertTravelRoute(departureLocationId, arrivalLocationId, transportType);
                    this.databaseService.InsertRouteSchedule(routeId, departureDateTime, arrivalDateTime, price,
                        frequency, validFrom, validUntil);
                    tran.Commit();
                }
            }

            messageBox = new CustomMessageWindow("info", "Success", "Service added successfully!");
            _ = messageBox.ShowDialog();

            this.LoadRoutes();
        }
        catch (Exception ex)
        {
            messageBox = new CustomMessageWindow("error", "Database Error", $"Failed to add service: {ex.Message}");
            _ = messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Handles the Click event of the Add Service button.
    /// </summary>
    private void ButtonAddService_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            this.InsertService();
        }
        catch (Exception ex)
        {
            Window messageBox =
                new CustomMessageWindow("error", "Database Error", $"Failed to add service: {ex.Message}");
            _ = messageBox.ShowDialog();
        }
    }

    #endregion

    #region Routes Management

    /// <summary>
    /// Loads the routes from the database and sets them as the DataGrid's data source.
    /// </summary>
    private void LoadRoutes(string? locationFilter = null)
    {
        var routes = new List<Route>();

        try
        {
            var query = """
                        SELECT
                            l1.abbreviation AS departure,
                            l2.abbreviation AS arrival,
                            rs.price,
                            (rs.price / EXTRACT(EPOCH FROM (rs.arrival_time - rs.departure_time) / 3600.0)) AS price_ratio,
                            rs.departure_time::time AS departure_time,
                            rs.arrival_time::time AS arrival_time
                        FROM
                        travel_routes tr
                        JOIN route_schedules rs ON tr.route_id = rs.route_id
                        JOIN locations l1 ON tr.departure_location_id = l1.location_id
                        JOIN locations l2 ON tr.arrival_location_id = l2.location_id
                        """;

            if (!string.IsNullOrEmpty(locationFilter))
            {
                query += """
                         WHERE l1.full_name = @LocationFilter OR l2.full_name = @LocationFilter
                         """;
            }

            using var conn = new NpgsqlConnection(this.databaseService.ConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            if (!string.IsNullOrEmpty(locationFilter))
            {
                cmd.Parameters.AddWithValue("@LocationFilter", locationFilter);
            }

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                routes.Add(new Route(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetDouble(3),
                    TimeOnly.FromTimeSpan(reader.GetTimeSpan(4)),
                    TimeOnly.FromTimeSpan(reader.GetTimeSpan(5))
                ));
            }
        }
        catch (Exception ex)
        {
            Window messageBox =
                new CustomMessageWindow("error", "Database Error", $"Failed to load routes: {ex.Message}");
            _ = messageBox.ShowDialog();
        }

        if (routes.Count == 0)
        {
            Window messageBox = new CustomMessageWindow("info", "Information", "No routes available");
            _ = messageBox.ShowDialog();
        }

        this.RoutesDataGrid.ItemsSource = routes.Count > 0
            ? routes
            :
            [
                new Route("N/A", "N/A", 0, 0.0, TimeOnly.MinValue, TimeOnly.MinValue)
            ];
    }

    /// <summary>
    /// Handles the Click event of the Search by Location button.
    /// </summary>
    private void ButtonSearchByLocation_Click(object sender, RoutedEventArgs e)
    {
        var location = this.TextBoxSearchByLocation.Text.Trim();
        if (string.IsNullOrEmpty(location))
        {
            this.LoadRoutes();
        }
        else if (this.allLocations.Contains(location))
        {
            this.LoadRoutes(location);
        }
        else
        {
            Window messageBox = new CustomMessageWindow("error", "Error", "Invalid location.");
            _ = messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Handles the Click event of the Delete All Shown button.
    /// Deletes all routes currently shown in the DataGrid.
    /// </summary>
    private void ButtonDeleteAllShown_Click(object sender, RoutedEventArgs e)
    {
        Window messageBox;
        if (this.RoutesDataGrid.ItemsSource is List<Route> { Count: > 0 } routes)
        {
            var realRoutes = routes.Where(route => route.Departure != "N/A" && route.Arrival != "N/A").ToList();
            if (realRoutes.Count > 0)
            {
                try
                {
                    this.databaseService.DeleteShownRoutes(realRoutes);
                    this.RoutesDataGrid.ItemsSource = new List<Route>
                    {
                        new("N/A", "N/A", 0, 0.0, TimeOnly.MinValue, TimeOnly.MinValue)
                    };
                }
                catch (Exception ex)
                {
                    messageBox = new CustomMessageWindow("error", "Database Error",
                        $"Failed to delete routes: {ex.Message}");
                    _ = messageBox.ShowDialog();
                }
            }
            else
            {
                messageBox = new CustomMessageWindow("info", "Information", "No routes to delete.");
                _ = messageBox.ShowDialog();
            }
        }
        else
        {
            messageBox = new CustomMessageWindow("info", "Information", "No routes to delete.");
            _ = messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Handles the Click event of the Cancel button in the DataGrid.
    /// Deletes the selected route from the database and updates the DataGrid.
    /// </summary>
    private void CancelRoute_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: Route route })
        {
            return;
        }

        try
        {
            this.databaseService.DeleteRoute(route);
            this.LoadRoutes();
        }
        catch (Exception ex)
        {
            Window messageBox =
                new CustomMessageWindow("error", "Database Error", $"Failed to delete route: {ex.Message}");
            _ = messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Handles the CellEditEnding event for the DataGrid.
    /// Updates the price and recalculates the price ratio if the price cell is edited.
    /// </summary>
    private void RoutesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Column.Header.ToString() != "Price" || e.Row.Item is not Route route)
        {
            return;
        }

        if (int.TryParse(((TextBox)e.EditingElement).Text, out var newPrice))
        {
            route.Price = newPrice;
            var duration = route.ArrivalTime - route.DepartureTime;
            route.PriceRatio = Math.Round(newPrice / duration.TotalHours, 2);

            try
            {
                this.databaseService.UpdateRoutePrice(route);
                this.LoadRoutes();
            }
            catch (Exception ex)
            {
                Window messageBox = new CustomMessageWindow("error", "Database Error",
                    $"Failed to update route price: {ex.Message}");
                _ = messageBox.ShowDialog();
            }
        }
        else
        {
            Window messageBox = new CustomMessageWindow("error", "Input Error", "Invalid price value.");
            _ = messageBox.ShowDialog();
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Handles the Closed event of the CompanyWindow.
    /// Opens the LoginWindow when the CompanyWindow is closed.
    /// </summary>
    private void CompanyWindow_Closed(object? sender, EventArgs e)
    {
        var loginWindow = new LoginWindow();
        loginWindow.Show();
    }

    /// <summary>
    /// Gets the selected transport type.
    /// </summary>
    /// <returns>The selected transport type as a string.</returns>
    private string? GetSelectedTransportType()
    {
        if (this.AirplaneRadioButton.IsChecked == true)
        {
            return "airplane";
        }

        if (this.BoatRadioButton.IsChecked == true)
        {
            return "ship";
        }

        if (this.TrainRadioButton.IsChecked == true)
        {
            return "train";
        }

        if (this.BusRadioButton.IsChecked == true)
        {
            return "bus";
        }

        return null;
    }

    /// <summary>
    /// Gets the selected frequency.
    /// </summary>
    /// <returns>The selected frequency as a TimeSpan.</returns>
    private TimeSpan GetSelectedFrequency()
    {
        if (this.ComboBoxFrequency.SelectedItem is not ComboBoxItem selectedItem)
        {
            return TimeSpan.Zero;
        }

        var frequency = selectedItem.Content.ToString();
        return frequency switch
        {
            "Daily" => TimeSpan.FromDays(1),
            "Weekly" => TimeSpan.FromDays(7),
            "Bi-weekly" => TimeSpan.FromDays(14),
            "Monthly" => TimeSpan.FromDays(30),
            _ => TimeSpan.Zero
        };
    }

    /// <summary>
    /// Handles the DateTime changed events.
    /// </summary>
    private void OnDateTimeChanged(object sender, EventArgs e)
    {
        if (!this.DatePickerStartDate.SelectedDate.HasValue || !this.DatePickerEndDate.SelectedDate.HasValue ||
            this.TimePickerDepartureTime.Time == TimeSpan.Zero || this.TimePickerArrivalTime.Time == TimeSpan.Zero)
        {
            return;
        }

        var departureDateTime = this.DatePickerStartDate.SelectedDate.Value.Date + this.TimePickerDepartureTime.Time;
        var arrivalDateTime = this.DatePickerEndDate.SelectedDate.Value.Date + this.TimePickerArrivalTime.Time;

        if (departureDateTime >= arrivalDateTime || DateTime.Now > departureDateTime || DateTime.Now > arrivalDateTime)
        {
            this.LabelTripTime.Content = "Invalid - Check Dates and Times";
            this.ButtonAddService.IsEnabled = false;
        }
        else
        {
            var timeDifference = arrivalDateTime - departureDateTime;
            this.LabelTripTime.Content = $"Trip time: {timeDifference.Hours:D2}:{timeDifference.Minutes:D2}";
            this.ButtonAddService.IsEnabled = true;
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
        return Array.Find(parameters, param => param.StartsWith("Username=", StringComparison.InvariantCulture))?[
            "Username=".Length..] ?? "";
    }

    #endregion
}
