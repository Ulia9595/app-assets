package com.example.pvp.navigation

import androidx.compose.runtime.remember
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavGraphBuilder
import androidx.navigation.NavHostController
import androidx.navigation.compose.composable
import androidx.navigation.navigation
import com.example.core_data.pvp.PvpManager
import com.example.pvp.PvpViewModel
import com.example.pvp.PvpViewModelFactory
import com.example.pvp.ui.PvpLobbyScreen
import com.example.pvp.ui.TournamentResultScreen
import com.example.pvp.ui.TournamentScreen

object PvpDestinations {
    const val GRAPH_ROUTE  = "pvp_graph"
    const val LOBBY        = "pvp_lobby"
    const val TOURNAMENT   = "pvp_tournament"
    const val RESULT       = "pvp_result"
}

fun NavGraphBuilder.pvpNavGraph(
    navController: NavHostController,
    pvpManager: PvpManager,
    myUserId: Int,
    onNavigateBack: () -> Unit
) {
    navigation(
        startDestination = PvpDestinations.LOBBY,
        route            = PvpDestinations.GRAPH_ROUTE
    ) {
        composable(PvpDestinations.LOBBY) { backStackEntry ->
            val graphEntry = remember(backStackEntry) {
                navController.getBackStackEntry(PvpDestinations.GRAPH_ROUTE)
            }
            val viewModel: PvpViewModel = viewModel(
                viewModelStoreOwner = graphEntry,
                factory = PvpViewModelFactory(pvpManager)
            )
            viewModel.setMyUserId(myUserId)

            PvpLobbyScreen(
                viewModel            = viewModel,
                onNavigateBack       = onNavigateBack,
                onNavigateToTournament = {
                    navController.navigate(PvpDestinations.TOURNAMENT) {
                    }
                }
            )
        }

        composable(PvpDestinations.TOURNAMENT) { backStackEntry ->
            val graphEntry = remember(backStackEntry) {
                navController.getBackStackEntry(PvpDestinations.GRAPH_ROUTE)
            }
            val viewModel: PvpViewModel = viewModel(
                viewModelStoreOwner = graphEntry,
                factory = PvpViewModelFactory(pvpManager)
            )

            TournamentScreen(
                viewModel          = viewModel,
                onNavigateToResult = {
                    navController.navigate(PvpDestinations.RESULT) {
                        popUpTo(PvpDestinations.TOURNAMENT) { inclusive = true }
                    }
                }
            )
        }

        composable(PvpDestinations.RESULT) { backStackEntry ->
            val graphEntry = remember(backStackEntry) {
                navController.getBackStackEntry(PvpDestinations.GRAPH_ROUTE)
            }
            val viewModel: PvpViewModel = viewModel(
                viewModelStoreOwner = graphEntry,
                factory = PvpViewModelFactory(pvpManager)
            )

            TournamentResultScreen(
                viewModel  = viewModel,
                onPlayAgain = {
                    navController.navigate(PvpDestinations.LOBBY) {
                        popUpTo(PvpDestinations.RESULT) { inclusive = true }
                    }
                },
                onGoToProfile = {
                    onNavigateBack()
                }
            )
        }
    }
}