/********************************************************************************************************
 * File: LoginFailedState.cs
 * Author: Curuliuc Cosmin-Ștefan
 * Created: 15.05.2024
 * Last Modified: 15.05.2024
 * Description: This file contains the LoginFailedState class which implements the ILoginState interface.
 *
 * Purpose:
 * The purpose of this class is to handle the failed login state by displaying an error message and
 * resetting the login state.
 *
 * Usage:
 * - This state is set after a failed login attempt.
 *
 ********************************************************************************************************/

namespace TravelWizards;
using System.Windows;

/// <summary>
/// Represents the failed state of the login process.
/// </summary>
public class LoginFailedState : ILoginState
{
    #region Methods

    /// <summary>
    /// Handles the login state by displaying an error message and resetting the login state.
    /// </summary>
    /// <param name="context">The LoginWindow context.</param>
    public void Handle(LoginWindow context)
    {
        Window messageBox = new CustomMessageWindow("error", "Authentication Error", "Login Failed. Please check your credentials.");
        _ = messageBox.ShowDialog();
        context.SetState(new LoginInitialState());
    }

    #endregion
}
