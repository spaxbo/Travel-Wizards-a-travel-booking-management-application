/********************************************************************************************************
 * File: ILoginState.cs
 * Author: Curuliuc Cosmin-Ștefan
 * Created: 15.05.2024
 * Last Modified: 15.05.2024
 * Description: This file contains the ILoginState interface which defines a method for handling login states.
 *
 * Purpose:
 * The purpose of this interface is to define a method that handles login states in the LoginWindow context.
 *
 * Usage:
 * - Implement this interface to define specific behaviors for different login states.
 *
 ********************************************************************************************************/

namespace TravelWizards;

/// <summary>
/// Defines a method for handling login states in the LoginWindow context.
/// </summary>
public interface ILoginState
{
    /// <summary>
    /// Handles the login state in the LoginWindow context.
    /// </summary>
    /// <param name="context">The LoginWindow context.</param>
    void Handle(LoginWindow context);
}
