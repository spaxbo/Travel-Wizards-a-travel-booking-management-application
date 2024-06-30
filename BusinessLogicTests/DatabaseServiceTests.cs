/********************************************************************************************************
 * File: DatabaseServiceTests.cs
 * Author: Diaconu Rareș-George
 * Created: 22.05.2024
 * Last Modified: 22.05.2024
 * Description: This file contains the implementation of the DatabaseServiceTests class which is the base class for
 *              the DataBaseTests class and in which we initialize and cleanup the tables in the database.
 *
 * Purpose:
 * The purpose of this class is to manage the data in the tables of the database.
 *
 * Notes:
 * - The connection to the database to be done properly
 * - Ensure the PostgreSQL server is running and accessible.
 *
 ********************************************************************************************************/

namespace TravelWizards.Data.Tests;

using Npgsql;

/// <summary>
/// The base class for DataBaseTests
/// </summary>
public class DatabaseServiceTests
{
    private string connectionString = "";
    private NpgsqlConnection connection = null!;

    /// <summary>
    /// Deletes all data from the specified table.
    /// </summary>
    /// <param name="table"></param>
    private void DeleteData(string table)
    {
        var query = $"DELETE FROM {table}";
        var cmd = new NpgsqlCommand(query, this.connection);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Initializes the test environment before running each test method.
    /// </summary>
    protected void TestInitialize()
    {
        this.connectionString =
            "Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433";
        this.connection = new NpgsqlConnection(this.connectionString);
        this.connection.Open();
        string[] targetTables =
        [
            "booking_details", "booking_segments", "bookings", "route_schedules", "travel_routes", "users", "companies",
            "locations"
        ];
        foreach (var table in targetTables)
        {
            this.DeleteData(table);
        }
    }

    /// <summary>
    /// Cleans up the test environment after running each test method.
    /// </summary>
    protected void TestCleanup()
    {
        this.connectionString =
            "Host=localhost;Database=postgres;Username=postgres;Password=postgres;Port=5433";
        this.connection = new NpgsqlConnection(this.connectionString);
        this.connection.Open();
        string[] targetTables =
        [
            "booking_details", "booking_segments", "bookings", "route_schedules", "travel_routes", "users", "companies",
            "locations"
        ];
        foreach (var table in targetTables)
        {
            this.DeleteData(table);
        }
    }
}
