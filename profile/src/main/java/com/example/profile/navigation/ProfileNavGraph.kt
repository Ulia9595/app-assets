package com.example.profile.navigation

import ProfileScreen
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import com.example.profile.ui.LeaderboardScreen
import com.example.profile.ui.LogoutSuccessScreen
import com.example.profile.ui.ProfileViewModel
import com.example.profile.ui.RatingHistoryScreen

object ProfileDestinations {
    const val MAIN           = "profile_main"
    const val LOGOUT_SUCCESS = "logout_success"
    const val RATING_HISTORY = "rating_history"
    const val LEADERBOARD    = "leaderboard"
}

@Composable
fun ProfileNavGraph(
    navController: NavHostController,
    profileViewModel: ProfileViewModel,
    unreadNotificationCount: Int = 0,
    onLogoutNavigate: () -> Unit,
    onGoToLobby: () -> Unit,
    onGoToPvp: () -> Unit,
    onGoToNotifications: () -> Unit
) {
    NavHost(
        navController = navController,
        startDestination = ProfileDestinations.MAIN
    ) {
        composable(ProfileDestinations.MAIN) {
            LaunchedEffect(Unit) {
                profileViewModel.refreshProfile()
            }

            ProfileScreen(
                profileViewModel = profileViewModel,
                unreadNotificationCount = unreadNotificationCount,
                onLogoutNavigate = {
                    navController.navigate(ProfileDestinations.LOGOUT_SUCCESS)
                },
                onGoToLobby = onGoToLobby,
                onGoToPvp = onGoToPvp,
                onGoToRatingHistory = {
                    navController.navigate(ProfileDestinations.RATING_HISTORY)
                },
                onGoToLeaderboard = {
                    navController.navigate(ProfileDestinations.LEADERBOARD)
                },
                onGoToNotifications = onGoToNotifications
            )
        }

        composable(ProfileDestinations.LOGOUT_SUCCESS) {
            LogoutSuccessScreen(
                onContinue = { onLogoutNavigate() }
            )
        }

        composable(ProfileDestinations.RATING_HISTORY) {
            RatingHistoryScreen(
                profileViewModel = profileViewModel,
                onBack = { navController.popBackStack() }
            )
        }

        composable(ProfileDestinations.LEADERBOARD) {
            LeaderboardScreen(
                profileViewModel = profileViewModel,
                onBack = { navController.popBackStack() }
            )
        }
    }
}