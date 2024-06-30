/********************************************************************************************************
 * File: LoginSuccessState.cs
 * Author: Curuliuc Cosmin-Ștefan
 * Created: 15.05.2024
 * Last Modified: 21.05.2024
 * Description: This file contains the LoginSuccessState class which implements the ILoginState interface.
 *
 * Purpose:
 * The purpose of this class is to handle the successful login state by displaying a success message and
 * opening the role-specific window.
 *
 * Usage:
 * - This state is set after a successful login attempt.
 *
 ********************************************************************************************************/

namespace TravelWizards;
using System.Windows;

/// <summary>
/// Represents the successful state of the login process.
/// </summary>
public class LoginSuccessState : ILoginState
{
    #region Methods

    /// <summary>
    /// Handles the login state by displaying a success message and opening the role-specific window.
    /// </summary>
    /// <param name="context">The LoginWindow context.</param>
    public void Handle(LoginWindow context)
    {
        Window messageBox = new CustomMessageWindow("info", "Login", "Login Successful!");
        _ = messageBox.ShowDialog();
        context.OpenRoleSpecificWindow();
        context.SetState(new LoginInitialState());
    }

    #endregion
}
