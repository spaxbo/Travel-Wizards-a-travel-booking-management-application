/********************************************************************************************************
 * File: DatabaseService.cs
 * Author: Curuliuc Cosmin-Stefan
 * Created: 22.05.2024
 * Last Modified: 22.05.2024
 * Description: This file contains the implementation of the DatabaseService class which provides methods
 *              to interact with the PostgreSQL database for managing travel routes, reservations, and boarding status.
 *
 * Purpose:
 * The purpose of this class is to encapsulate all database operations related to travel management,
 * including loading locations, trips, and passengers, as well as handling reservations and route schedules.
 *
 * Notes:
 * - Ensure the PostgreSQL server is running and accessible.
 * - Database schema should include relevant tables such as 'locations', 'travel_routes', 'route_schedules',
 *   'bookings', and 'booking_details'.
 *
 ********************************************************************************************************/

namespace TravelWizards.Data;

using Npgsql;
using System.Globalization;

/// <summary>
/// Provides methods to interact with the PostgreSQL database for managing travel routes, reservations, and boarding status.
/// </summary>
public class DatabaseService
{
    #region Fields

    /// <summary>
    /// The connection string to the PostgreSQL database.
    /// </summary>
    public string ConnectionString { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseService"/> class.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    public DatabaseService(string connectionString) => this.ConnectionString = connectionString;

    #endregion

    #region Methods

    /// <summary>
    /// Toggles the boarding status of the specified passenger and updates the database.
    /// </summary>
    /// <param name="passenger">The passenger whose boarding status is to be toggled.</param>
    /// <exception cref="NpgsqlException">Thrown if there is a database error</exception>
    public void ToggleBoardedStatus(Passenger passenger)
    {
        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();
        var cmd = new NpgsqlCommand(@"
                UPDATE booking_details
                SET boarded = @Boarded
                WHERE booking_detail_id = @BookingDetailId", conn);

        passenger.HasBoarded = !passenger.HasBoarded;
        cmd.Parameters.AddWithValue("@Boarded", passenger.HasBoarded);
        cmd.Parameters.AddWithValue("@BookingDetailId", passenger.BookingDetailId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Loads location names from the database.
    /// </summary>
    /// <returns>A list of location names.</returns>
    /// <exception cref="NpgsqlException">Thrown if there is a database error</exception>
    public List<string> LoadLocations()
    {
        var locations = new List<string>();

        using var conn = new NpgsqlConnection(this.ConnectionString);

        conn.Open();
        var cmd = new NpgsqlCommand("SELECT full_name FROM locations", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            locations.Add(reader.GetString(0));
        }


        return locations;
    }

    /// <summary>
    /// Loads available trips for the selected location from the database.
    /// </summary>
    /// <param name = "location" > The selected location.</param>
    /// <returns>A list of available trips.</returns>
    /// <exception cref="NpgsqlException">Thrown if there is a database error</exception>
    public List<string> LoadTrips(string location)
    {
        var trips = new List<string>();

        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();
        var query = @"
                    SELECT
                        rs.departure_time,
                        l2.full_name AS arrival_location
                    FROM
                        route_schedules rs
                    JOIN
                        travel_routes tr ON rs.route_id = tr.route_id
                    JOIN
                        locations l1 ON tr.departure_location_id = l1.location_id
                    JOIN
                        locations l2 ON tr.arrival_location_id = l2.location_id
                    WHERE
                        l1.full_name = @Location
                        AND rs.departure_time >= NOW()
                        AND rs.departure_time <= NOW() + INTERVAL '1 day'
                    ORDER BY
                        rs.departure_time";

        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Location", location);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var departureTime = reader.GetDateTime(0).ToString("HH:mm", CultureInfo.InvariantCulture);
            var arrivalLocation = reader.GetString(1);
            trips.Add($"{departureTime} -> {arrivalLocation}");
        }


        return trips;
    }

    /// <summary>
    /// Loads passengers assigned to the selected trip from the database.
    /// </summary>
    /// <param name = "currentLocation" > The current location.</param>
    /// <returns>A list of passengers.</returns>
    /// <exception cref="NpgsqlException">Thrown if there is a database error</exception>
    public List<Passenger> LoadPassengers(string currentLocation)
    {
        var passengers = new List<Passenger>();

        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();
        var cmd = new NpgsqlCommand(@"
                        SELECT bd.booking_detail_id, b.first_name || ' ' || b.last_name AS name, bd.boarded
                        FROM booking_details bd
                        JOIN bookings b ON bd.booking_id = b.booking_id
                        JOIN route_schedules rs ON bd.schedule_id = rs.schedule_id
                        JOIN travel_routes tr ON rs.route_id = tr.route_id
                        JOIN locations l1 ON tr.departure_location_id = l1.location_id
                        JOIN locations l2 ON tr.arrival_location_id = l2.location_id
                        WHERE l1.full_name = @CurrentLocation
                        AND rs.departure_time >= NOW()
                        AND rs.departure_time <= NOW() + INTERVAL '1 week'", conn);

        cmd.Parameters.AddWithValue("@CurrentLocation", currentLocation);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            passengers.Add(new Passenger
            {
                BookingDetailId = reader.GetInt32(0),
                Name = reader.GetString(1),
                HasBoarded = reader.GetBoolean(2)
            });
        }


        return passengers;
    }

    /// <summary>
    /// Gets the ID of a location based on its name.
    /// </summary>
    /// <param name = "locationName" > The name of the location.</param>
    /// <returns>The ID of the location.</returns>
    public int GetLocationId(string locationName)
    {
        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();
        var cmd = new NpgsqlCommand("SELECT location_id FROM locations WHERE full_name = @LocationName", conn);
        cmd.Parameters.AddWithValue("@LocationName", locationName);
        var locationId = cmd.ExecuteScalar();
        return locationId != null
            ? Convert.ToInt32(locationId, CultureInfo.InvariantCulture)
            : 0;
    }

    /// <summary>
    /// Fetches and displays the company name associated with the user.
    /// </summary>
    /// <param name = "userEmail" > The email of the user.</param>
    /// <returns>The company name, or "Company name not found"</returns>
    public string FetchCompanyName(string userEmail)
    {
        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();

        const string query = """
                             SELECT c.company_name FROM companies c
                                                   JOIN users u ON c.company_id = u.company_id
                                                   WHERE u.email = @Email
                             """;

        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Email", userEmail);

        var companyName = cmd.ExecuteScalar()?.ToString();
        return companyName ?? "Company name not found";
    }

    /// <summary>
    /// Inserts a new travel route into the database.
    /// </summary>
    /// <param name = "departureId" > The departure location ID.</param>
    /// <param name = "arrivalId" > The arrival location ID.</param>
    /// <param name = "transportType" > The transport type.</param>
    /// <returns>The ID of the inserted route.</returns>
    public int InsertTravelRoute(int departureId, int arrivalId, string transportType)
    {
        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();

        var cmd = new NpgsqlCommand(
            "INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type) VALUES (@depId, @arrId, @type) RETURNING route_id;",
            conn);
        cmd.Parameters.AddWithValue("@depId", departureId);
        cmd.Parameters.AddWithValue("@arrId", arrivalId);
        cmd.Parameters.AddWithValue("@type", transportType);
        return (int)cmd.ExecuteScalar()!;
    }

    /// <summary>
    /// Inserts a new route schedule into the database.
    /// </summary>
    /// <param name = "routeId" > The route ID.</param>
    /// <param name = "departureTime" > The departure time.</param>
    /// <param name = "arrivalTime" > The arrival time.</param>
    /// <param name = "price" > The price.</param>
    /// <param name = "frequency" > The frequency.</param>
    /// <param name = "validFrom" > The valid from date.</param>
    /// <param name = "validUntil" > The valid until date.</param>
    public void InsertRouteSchedule(int routeId, DateTime departureTime, DateTime arrivalTime, int price,
        TimeSpan frequency, DateTime validFrom, DateTime validUntil)
    {
        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();

        var cmd = new NpgsqlCommand(
            """
            INSERT INTO route_schedules (route_id, departure_time, arrival_time, price, frequency, valid_from, valid_until)
            VALUES (@routeId, @departure, @arrival, @price, @frequency, @validFrom, @validUntil);
            """, conn);

        cmd.Parameters.AddWithValue("@routeId", routeId);
        cmd.Parameters.AddWithValue("@departure", departureTime);
        cmd.Parameters.AddWithValue("@arrival", arrivalTime);
        cmd.Parameters.AddWithValue("@price", price);
        cmd.Parameters.Add("@frequency", NpgsqlTypes.NpgsqlDbType.Interval).Value = frequency;
        cmd.Parameters.AddWithValue("@validFrom", validFrom);
        cmd.Parameters.AddWithValue("@validUntil", validUntil);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Updates the price of the given route in the database.
    /// </summary>
    /// <param name = "route" > The route with the updated price.</param>
    public void UpdateRoutePrice(Route route)
    {
        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();

        const string query = @"
            UPDATE route_schedules
            SET price = @Price
            WHERE route_id = (
                SELECT route_id FROM travel_routes
                WHERE departure_location_id = (
                    SELECT location_id FROM locations WHERE abbreviation = @Departure
                ) AND arrival_location_id = (
                    SELECT location_id FROM locations WHERE abbreviation = @Arrival
                )
            ) AND departure_time::time = @DepartureTime AND arrival_time::time = @ArrivalTime";

        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Price", route.Price);
        cmd.Parameters.AddWithValue("@Departure", route.Departure);
        cmd.Parameters.AddWithValue("@Arrival", route.Arrival);
        cmd.Parameters.AddWithValue("@DepartureTime", route.DepartureTime.ToTimeSpan());
        cmd.Parameters.AddWithValue("@ArrivalTime", route.ArrivalTime.ToTimeSpan());

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Deletes the given routes from the database.
    /// </summary>
    /// <param name = "routes" > The list of routes to delete.</param>
    public void DeleteShownRoutes(List<Route> routes)
    {
        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();
        using var tran = conn.BeginTransaction();

        foreach (var route in routes)
        {
            const string query = @"
                DELETE FROM route_schedules
                WHERE route_id = (
                    SELECT route_id FROM travel_routes
                    WHERE departure_location_id = (
                        SELECT location_id FROM locations WHERE abbreviation = @Departure
                    ) AND arrival_location_id = (
                        SELECT location_id FROM locations WHERE abbreviation = @Arrival
                    )
                ) AND departure_time::time = @DepartureTime AND arrival_time::time = @ArrivalTime";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Departure", route.Departure);
            cmd.Parameters.AddWithValue("@Arrival", route.Arrival);
            cmd.Parameters.AddWithValue("@DepartureTime", route.DepartureTime.ToTimeSpan());
            cmd.Parameters.AddWithValue("@ArrivalTime", route.ArrivalTime.ToTimeSpan());

            cmd.ExecuteNonQuery();
        }

        tran.Commit();
    }

    /// <summary>
    /// Deletes the given route from the database.
    /// </summary>
    /// <param name = "route" > The route to delete.</param>
    public void DeleteRoute(Route route)
    {
        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();

        const string query = @"
            DELETE FROM route_schedules
            WHERE route_id = (
                SELECT route_id FROM travel_routes
                WHERE departure_location_id = (
                    SELECT location_id FROM locations WHERE abbreviation = @Departure
                ) AND arrival_location_id = (
                    SELECT location_id FROM locations WHERE abbreviation = @Arrival
                )
            ) AND departure_time::time = @DepartureTime AND arrival_time::time = @ArrivalTime";

        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Departure", route.Departure);
        cmd.Parameters.AddWithValue("@Arrival", route.Arrival);
        cmd.Parameters.AddWithValue("@DepartureTime", route.DepartureTime.ToTimeSpan());
        cmd.Parameters.AddWithValue("@ArrivalTime", route.ArrivalTime.ToTimeSpan());

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Loads optimal routes from the database based on the selected departure and arrival locations.
    /// </summary>
    /// <param name = "departure" > The departure location.</param>
    /// <param name = "arrival" > The arrival location.</param>
    /// <returns>A list of optimal routes.</returns>
    public List<RouteInfo> LoadIdealRoutes(string departure, string arrival)
    {
        var routes = new List<RouteInfo>();
        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();
        const string query = @"
                WITH RECURSIVE RouteConnections AS (
                   SELECT
                       route_id,
                       departure_location_id,
                       arrival_location_id,
                       ARRAY[route_id] AS route_path,
                       1 AS hop_count,
                       price AS total_price,
                       EXTRACT(EPOCH FROM (arrival_time - departure_time)) / 3600 AS total_hours,
                       price + (EXTRACT(EPOCH FROM (arrival_time - departure_time)) / 3600) AS weighted_score
                   FROM travel_routes
                   JOIN route_schedules USING (route_id)
                   WHERE departure_location_id = (SELECT location_id FROM locations WHERE full_name = @Departure)

                   UNION ALL

                   SELECT
                       r.route_id,
                       rc.departure_location_id,
                       r.arrival_location_id,
                       rc.route_path || r.route_id,
                       rc.hop_count + 1,
                       rc.total_price + rs.price,
                       rc.total_hours + EXTRACT(EPOCH FROM (rs.arrival_time - rs.departure_time)) / 3600,
                       rc.weighted_score + rs.price + (EXTRACT(EPOCH FROM (rs.arrival_time - rs.departure_time)) / 3600)
                   FROM travel_routes r
                   JOIN route_schedules rs ON r.route_id = rs.route_id
                   JOIN RouteConnections rc ON rc.arrival_location_id = r.departure_location_id
                   WHERE NOT r.route_id = ANY(rc.route_path)
                )
                SELECT
                    ARRAY_TO_STRING(route_path, ' -> ') AS RouteIds,
                    l1.full_name AS DepartureLocation,
                    l2.full_name AS ArrivalLocation,
                    hop_count - 1 AS NumberOfStops,
                    ROUND(total_price::numeric, 2) AS TotalPrice,
                    ROUND(total_hours::numeric, 2) AS TotalHours,
                    ROUND(weighted_score::numeric, 2) AS WeightedScore,
                    rs.departure_time,
                    rs.arrival_time
                FROM RouteConnections
                JOIN locations l1 ON RouteConnections.departure_location_id = l1.location_id
                JOIN locations l2 ON RouteConnections.arrival_location_id = l2.location_id
                JOIN route_schedules rs ON rs.route_id = RouteConnections.route_id
                WHERE arrival_location_id = (SELECT location_id FROM locations WHERE full_name = @Arrival)
                ORDER BY total_price, weighted_score";

        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Departure", departure);
        cmd.Parameters.AddWithValue("@Arrival", arrival);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            routes.Add(new RouteInfo
            {
                RouteIds = reader.GetString(0),
                DepartureLocation = reader.GetString(1),
                ArrivalLocation = reader.GetString(2),
                NumberOfStops = reader.GetInt32(3),
                TotalPrice = reader.GetInt32(4),
                TotalHours = reader.GetDouble(5),
                WeightedScore = reader.GetDouble(6),
                DepartureTime = reader.GetDateTime(7),
                ArrivalTime = reader.GetDateTime(8)
            });
        }

        return routes;
    }

    /// <summary>
    /// Cancels the reservation for the given route.
    /// </summary>
    /// <param name="selectedReservation">The selected reservation.</param>
    /// <param name="firstName">The first name of the user.</param>
    /// <param name="lastName">The last name of the user.</param>
    public void CancelReservation(RouteInfo selectedReservation, string firstName, string lastName)
    {
        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();

        // Fetch BookingId and ScheduleId from the database based on route details
        var cmd = new NpgsqlCommand(@"
                SELECT bd.booking_id, bd.schedule_id
                FROM booking_details bd
                JOIN bookings b ON bd.booking_id = b.booking_id
                JOIN route_schedules rs ON bd.schedule_id = rs.schedule_id
                JOIN travel_routes tr ON rs.route_id = tr.route_id
                JOIN locations l1 ON tr.departure_location_id = l1.location_id
                JOIN locations l2 ON tr.arrival_location_id = l2.location_id
                WHERE b.first_name = @FirstName
                AND b.last_name = @LastName
                AND l1.full_name = @Departure
                AND l2.full_name = @Arrival
                AND rs.departure_time = @DepartureTime
                AND rs.arrival_time = @ArrivalTime
                AND rs.price = @Price
                LIMIT 1", conn);

        cmd.Parameters.AddWithValue("@FirstName", firstName);
        cmd.Parameters.AddWithValue("@LastName", lastName);
        cmd.Parameters.AddWithValue("@Departure", selectedReservation.DepartureLocation);
        cmd.Parameters.AddWithValue("@Arrival", selectedReservation.ArrivalLocation);
        cmd.Parameters.AddWithValue("@DepartureTime", selectedReservation.DepartureTime);
        cmd.Parameters.AddWithValue("@ArrivalTime", selectedReservation.ArrivalTime);
        cmd.Parameters.AddWithValue("@Price", selectedReservation.TotalPrice);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            throw new Exception("No matching reservation found.");
        }

        var bookingId = reader.GetInt32(0);
        var scheduleId = reader.GetInt32(1);

        reader.Close();

        // Delete booking details
        cmd = new NpgsqlCommand(
            "DELETE FROM booking_details WHERE booking_id = @BookingId AND schedule_id = @ScheduleId", conn);
        cmd.Parameters.AddWithValue("@BookingId", bookingId);
        cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
        cmd.ExecuteNonQuery();

        // Delete booking if no more details exist
        cmd = new NpgsqlCommand(
            "DELETE FROM bookings WHERE booking_id = @BookingId AND NOT EXISTS (SELECT 1 FROM booking_details WHERE booking_id = @BookingId)",
            conn);
        cmd.Parameters.AddWithValue("@BookingId", bookingId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Reserves the selected route for the user.
    /// </summary>
    /// <param name="selectedRoute">The selected route.</param>
    /// <param name="userName">The name of the user.</param>
    public void ReserveRoute(RouteInfo selectedRoute, string userName)
    {
        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();
        var transaction = conn.BeginTransaction();

        try
        {
            // Obține user_id și company_id pentru utilizatorul curent
            var userId = GetUserId(conn);
            var companyId = GetCompanyId(conn);

            var phoneNumber = "+0000000000";

            // Inserare o nouă rezervare cu user_id și company_id
            var cmd = new NpgsqlCommand(@"
                    INSERT INTO bookings (user_id, first_name, last_name, phone_number, company_id) 
                    VALUES (@UserId, @FirstName, @LastName, @Phone, @CompanyId) RETURNING booking_id", conn);

            var nameParts = userName.Split(' ');
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@Phone", phoneNumber);
            cmd.Parameters.AddWithValue("@CompanyId", companyId);
            var bookingId = cmd.ExecuteScalar();

            // Obține schedule_id pe baza detaliilor rutei
            cmd = new NpgsqlCommand(@"
                    SELECT schedule_id
                    FROM route_schedules
                    JOIN travel_routes ON route_schedules.route_id = travel_routes.route_id
                    WHERE travel_routes.departure_location_id = (SELECT location_id FROM locations WHERE full_name = @Departure)
                    AND travel_routes.arrival_location_id = (SELECT location_id FROM locations WHERE full_name = @Arrival)
                    AND route_schedules.departure_time = @DepartureTime
                    AND route_schedules.arrival_time = @ArrivalTime
                    AND route_schedules.price = @Price", conn);
            cmd.Parameters.AddWithValue("@Departure", selectedRoute.DepartureLocation);
            cmd.Parameters.AddWithValue("@Arrival", selectedRoute.ArrivalLocation);
            cmd.Parameters.AddWithValue("@DepartureTime", selectedRoute.DepartureTime);
            cmd.Parameters.AddWithValue("@ArrivalTime", selectedRoute.ArrivalTime);
            cmd.Parameters.AddWithValue("@Price", selectedRoute.TotalPrice);

            var scheduleId = cmd.ExecuteScalar();
            if (scheduleId == null)
            {
                throw new Exception("No schedule found for the selected route.");
            }

            // Inserare detalii rezervare cu schedule_id obținut
            cmd = new NpgsqlCommand("INSERT INTO booking_details (booking_id, schedule_id, boarded) VALUES (@BookingId, @ScheduleId, FALSE)", conn);
            cmd.Parameters.AddWithValue("@BookingId", bookingId);
            cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
            cmd.ExecuteNonQuery();

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Loads the user reservations from the database based on the user's name.
    /// </summary>
    /// <param name = "firstName" > The first name of the user.</param>
    /// <param name = "lastName" > The last name of the user.</param>
    /// <returns>A list of user reservations.</returns>
    public List<RouteInfo> LoadUserReservations(string firstName, string lastName)
    {
        var reservations = new List<RouteInfo>();
        using var conn = new NpgsqlConnection(this.ConnectionString);
        conn.Open();
        var cmd = new NpgsqlCommand(@"
                SELECT
                    l1.full_name AS Departure,
                    l2.full_name AS Arrival,
                    rs.departure_time AS DepartureTime,
                    rs.arrival_time AS ArrivalTime,
                    rs.price AS TotalPrice,
                    EXTRACT(EPOCH FROM rs.arrival_time - rs.departure_time) / 3600 AS TotalHours
                FROM
                    bookings b
                    JOIN booking_details bd ON b.booking_id = bd.booking_id
                    JOIN route_schedules rs ON bd.schedule_id = rs.schedule_id
                    JOIN travel_routes tr ON rs.route_id = tr.route_id
                    JOIN locations l1 ON tr.departure_location_id = l1.location_id
                    JOIN locations l2 ON tr.arrival_location_id = l2.location_id
                WHERE
                    b.first_name = @FirstName
                    AND b.last_name = @LastName", conn);

        cmd.Parameters.AddWithValue("@FirstName", firstName);
        cmd.Parameters.AddWithValue("@LastName", lastName);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            reservations.Add(new RouteInfo
            {
                DepartureLocation = reader.GetString(0),
                ArrivalLocation = reader.GetString(1),
                DepartureTime = reader.GetDateTime(2),
                ArrivalTime = reader.GetDateTime(3),
                TotalPrice = reader.GetInt32(4),
                TotalHours = reader.GetDouble(5),
                WeightedScore = 0,
                NumberOfStops = 0
            });
        }

        return reservations;
    }

    /// <summary>
    /// Obține user_id pentru utilizatorul curent.
    /// </summary>
    /// <param name="conn">Conexiunea la baza de date.</param>
    /// <returns>user_id-ul utilizatorului curent.</returns>
    private static int GetUserId(NpgsqlConnection conn)
    {
        var cmd = new NpgsqlCommand("SELECT user_id FROM users WHERE email = current_user", conn);
        var userId = cmd.ExecuteScalar();
        if (userId == null)
        {
            throw new Exception("User ID not found.");
        }
        return (int)userId;
    }

    /// <summary>
    /// Obține company_id pentru utilizatorul curent.
    /// </summary>
    /// <param name="conn">Conexiunea la baza de date.</param>
    /// <returns>company_id-ul utilizatorului curent.</returns>
    private static int GetCompanyId(NpgsqlConnection conn)
    {
        var cmd = new NpgsqlCommand("SELECT company_id FROM users WHERE email = current_user", conn);
        var companyId = cmd.ExecuteScalar();
        if (companyId == null)
        {
            throw new Exception("Company ID not found.");
        }
        return (int)companyId;
    }

    #endregion
}

/// <summary>
/// Represents a passenger with name and boarding status.
/// </summary>
public class Passenger
{
    /// <summary>
    /// Gets or sets the booking detail ID of the passenger.
    /// </summary>
    public int BookingDetailId { get; set; }

    /// <summary>
    /// Gets or sets the name of the passenger.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the passenger has boarded.
    /// </summary>
    public bool HasBoarded { get; set; }
}

/// <summary>
/// Represents a travel route with departure and arrival locations, price, price ratio, and times.
/// </summary>
public class Route
{
    public string Departure { get; set; }
    public string Arrival { get; set; }
    public int Price { get; set; }
    public double PriceRatio { get; set; }
    public TimeOnly DepartureTime { get; set; }
    public TimeOnly ArrivalTime { get; set; }

    public Route(string departure, string arrival, int price, double priceRatio, TimeOnly departureTime,
        TimeOnly arrivalTime)
    {
        Departure = departure;
        Arrival = arrival;
        Price = price;
        PriceRatio = priceRatio;
        DepartureTime = departureTime;
        ArrivalTime = arrivalTime;
    }
}

/// <summary>
/// Represents route information with departure and arrival details, stops, prices, and times.
/// </summary>
public class RouteInfo
{
    public string RouteIds { get; set; }
    public string DepartureLocation { get; set; }
    public string ArrivalLocation { get; set; }
    public int NumberOfStops { get; set; }
    public int TotalPrice { get; set; }
    public double TotalHours { get; set; }
    public double WeightedScore { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
}
