/********************************************************************************************************
 * File: TravelAgentWindowRoutePopup.cs
 * Author: Obrocea Traian
 * Created: 21.05.2024
 * Last Modified: 21.05.2024
 * Description: This file contains the implementation of the TravelAgentWindowRoutePopup class which is
 *              responsible for displaying route details in a popup window.
 *
 * Purpose:
 * The purpose of this class is to show the details of a selected route including departure time, arrival
 * time, and other route-specific information in a popup window.
 *
 * Usage:
 * - Instances of this class are used to display route details when a route is double-clicked in the main
 *   DataGrid.
 * - The class provides a simple UI to display the route details.
 *
 ********************************************************************************************************/

namespace TravelWizards;

using System;
using System.Windows;

/// <summary>
/// Interaction logic for TravelAgentWindowRoutePopup.xaml
/// </summary>
public partial class TravelAgentWindowRoutePopup
{
    /// <summary>
    /// Initializes a new instance of the TravelAgentWindowRoutePopup class.
    /// </summary>
    /// <param name="departureTime">The departure time of the route.</param>
    /// <param name="arrivalTime">The arrival time of the route.</param>
    /// <param name="routeDetails">The details of the route.</param>
    public TravelAgentWindowRoutePopup(DateTime departureTime, DateTime arrivalTime, string routeDetails)
    {
        this.InitializeComponent();
        this.DepartureTimeTextBlock.Text = $"Departure Time: {departureTime:HH:mm}";
        this.ArrivalTextBlock.Text = $"Arrival Time: {arrivalTime:HH:mm}";
        this.RouteTextBlock.Text = routeDetails;
    }

    #region Event Handlers

    /// <summary>
    /// Handles the Click event of the OK button to close the popup window.
    /// </summary>
    private void OKButton_Click(object sender, RoutedEventArgs e) => this.Close();

    #endregion
}
