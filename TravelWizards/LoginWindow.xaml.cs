/********************************************************************************************************
 * File: LoginWindow.cs
 * Author: Curuliuc Cosmin-Ștefan
 * Created: 23.04.2024
 * Last Modified: 15.05.2024
 * Description: This file contains the implementation of the LoginWindow class which is responsible for
 *              managing user authentication against PostgreSQL roles and redirecting users based on their role.
 *
 * Purpose:
 * The purpose of this class is to handle user login, verify credentials against a PostgreSQL database, and
 * redirect users to their respective windows based on their role.
 *
 * Usage:
 * - Users enter their email and password.
 * - The application attempts to authenticate the user.
 * - Upon successful authentication, the application redirects the user to a specific window based on their role.
 *
 * Notes:
 * - Ensure the PostgreSQL server is running and accessible.
 * - Database schema should include a 'users' table with 'email' and 'role' columns.
 *
 ********************************************************************************************************/

namespace TravelWizards;
using System;
using Npgsql;
using System.Windows;
using System.Diagnostics.Eventing.Reader;

/// <summary>
/// Interaction logic for LoginWindow.xaml
/// This window manages user authentication against PostgreSQL roles and redirects users based on their role.
/// </summary>
public partial class LoginWindow
{
    private const string ConnectionString = "Host=localhost;Database=postgres";
    private string specificConnectionString = "";
    private ILoginState currentState = new LoginInitialState();

    /// <summary>
    /// Initializes a new instance of the LoginWindow class.
    /// </summary>
    public LoginWindow() => this.InitializeComponent();

    #region Event Handlers

    /// <summary>
    /// Handles the Click event of the login button.
    /// Tries to log in using the provided credentials, opening the appropriate window based on the role.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The RoutedEventArgs that contains the event data.</param>
    private void ButtonLogin_Click(object sender, RoutedEventArgs e) => this.currentState.Handle(this);

    /// <summary>
    /// Performs the login operation by validating the user's credentials and determining the user's role.
    /// Updates the current state based on the success or failure of the login attempt.
    /// </summary>
    public void PerformLogin()
    {
        var email = this.TextBoxEmail.Text;
        var password = this.TextBoxPassword.Password;

        // Use specific login details to try connecting to the database
        this.specificConnectionString = $"{ConnectionString};Username={email};Password={password}";

        try
        {
            using var conn = new NpgsqlConnection(this.specificConnectionString);
            conn.Open();
            // If connection is successful, determine user role
            var userRole = DetermineUserRole(conn, email);
            if (userRole != null)
            {
                this.SetState(new LoginSuccessState());
                this.currentState.Handle(this);
            }
            else
            {
                throw new EventLogNotFoundException("User does not have a role assigned.");
            }
        }
        catch (NpgsqlException)
        {
            this.SetState(new LoginFailedState());
            this.currentState.Handle(this);
        }
        catch (Exception ex)
        {
            Window messageBox = new CustomMessageWindow("error", "Error", $"An error occurred: {ex.Message}");
            _ = messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Handles the GotFocus event of the email TextBox.
    /// Clears the placeholder text when the TextBox gains focus.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The RoutedEventArgs that contains the event data.</param>
    private void TextBoxEmail_GotFocus(object sender, RoutedEventArgs e)
    {
        if (this.TextBoxEmail.Text == "Email")
        {
            this.TextBoxEmail.Text = "";
        }
    }

    /// <summary>
    /// Handles the GotFocus event of the password TextBox.
    /// Clears the placeholder text when the TextBox gains focus.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The RoutedEventArgs that contains the event data.</param>
    private void TextBoxPassword_GotFocus(object sender, RoutedEventArgs e)
    {
        if (this.TextBoxPassword.Password == "Password")
        {
            this.TextBoxPassword.Password = "";
        }
    }

    /// <summary>
    /// Handles the LostFocus event of the email TextBox.
    /// Restores the placeholder text if the TextBox is empty when it loses focus.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The RoutedEventArgs that contains the event data.</param>
    private void TextBoxEmail_LostFocus(object sender, RoutedEventArgs e)
    {
        if (this.TextBoxEmail.Text == "")
        {
            this.TextBoxEmail.Text = "Email";
        }
    }

    #endregion

    #region User Role Management

    /// <summary>
    /// Determines the user's role from the database based on their email.
    /// </summary>
    /// <param name="conn">An open NpgsqlConnection.</param>
    /// <param name="email">The user's email, used to fetch their role.</param>
    /// <returns>The role of the user as a string, or null if the role is not found.</returns>
    private static string? DetermineUserRole(NpgsqlConnection conn, string email)
    {
        const string sql = "SELECT role FROM users WHERE email = @Email";
        using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@Email", email);
        return cmd.ExecuteScalar() as string;
    }

    /// <summary>
    /// Opens the window corresponding to the user's role and hides the login window.
    /// </summary>
    public void OpenRoleSpecificWindow()
    {
        using var conn = new NpgsqlConnection(this.specificConnectionString);
        conn.Open();

        var role = DetermineUserRole(conn, this.TextBoxEmail.Text);
        Window? roleWindow = null;
        switch (role)
        {
            case "transportation_company_role":
                roleWindow = new CompanyWindow(this.specificConnectionString);
                break;
            case "travel_agent_role":
                roleWindow = new TravelAgentWindow(this.specificConnectionString);
                break;
            case "boarding_agent_role":
                roleWindow = new BoardingAgentWindow(this.specificConnectionString);
                break;
            default:
                Window messageBox = new CustomMessageWindow("error", "Authentication Error", "Unknown or unimplemented role!");
                _ = messageBox.ShowDialog();
                return;
        }

        roleWindow.Show();
        this.Hide();
    }

    #endregion

    #region Setters

    /// <summary>
    /// Sets the current state of the login process.
    /// </summary>
    /// <param name="state">The new state to set.</param>
    public void SetState(ILoginState state) => this.currentState = state;

    #endregion
}
