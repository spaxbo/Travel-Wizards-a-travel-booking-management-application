/********************************************************************************************************
 * File: LoginInitialState.cs
 * Author: Curuliuc Cosmin-Ștefan
 * Created: 15.05.2024
 * Last Modified: 15.05.2024
 * Description: This file contains the LoginInitialState class which implements the ILoginState interface.
 *
 * Purpose:
 * The purpose of this class is to handle the initial state of the login process by performing the login operation.
 *
 * Usage:
 * - This state is set when the LoginWindow is initialized or after a login attempt.
 *
 ********************************************************************************************************/

namespace TravelWizards;

/// <summary>
/// Represents the initial state of the login process.
/// </summary>
public class LoginInitialState : ILoginState
{
    #region Methods

    /// <summary>
    /// Handles the login state by performing the login operation.
    /// </summary>
    /// <param name="context">The LoginWindow context.</param>
    public void Handle(LoginWindow context) => context.PerformLogin();

    #endregion
}
