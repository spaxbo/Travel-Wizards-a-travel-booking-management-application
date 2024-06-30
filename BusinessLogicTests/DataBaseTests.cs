/********************************************************************************************************
 * File: DataBaseTests.cs
 * Author: Diaconu Rareș-George
 * Created: 22.05.2024
 * Last Modified: 22.05.2024
 * Description: This file contains the implementation of the DataBaseTests class which provides tests on the methods
 *              that interact with the PostgreSQL database for managing travel routes.
 *
 * Purpose:
 * The purpose of this class is to test the encapsulation of all database operations related to travel management,
 * including loading locations, trips, and passengers, as well as handling reservations and route schedules.
 *
 * Notes:
 * - The tests to be done properly
 * - Ensure the PostgreSQL server is running and accessible.
 * - Database schema should include relevant tables such as 'locations', 'travel_routes', 'route_schedules'.
 *
 ********************************************************************************************************/

namespace TravelWizards.Data.Tests;

using System.Data;
using Npgsql;

/// <summary>
/// Contains unit tests for the DataBase class.
/// </summary>
[TestClass]
public class DataBaseTests : DatabaseServiceTests
{
    private string connectionString = "";
    private NpgsqlConnection connectionAdmin = null!;

    /// <summary>
    /// Initializes the test environment before running each test method.
    /// </summary>
    [TestInitialize]
    public new void TestInitialize()
    {
        base.TestInitialize();
        this.connectionString =
            "Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433";
        this.connectionAdmin = new NpgsqlConnection(this.connectionString);
        this.connectionAdmin.Open();
    }

    /// <summary>
    /// Cleans up the test environment after running each test method.
    /// </summary>
    [TestCleanup]
    public new void TestCleanup()
    {
        base.TestCleanup();
        if (this.connectionAdmin.State == ConnectionState.Open)
        {
            this.connectionAdmin.Close();
        }
    }

    /// <summary>
    /// Test case for loading the ideal routes between Iasi and Bucuresti.
    /// </summary>
    [TestMethod]
    public void Test_Recursive_Ias_Buc_test_passed()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");
        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var cmd_tr_1 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
            ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'bus') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_1.ExecuteNonQuery();
        var route_1_id = cmd_tr_1.ExecuteScalar();


        var cmd_tr_2 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'CLJ'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'train') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_2.ExecuteNonQuery();
        var route_2_id = cmd_tr_2.ExecuteScalar();


        var cmd_tr_3 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'SIB'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'train') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_3.ExecuteNonQuery();
        var route_3_id = cmd_tr_3.ExecuteScalar();

        var cmd_rs = new NpgsqlCommand(@"
            INSERT INTO route_schedules (route_id, departure_time, arrival_time, price, frequency, valid_from, valid_until)
            VALUES
            (@route_id1, '2024-06-01 06:00:00', '2024-06-01 10:00:00', 45, '7 days'::interval, '2024-06-01', '2024-12-31'),
            (@route_id2, '2024-06-01 09:00:00', '2024-06-01 13:00:00', 75, '7 days'::interval, '2024-06-01', '2024-12-31'),
            (@route_id3, '2024-06-01 15:00:00', '2024-06-01 19:00:00', 50, '7 days'::interval, '2024-06-01', '2024-12-31')",
            this.connectionAdmin);
        cmd_rs.Parameters.AddWithValue("@route_id1", route_1_id!);
        cmd_rs.Parameters.AddWithValue("@route_id2", route_2_id!);
        cmd_rs.Parameters.AddWithValue("@route_id3", route_3_id!);
        cmd_rs.ExecuteNonQuery();

        var routes = ds.LoadIdealRoutes("Iasi", "Bucuresti");

        var expectedRoutes = new List<RouteInfo>
        {
            new()
            {
                RouteIds = route_1_id!.ToString()!,
                DepartureLocation = "Iasi",
                ArrivalLocation = "Bucuresti",
                NumberOfStops = 0,
                TotalPrice = 45,
                TotalHours = 4,
                WeightedScore = 49,
                DepartureTime = new DateTime(2024, 6, 1, 6, 0, 0),
                ArrivalTime = new DateTime(2024, 6, 1, 10, 0, 0)
            }
        };

        // Use assertions to verify the results
        Assert.AreEqual(expectedRoutes.Count, routes.Count);
        for (var i = 0; i < expectedRoutes.Count; i++)
        {
            Assert.AreEqual(expectedRoutes[i].RouteIds, routes[i].RouteIds);
            Assert.AreEqual(expectedRoutes[i].DepartureLocation, routes[i].DepartureLocation);
            Assert.AreEqual(expectedRoutes[i].ArrivalLocation, routes[i].ArrivalLocation);
            Assert.AreEqual(expectedRoutes[i].NumberOfStops, routes[i].NumberOfStops);
            Assert.AreEqual(expectedRoutes[i].TotalPrice, routes[i].TotalPrice);
            Assert.AreEqual(expectedRoutes[i].TotalHours, routes[i].TotalHours, 0.01);
            Assert.AreEqual(expectedRoutes[i].WeightedScore, routes[i].WeightedScore, 0.01);
            Assert.AreEqual(expectedRoutes[i].DepartureTime, routes[i].DepartureTime);
            Assert.AreEqual(expectedRoutes[i].ArrivalTime, routes[i].ArrivalTime);
        }
    }

    /// <summary>
    /// Test case for loading the ideal routes between Iasi and Chisinau(which does not exist).
    /// </summary>
    [TestMethod]
    public void Test_Recursive_Chi_Buc_chi_does_not_exist_test_failed()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");
        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var cmd_tr_1 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
            ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'bus') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_1.ExecuteNonQuery();
        var route_1_id = cmd_tr_1.ExecuteScalar();


        var cmd_tr_2 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'CLJ'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'train') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_2.ExecuteNonQuery();
        var route_2_id = cmd_tr_2.ExecuteScalar();


        var cmd_tr_3 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'SIB'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'train') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_3.ExecuteNonQuery();
        var route_3_id = cmd_tr_3.ExecuteScalar();

        var cmd_rs = new NpgsqlCommand(@"
            INSERT INTO route_schedules (route_id, departure_time, arrival_time, price, frequency, valid_from, valid_until)
            VALUES
            (@route_id1, '2024-06-01 06:00:00', '2024-06-01 10:00:00', 45, '7 days'::interval, '2024-06-01', '2024-12-31'),
            (@route_id2, '2024-06-01 09:00:00', '2024-06-01 13:00:00', 75, '7 days'::interval, '2024-06-01', '2024-12-31'),
            (@route_id3, '2024-06-01 15:00:00', '2024-06-01 19:00:00', 50, '7 days'::interval, '2024-06-01', '2024-12-31')",
            this.connectionAdmin);
        cmd_rs.Parameters.AddWithValue("@route_id1", route_1_id!);
        cmd_rs.Parameters.AddWithValue("@route_id2", route_2_id!);
        cmd_rs.Parameters.AddWithValue("@route_id3", route_3_id!);
        cmd_rs.ExecuteNonQuery();

        var routes = ds.LoadIdealRoutes("Chisinau", "Bucuresti");

        var expectedRoutes = new List<RouteInfo>
        {
            new()
            {
                RouteIds = route_1_id!.ToString()!,
                DepartureLocation = "Chisinau",
                ArrivalLocation = "Bucuresti",
                NumberOfStops = 0,
                TotalPrice = 46,
                TotalHours = 4,
                WeightedScore = 49,
                DepartureTime = new DateTime(2024, 6, 1, 6, 0, 0),
                ArrivalTime = new DateTime(2024, 6, 1, 10, 0, 0)
            }
        };

        // Use assertions to verify the results
        Assert.AreEqual(expectedRoutes.Count, routes.Count);
        for (var i = 0; i < expectedRoutes.Count; i++)
        {
            Assert.AreEqual(expectedRoutes[i].RouteIds, routes[i].RouteIds);
            Assert.AreEqual(expectedRoutes[i].DepartureLocation, routes[i].DepartureLocation);
            Assert.AreEqual(expectedRoutes[i].ArrivalLocation, routes[i].ArrivalLocation);
            Assert.AreEqual(expectedRoutes[i].NumberOfStops, routes[i].NumberOfStops);
            Assert.AreEqual(expectedRoutes[i].TotalPrice, routes[i].TotalPrice);
            Assert.AreEqual(expectedRoutes[i].TotalHours, routes[i].TotalHours, 0.01);
            Assert.AreEqual(expectedRoutes[i].WeightedScore, routes[i].WeightedScore, 0.01);
            Assert.AreEqual(expectedRoutes[i].DepartureTime, routes[i].DepartureTime);
            Assert.AreEqual(expectedRoutes[i].ArrivalTime, routes[i].ArrivalTime);
        }
    }

    /// <summary>
    /// Test case for loading the ideal routes between Iasi and Bucuresti and chooses the fastest one.
    /// </summary>
    [TestMethod]
    public void Test_Recursive_Ias_Buc_two_possibilities()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");
        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var cmd_tr_1 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
            ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'bus') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_1.ExecuteNonQuery();
        var route_1_id = cmd_tr_1.ExecuteScalar();


        var cmd_tr_2 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'airplane') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_2.ExecuteNonQuery();
        var route_2_id = cmd_tr_2.ExecuteScalar();

        var cmd_rs = new NpgsqlCommand(@"
            INSERT INTO route_schedules (route_id, departure_time, arrival_time, price, frequency, valid_from, valid_until)
            VALUES
            (@route_id1, '2024-06-01 08:00:00', '2024-06-01 10:00:00', 45, '7 days'::interval, '2024-06-01', '2024-12-31'),
            (@route_id2, '2024-06-01 08:00:00', '2024-06-01 13:00:00', 75, '14 days'::interval, '2024-06-01', '2024-12-31')",
            this.connectionAdmin);
        cmd_rs.Parameters.AddWithValue("@route_id1", route_1_id!);
        cmd_rs.Parameters.AddWithValue("@route_id2", route_2_id!);
        cmd_rs.ExecuteNonQuery();

        var routes = ds.LoadIdealRoutes("Iasi", "Bucuresti");

        var expectedRoutes = new List<RouteInfo>
        {
            new()
            {
                RouteIds = route_1_id!.ToString()!,
                DepartureLocation = "Iasi",
                ArrivalLocation = "Bucuresti",
                NumberOfStops = 0,
                TotalPrice = 45,
                TotalHours = 2,
                WeightedScore = 47,
                DepartureTime = new DateTime(2024, 6, 1, 8, 0, 0),
                ArrivalTime = new DateTime(2024, 6, 1, 10, 0, 0)
            }
        };

        // Use assertions to verify the results
        Assert.AreEqual(expectedRoutes.Count, routes.Count - 1);
        for (var i = 0; i < expectedRoutes.Count; i++)
        {
            Assert.AreEqual(expectedRoutes[i].RouteIds, routes[i].RouteIds);
            Assert.AreEqual(expectedRoutes[i].DepartureLocation, routes[i].DepartureLocation);
            Assert.AreEqual(expectedRoutes[i].ArrivalLocation, routes[i].ArrivalLocation);
            Assert.AreEqual(expectedRoutes[i].NumberOfStops, routes[i].NumberOfStops);
            Assert.AreEqual(expectedRoutes[i].TotalPrice, routes[i].TotalPrice);
            Assert.AreEqual(expectedRoutes[i].TotalHours, routes[i].TotalHours, 0.01);
            Assert.AreEqual(expectedRoutes[i].WeightedScore, routes[i].WeightedScore, 0.01);
            Assert.AreEqual(expectedRoutes[i].DepartureTime, routes[i].DepartureTime);
            Assert.AreEqual(expectedRoutes[i].ArrivalTime, routes[i].ArrivalTime);
        }
    }

    /// <summary>
    /// Test case for loading the ideal routes between Iasi and Bucuresti with the arrival before the departure.
    /// </summary>
    [TestMethod]
    public void Test_Recursive_arrival_before_the_departure()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");
        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var cmd_tr_1 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
            ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'bus') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_1.ExecuteNonQuery();
        var route_1_id = cmd_tr_1.ExecuteScalar();


        var cmd_tr_2 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'CLJ'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'train') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_2.ExecuteNonQuery();
        var route_2_id = cmd_tr_2.ExecuteScalar();


        var cmd_tr_3 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'SIB'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'train') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_3.ExecuteNonQuery();
        var route_3_id = cmd_tr_3.ExecuteScalar();

        var cmd_rs = new NpgsqlCommand(@"
            INSERT INTO route_schedules (route_id, departure_time, arrival_time, price, frequency, valid_from, valid_until)
            VALUES
            (@route_id1, '2024-06-02 06:00:00', '2024-06-01 10:00:00', 45, '7 days'::interval, '2024-06-01', '2024-12-31'),
            (@route_id2, '2024-06-02 09:00:00', '2024-06-01 13:00:00', 75, '7 days'::interval, '2024-06-01', '2024-12-31'),
            (@route_id3, '2024-06-02 15:00:00', '2024-06-01 19:00:00', 50, '7 days'::interval, '2024-06-01', '2024-12-31')",
            this.connectionAdmin);
        cmd_rs.Parameters.AddWithValue("@route_id1", route_1_id!);
        cmd_rs.Parameters.AddWithValue("@route_id2", route_2_id!);
        cmd_rs.Parameters.AddWithValue("@route_id3", route_3_id!);
        cmd_rs.ExecuteNonQuery();

        var routes = ds.LoadIdealRoutes("Iasi", "Bucuresti");

        var expectedRoutes = new List<RouteInfo>
        {
            new()
            {
                RouteIds = route_1_id!.ToString()!,
                DepartureLocation = "Iasi",
                ArrivalLocation = "Bucuresti",
                NumberOfStops = 0,
                TotalPrice = 45,
                TotalHours = 4,
                WeightedScore = 49,
                DepartureTime = new DateTime(2024, 6, 1, 6, 0, 0),
                ArrivalTime = new DateTime(2024, 6, 1, 10, 0, 0)
            }
        };

        // Use assertions to verify the results
        Assert.AreEqual(expectedRoutes.Count, routes.Count);
        for (var i = 0; i < expectedRoutes.Count; i++)
        {
            Assert.AreEqual(expectedRoutes[i].RouteIds, routes[i].RouteIds);
            Assert.AreEqual(expectedRoutes[i].DepartureLocation, routes[i].DepartureLocation);
            Assert.AreEqual(expectedRoutes[i].ArrivalLocation, routes[i].ArrivalLocation);
            Assert.AreEqual(expectedRoutes[i].NumberOfStops, routes[i].NumberOfStops);
            Assert.AreEqual(expectedRoutes[i].TotalPrice, routes[i].TotalPrice);
            Assert.AreEqual(expectedRoutes[i].TotalHours, routes[i].TotalHours, 0.01);
            Assert.AreEqual(expectedRoutes[i].WeightedScore, routes[i].WeightedScore, 0.01);
            Assert.AreEqual(expectedRoutes[i].DepartureTime, routes[i].DepartureTime);
            Assert.AreEqual(expectedRoutes[i].ArrivalTime, routes[i].ArrivalTime);
        }
    }


    /// <summary>
    /// Test case for retrieving the location ID for Iasi.
    /// </summary>
    [TestMethod]
    public void Test_GetLocationId_Ias()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");

        var locationName = "Iasi";

        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var locationId = ds.GetLocationId(locationName);

        Assert.AreNotEqual(0, locationId, $"Location ID for {locationName} should not be zero.");
    }

    /// <summary>
    /// Test case for retrieving the location ID for Chisinau(which does not exist).
    /// </summary>
    [TestMethod]
    public void Test_GetLocationId_Chi_not_existing()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");

        const string locationName = "Chisinau";

        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var locationId = ds.GetLocationId(locationName);

        Assert.AreNotEqual(0, locationId, $"Location ID for {locationName} should not be zero.");
    }

    /// <summary>
    /// Test case for retrieving the location ID for Iasi duplicate city.
    /// </summary>
    [TestMethod]
    public void Test_GetLocationId_duplicate_city()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");

        const string locationName = "Chisinau";

        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Iasi', 'IAS')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var locationId = ds.GetLocationId(locationName);

        Assert.AreNotEqual(0, locationId, $"Location ID for {locationName} should not be zero.");
    }

    /// <summary>
    /// Test case for inserting a travel route from Iasi to Bucuresti.
    /// </summary>
    [TestMethod]
    public void Test_InsertTravelRoute_Ias_Buc()
    {
        // Creează o instanță a serviciului de bază de date
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");

        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var departureId = ds.GetLocationId("Iasi"); // ID-ul locației de plecare
        var arrivalId = ds.GetLocationId("Bucuresti"); // ID-ul locației de sosire
        const string transportType = "bus"; // Tipul de transport

        // Inserează o rută de călătorie și obține ID-ul rutei
        var routeId = ds.InsertTravelRoute(departureId, arrivalId, transportType);

        // Verifică dacă ID-ul rutei returnat este nenul și mai mare sau egal cu 1
        Assert.IsTrue(routeId >= 1, "InsertTravelRoute nu returnează un ID de rută valid.");
    }

    /// <summary>
    /// Test case for loading the ideal routes between Iasi and Bucuresti with a bicycle.
    /// </summary>
    [TestMethod]
    public void Test_InsertTravelRoute_wrong_type_of_transport()
    {
        // Creează o instanță a serviciului de bază de date
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");

        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var departureId = ds.GetLocationId("Iasi"); // ID-ul locației de plecare
        var arrivalId = ds.GetLocationId("Bucuresti"); // ID-ul locației de sosire
        const string transportType = "bicycle"; // Tipul de transport

        // Inserează o rută de călătorie și obține ID-ul rutei
        var routeId = ds.InsertTravelRoute(departureId, arrivalId, transportType);

        // Verifică dacă ID-ul rutei returnat este nenul și mai mare sau egal cu 1
        Assert.IsTrue(routeId >= 1, "InsertTravelRoute nu returnează un ID de rută valid.");
    }


    /// <summary>
    /// Test case for inserting a travel route from Iasi to Chisinau(which does not exist).
    /// </summary>
    [TestMethod]
    public void Test_InsertTravelRoute_Ias_Chi_not_existing()
    {
        // Creează o instanță a serviciului de bază de date
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");

        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var departureId = ds.GetLocationId("Iasi"); // ID-ul locației de plecare
        var arrivalId = ds.GetLocationId("Chisinau"); // ID-ul locației de sosire
        const string transportType = "train"; // Tipul de transport

        // Inserează o rută de călătorie și obține ID-ul rutei
        var routeId = ds.InsertTravelRoute(departureId, arrivalId, transportType);

        // Verifică dacă ID-ul rutei returnat este nenul și mai mare sau egal cu 1
        Assert.IsTrue(routeId >= 1, "InsertTravelRoute nu returnează un ID de rută valid.");
    }

    /// <summary>
    /// Test case for inserting a travel route from Iasi to Iasi.
    /// </summary>
    [TestMethod]
    public void Test_InsertTravelRoute_Ias_Ias_from_the_same_city_to_the_same_city()
    {
        // Creează o instanță a serviciului de bază de date
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");

        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Iasi', 'IAS')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var departureId = ds.GetLocationId("Iasi"); // ID-ul locației de plecare
        var arrivalId = ds.GetLocationId("Iasi"); // ID-ul locației de sosire
        const string transportType = "train"; // Tipul de transport

        // Inserează o rută de călătorie și obține ID-ul rutei
        var routeId = ds.InsertTravelRoute(departureId, arrivalId, transportType);

        // Verifică dacă ID-ul rutei returnat este nenul și mai mare sau egal cu 1
        Assert.IsTrue(routeId >= 1, "InsertTravelRoute nu returnează un ID de rută valid.");
    }

    /// <summary>
    /// Test case for loading all the locations.
    /// </summary>
    [TestMethod]
    public void Load_Locations_test_passed()
    {
        // Creează o instanță a serviciului de bază de date
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");

        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();
        var locations = ds.LoadLocations();
        var expectedLocations = new List<string>
        {
            "Iasi",
            "Bucuresti",
            "Cluj-Napoca",
            "Timisoara",
            "Sibiu",
            "Constanta",
            "Brasov"
        };
        CollectionAssert.AreEquivalent(expectedLocations, locations,
            "Loaded locations do not match expected locations.");
    }

    /// <summary>
    /// Test case for loading all the locations with an identical location.
    /// </summary>
    [TestMethod]
    public void Load_Locations_test_two_identical_locations()
    {
        // Creează o instanță a serviciului de bază de date
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");

        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Iasi', 'IAS'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();
        var locations = ds.LoadLocations();
        var expectedLocations = new List<string>
        {
            "Iasi",
            "Bucuresti",
            "Cluj-Napoca",
            "Timisoara",
            "Sibiu",
            "Constanta",
            "Brasov"
        };
        CollectionAssert.AreEquivalent(expectedLocations, locations,
            "Loaded locations do not match expected locations.");
    }

    /// <summary>
    /// Test case for finding a company name.
    /// </summary>
    [TestMethod]
    public void Test_FetchCompanyName_test_passed()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");

        using (var cmd = new NpgsqlCommand("CALL create_company('TravelE', '#FFFFFF', '#000000')",
                   this.connectionAdmin))
        {
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new NpgsqlCommand(
                   "CALL create_user('agent@travele.com', 'pass123', 'TravelE', 'boarding_agent_role', 'Agent TravelE')",
                   this.connectionAdmin))
        {
            cmd.ExecuteNonQuery();
        }

        // Definește un email de test
        const string userEmail = "agent@travele.com";

        // Apelăm funcția FetchCompanyName pentru a obține numele companiei asociate cu emailul dat
        var companyName = ds.FetchCompanyName(userEmail);

        // Verificăm dacă numele companiei returnat este cel așteptat
        Assert.AreEqual("TravelE", companyName,
            "FetchCompanyName nu returnează numele companiei corect pentru emailul dat.");
    }

    /// <summary>
    /// Test case for finding a company name which does not exist.
    /// </summary>
    [TestMethod]
    public void Test_FetchCompanyName_company_not_found()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");

        using (var cmd = new NpgsqlCommand("CALL create_company('TravelY', '#FFFFFF', '#000000')",
                   this.connectionAdmin))
        {
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new NpgsqlCommand(
                   "CALL create_user('agent@travely.com', 'pass123', 'TravelY', 'boarding_agent_role', 'Agent TravelY')",
                   this.connectionAdmin))
        {
            cmd.ExecuteNonQuery();
        }

        // Definește un email de test
        const string userEmail = "agent@travelz.com";

        // Apelăm funcția FetchCompanyName pentru a obține numele companiei asociate cu emailul dat
        var companyName = ds.FetchCompanyName(userEmail);

        // Verificăm dacă numele companiei returnat este cel așteptat
        Assert.AreEqual("TravelZ", companyName,
            "FetchCompanyName nu returnează numele companiei corect pentru emailul dat.");
    }

    /// <summary>
    /// This test method verifies the functionality of the InsertRouteSchedule method
    /// and subsequently checks if the LoadIdealRoutes method returns the expected routes.
    /// </summary>
    [TestMethod]
    public void InsertRouteSchedule_test_passed()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");
        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var cmd_tr_1 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
            ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'bus') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_1.ExecuteNonQuery();
        var route_1_id = cmd_tr_1.ExecuteScalar();

        var cmd_tr_2 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'airplane') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_2.ExecuteNonQuery();
        var route_2_id = cmd_tr_2.ExecuteScalar();
        ds.InsertRouteSchedule((int)route_1_id!, new DateTime(2024, 6, 1, 6, 0, 0), new DateTime(2024, 6, 1, 10, 0, 0),
            45, TimeSpan.FromDays(7), new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        ds.InsertRouteSchedule((int)route_2_id!, new DateTime(2024, 6, 1, 14, 0, 0),
            new DateTime(2024, 6, 1, 15, 30, 0),
            100, TimeSpan.FromDays(7), new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));

        // Încărcarea rutelor ideale de la Iasi la Bucuresti
        var routes = ds.LoadIdealRoutes("Iasi", "Bucuresti");

        // Definirea rutelor așteptate
        var expectedRoutes = new List<RouteInfo>
        {
            new()
            {
                RouteIds = route_1_id.ToString()!,
                DepartureLocation = "Iasi",
                ArrivalLocation = "Bucuresti",
                NumberOfStops = 0,
                TotalPrice = 45,
                TotalHours = 4,
                WeightedScore = 49, // Asumând calculul scorului
                DepartureTime = new DateTime(2024, 6, 1, 6, 0, 0),
                ArrivalTime = new DateTime(2024, 6, 1, 10, 0, 0)
            },
            new()
            {
                RouteIds = route_2_id.ToString()!,
                DepartureLocation = "Iasi",
                ArrivalLocation = "Bucuresti",
                NumberOfStops = 0,
                TotalPrice = 100,
                TotalHours = 1.5,
                WeightedScore = 101.5, // Asumând calculul scorului
                DepartureTime = new DateTime(2024, 6, 1, 14, 0, 0),
                ArrivalTime = new DateTime(2024, 6, 1, 15, 30, 0)
            }
        };

        // Utilizarea aserțiunilor pentru a verifica rezultatele
        Assert.AreEqual(expectedRoutes.Count, routes.Count);
        for (var i = 0; i < expectedRoutes.Count; i++)
        {
            Assert.AreEqual(expectedRoutes[i].RouteIds, routes[i].RouteIds);
            Assert.AreEqual(expectedRoutes[i].DepartureLocation, routes[i].DepartureLocation);
            Assert.AreEqual(expectedRoutes[i].ArrivalLocation, routes[i].ArrivalLocation);
            Assert.AreEqual(expectedRoutes[i].NumberOfStops, routes[i].NumberOfStops);
            Assert.AreEqual(expectedRoutes[i].TotalPrice, routes[i].TotalPrice);
            Assert.AreEqual(expectedRoutes[i].TotalHours, routes[i].TotalHours, 0.01);
            Assert.AreEqual(expectedRoutes[i].WeightedScore, routes[i].WeightedScore, 0.01);
            Assert.AreEqual(expectedRoutes[i].DepartureTime, routes[i].DepartureTime);
            Assert.AreEqual(expectedRoutes[i].ArrivalTime, routes[i].ArrivalTime);
        }
    }

    /// <summary>
    /// This test method verifies the functionality of the InsertRouteSchedule method
    /// and subsequently checks if the LoadIdealRoutes method returns the expected routes.
    /// with a route to itself.
    /// </summary>
    [TestMethod]
    public void InsertRouteSchedule_test_from_the_city_to_itself()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");
        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var cmd_tr_1 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
            ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'bus') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_1.ExecuteNonQuery();
        var route_1_id = cmd_tr_1.ExecuteScalar();


        var cmd_tr_2 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'airplane') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_2.ExecuteNonQuery();
        var route_2_id = cmd_tr_2.ExecuteScalar();
        ds.InsertRouteSchedule((int)route_1_id!, new DateTime(2024, 6, 1, 6, 0, 0), new DateTime(2024, 6, 1, 10, 0, 0),
            45, TimeSpan.FromDays(7), new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        ds.InsertRouteSchedule((int)route_2_id!, new DateTime(2024, 6, 1, 14, 0, 0),
            new DateTime(2024, 6, 1, 15, 30, 0),
            100, TimeSpan.FromDays(7), new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));

        // Încărcarea rutelor ideale de la Iasi la Bucuresti
        var routes = ds.LoadIdealRoutes("Iasi", "Iasi");

        // Definirea rutelor așteptate
        var expectedRoutes = new List<RouteInfo>
        {
            new()
            {
                RouteIds = route_1_id.ToString()!,
                DepartureLocation = "Iasi",
                ArrivalLocation = "Bucuresti",
                NumberOfStops = 0,
                TotalPrice = 45,
                TotalHours = 4,
                WeightedScore = 49, // Asumând calculul scorului
                DepartureTime = new DateTime(2024, 6, 1, 6, 0, 0),
                ArrivalTime = new DateTime(2024, 6, 1, 10, 0, 0)
            },
            new()
            {
                RouteIds = route_2_id.ToString()!,
                DepartureLocation = "Iasi",
                ArrivalLocation = "Bucuresti",
                NumberOfStops = 0,
                TotalPrice = 100,
                TotalHours = 1.5,
                WeightedScore = 101.5, // Asumând calculul scorului
                DepartureTime = new DateTime(2024, 6, 1, 14, 0, 0),
                ArrivalTime = new DateTime(2024, 6, 1, 15, 30, 0)
            }
        };

        // Utilizarea aserțiunilor pentru a verifica rezultatele
        Assert.AreEqual(expectedRoutes.Count, routes.Count);
        for (var i = 0; i < expectedRoutes.Count; i++)
        {
            Assert.AreEqual(expectedRoutes[i].RouteIds, routes[i].RouteIds);
            Assert.AreEqual(expectedRoutes[i].DepartureLocation, routes[i].DepartureLocation);
            Assert.AreEqual(expectedRoutes[i].ArrivalLocation, routes[i].ArrivalLocation);
            Assert.AreEqual(expectedRoutes[i].NumberOfStops, routes[i].NumberOfStops);
            Assert.AreEqual(expectedRoutes[i].TotalPrice, routes[i].TotalPrice);
            Assert.AreEqual(expectedRoutes[i].TotalHours, routes[i].TotalHours, 0.01);
            Assert.AreEqual(expectedRoutes[i].WeightedScore, routes[i].WeightedScore, 0.01);
            Assert.AreEqual(expectedRoutes[i].DepartureTime, routes[i].DepartureTime);
            Assert.AreEqual(expectedRoutes[i].ArrivalTime, routes[i].ArrivalTime);
        }
    }

    /// <summary>
    /// This test method verifies the functionality of the InsertRouteSchedule method
    /// and subsequently checks if the LoadIdealRoutes method returns the expected routes.
    /// with the arrival before the departure.
    /// </summary>
    [TestMethod]
    public void InsertRouteSchedule_test_the_arrival_is_before_the_departure()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");
        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var cmd_tr_1 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
            ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'bus') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_1.ExecuteNonQuery();
        var route_1_id = cmd_tr_1.ExecuteScalar();


        var cmd_tr_2 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'airplane') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_2.ExecuteNonQuery();
        var route_2_id = cmd_tr_2.ExecuteScalar();
        ds.InsertRouteSchedule((int)route_1_id!, new DateTime(2024, 6, 2, 6, 0, 0), new DateTime(2024, 6, 1, 10, 0, 0),
            45, TimeSpan.FromDays(7), new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        ds.InsertRouteSchedule((int)route_2_id!, new DateTime(2024, 6, 3, 14, 0, 0),
            new DateTime(2024, 6, 1, 15, 30, 0),
            100, TimeSpan.FromDays(7), new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));

        // Încărcarea rutelor ideale de la Iasi la Bucuresti
        var routes = ds.LoadIdealRoutes("Iasi", "Bucuresti");

        // Definirea rutelor așteptate
        var expectedRoutes = new List<RouteInfo>
        {
            new()
            {
                RouteIds = route_1_id.ToString()!,
                DepartureLocation = "Iasi",
                ArrivalLocation = "Bucuresti",
                NumberOfStops = 0,
                TotalPrice = 45,
                TotalHours = 4,
                WeightedScore = 49, // Asumând calculul scorului
                DepartureTime = new DateTime(2024, 6, 1, 6, 0, 0),
                ArrivalTime = new DateTime(2024, 6, 1, 10, 0, 0)
            },
            new()
            {
                RouteIds = route_2_id.ToString()!,
                DepartureLocation = "Iasi",
                ArrivalLocation = "Bucuresti",
                NumberOfStops = 0,
                TotalPrice = 100,
                TotalHours = 1.5,
                WeightedScore = 101.5, // Asumând calculul scorului
                DepartureTime = new DateTime(2024, 6, 1, 14, 0, 0),
                ArrivalTime = new DateTime(2024, 6, 1, 15, 30, 0)
            }
        };

        // Utilizarea aserțiunilor pentru a verifica rezultatele
        Assert.AreEqual(expectedRoutes.Count, routes.Count);
        for (var i = 0; i < expectedRoutes.Count; i++)
        {
            Assert.AreEqual(expectedRoutes[i].RouteIds, routes[i].RouteIds);
            Assert.AreEqual(expectedRoutes[i].DepartureLocation, routes[i].DepartureLocation);
            Assert.AreEqual(expectedRoutes[i].ArrivalLocation, routes[i].ArrivalLocation);
            Assert.AreEqual(expectedRoutes[i].NumberOfStops, routes[i].NumberOfStops);
            Assert.AreEqual(expectedRoutes[i].TotalPrice, routes[i].TotalPrice);
            Assert.AreEqual(expectedRoutes[i].TotalHours, routes[i].TotalHours, 0.01);
            Assert.AreEqual(expectedRoutes[i].WeightedScore, routes[i].WeightedScore, 0.01);
            Assert.AreEqual(expectedRoutes[i].DepartureTime, routes[i].DepartureTime);
            Assert.AreEqual(expectedRoutes[i].ArrivalTime, routes[i].ArrivalTime);
        }
    }

    /// <summary>
    /// This test method verifies the functionality of the LoadTrips method
    /// by setting up a database with locations, travel routes, and route schedules,
    /// and then calling the LoadTrips method for a specific location.
    /// </summary>
    [TestMethod]
    public void LoadTrips_test_passed()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");
        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var cmd_tr_1 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
            ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'bus') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_1.ExecuteNonQuery();
        var route_1_id = cmd_tr_1.ExecuteScalar();


        var cmd_tr_2 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'CLJ'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'train') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_2.ExecuteNonQuery();
        var route_2_id = cmd_tr_2.ExecuteScalar();


        var cmd_tr_3 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'SIB'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'train') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_3.ExecuteNonQuery();
        var route_3_id = cmd_tr_3.ExecuteScalar();

        var cmd_rs = new NpgsqlCommand(@"
            INSERT INTO route_schedules (route_id, departure_time, arrival_time, price, frequency, valid_from, valid_until)
            VALUES
            (@route_id1, '2024-06-02 06:00:00', '2024-06-01 10:00:00', 45, '7 days'::interval, '2024-06-01', '2024-12-31'),
            (@route_id2, '2024-06-02 09:00:00', '2024-06-01 13:00:00', 75, '7 days'::interval, '2024-06-01', '2024-12-31'),
            (@route_id3, '2024-06-02 15:00:00', '2024-06-01 19:00:00', 50, '7 days'::interval, '2024-06-01', '2024-12-31')",
            this.connectionAdmin);
        cmd_rs.Parameters.AddWithValue("@route_id1", route_1_id!);
        cmd_rs.Parameters.AddWithValue("@route_id2", route_2_id!);
        cmd_rs.Parameters.AddWithValue("@route_id3", route_3_id!);
        cmd_rs.ExecuteNonQuery();
        const string location = "Iasi";
        var trips = ds.LoadTrips(location);
        Assert.IsNotNull(trips, "Trips list is null.");
    }

    /// <summary>
    /// This test method verifies the functionality of the LoadTrips method
    /// by setting up a database with locations, travel routes, and route schedules,
    /// and then calling the LoadTrips method for a specific location which does not exist.
    /// </summary>
    [TestMethod]
    public void LoadTrips_test_failed_city_does_not_exist()
    {
        var ds = new DatabaseService("Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433");
        var cmd_l = new NpgsqlCommand(@"
            INSERT INTO locations (full_name, abbreviation)
            VALUES
            ('Iasi', 'IAS'),
            ('Bucuresti', 'BUC'),
            ('Cluj-Napoca', 'CLJ'),
            ('Timisoara', 'TIM'),
            ('Sibiu', 'SIB'),
            ('Constanta', 'CST'),
            ('Brasov', 'BSV')", this.connectionAdmin);

        cmd_l.ExecuteNonQuery();

        var cmd_tr_1 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
            ((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'bus') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_1.ExecuteNonQuery();
        var route_1_id = cmd_tr_1.ExecuteScalar();


        var cmd_tr_2 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'CLJ'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'train') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_2.ExecuteNonQuery();
        var route_2_id = cmd_tr_2.ExecuteScalar();


        var cmd_tr_3 = new NpgsqlCommand(@"
            INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type)
            VALUES
             ((SELECT location_id FROM locations WHERE abbreviation = 'SIB'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'train') RETURNING route_id",
            this.connectionAdmin);
        cmd_tr_3.ExecuteNonQuery();
        var route_3_id = cmd_tr_3.ExecuteScalar();

        var cmd_rs = new NpgsqlCommand(@"
            INSERT INTO route_schedules (route_id, departure_time, arrival_time, price, frequency, valid_from, valid_until)
            VALUES
            (@route_id1, '2024-06-02 06:00:00', '2024-06-01 10:00:00', 45, '7 days'::interval, '2024-06-01', '2024-12-31'),
            (@route_id2, '2024-06-02 09:00:00', '2024-06-01 13:00:00', 75, '7 days'::interval, '2024-06-01', '2024-12-31'),
            (@route_id3, '2024-06-02 15:00:00', '2024-06-01 19:00:00', 50, '7 days'::interval, '2024-06-01', '2024-12-31')",
            this.connectionAdmin);
        cmd_rs.Parameters.AddWithValue("@route_id1", route_1_id!);
        cmd_rs.Parameters.AddWithValue("@route_id2", route_2_id!);
        cmd_rs.Parameters.AddWithValue("@route_id3", route_3_id!);
        cmd_rs.ExecuteNonQuery();
        var location = "Chisinau";
        var trips = ds.LoadTrips(location);
        var expected_trips = new List<string> { "06:00 -> Bucuresti", "09:00 -> Bucuresti", "15:00 -> Bucuresti" };
        CollectionAssert.AreEquivalent(trips, expected_trips);
    }
}
