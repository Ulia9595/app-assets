package com.example.profile.navigation

import ProfileScreen
import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import com.example.profile.ui.LogoutSuccessScreen

object ProfileDestinations {
    const val MAIN = "profile_main"
    const val LOGOUT_SUCCESS = "logout_success"
}

@Composable
fun ProfileNavGraph(
    navController: NavHostController,
    onLogoutNavigate: () -> Unit
) {
    NavHost(
        navController = navController,
        startDestination = ProfileDestinations.MAIN
    ) {
        composable(ProfileDestinations.MAIN) {
            ProfileScreen(
                onLogoutNavigate = {
                    navController.navigate(ProfileDestinations.LOGOUT_SUCCESS)
                }
            )
        }

        composable(ProfileDestinations.LOGOUT_SUCCESS) {
            LogoutSuccessScreen(
                onContinue = {
                    onLogoutNavigate()
                }
            )
        }
    }
}