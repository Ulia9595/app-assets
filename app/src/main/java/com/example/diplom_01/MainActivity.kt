package com.example.diplom_01

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.example.auth.navigation.AuthNavGraph
import com.example.profile.navigation.ProfileNavGraph
import com.example.diplom_01.navigation.RootDestinations

class MainActivity : ComponentActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContent {
            MaterialTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    val rootNavController = rememberNavController()

                    NavHost(
                        navController = rootNavController,
                        startDestination = RootDestinations.AUTH_GRAPH
                    ) {

                        composable(RootDestinations.AUTH_GRAPH) {
                            val authNavController = rememberNavController()
                            AuthNavGraph(
                                navController = authNavController,
                                onAuthFinished = {
                                    rootNavController.navigate(RootDestinations.PROFILE_GRAPH) {
                                        popUpTo(RootDestinations.AUTH_GRAPH) {
                                            inclusive = true
                                        }
                                    }
                                }
                            )
                        }

                        composable(RootDestinations.PROFILE_GRAPH) {
                            val profileNavController = rememberNavController()
                            ProfileNavGraph(
                                navController = profileNavController,
                                onLogoutNavigate = {
                                    rootNavController.navigate(RootDestinations.AUTH_GRAPH) {
                                        popUpTo(RootDestinations.PROFILE_GRAPH) {
                                            inclusive = true
                                        }
                                    }
                                }
                            )
                        }
                    }
                }
            }
        }
    }
}
