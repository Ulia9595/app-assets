package com.example.auth.navigation

sealed class AuthDestinations(val route: String) {
    object Splash : AuthDestinations("splash")
    object Welcome : AuthDestinations("welcome")
    object Login : AuthDestinations("login")
    object ForgotPassword : AuthDestinations("forgot_password")
    object Email : AuthDestinations("email")
    object Code : AuthDestinations("code")
    object Password : AuthDestinations("password")
    object NameAvatar : AuthDestinations("name_avatar")
    object Finish : AuthDestinations("finish")
}