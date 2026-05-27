package com.example.diplom_01


import android.content.Context
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import com.example.auth.navigation.AuthNavGraph
import com.example.core_data.pvp.PvpManager
import com.example.core.ui.NoInternetScreen
import com.example.core.ui.ServerErrorScreen
import com.example.data.network.HttpClientFactory
import com.example.data.network.NetworkMonitor
import com.example.data.hubs.NotificationHub
import com.example.core_models.InviteState
import com.example.data.AppConfig
import com.example.data.TokenManager
import com.example.diplom_01.navigation.RootDestinations
import com.example.learning.LearningViewModel
import com.example.learning.LearningViewModelFactory
import com.example.learning.navigation.LearningDestinations
import com.example.learning.navigation.learningNavGraph
import com.example.profile.navigation.ProfileNavGraph
import com.example.profile.ui.ProfileViewModel
import com.example.pvp.navigation.PvpDestinations
import com.example.pvp.navigation.pvpNavGraph
import com.example.pvp.ui.InviteDialog

class MainActivity : ComponentActivity() {

    private lateinit var pvpManager: PvpManager
    private lateinit var tokenManager: TokenManager
    private lateinit var networkMonitor: NetworkMonitor
    private lateinit var notificationHub: NotificationHub


    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        tokenManager = TokenManager(applicationContext)
        pvpManager = PvpManager(
            serverUrl     = SERVER_BASE_URL,
            tokenProvider = { tokenManager.getTokenBlocking() }
        )
        networkMonitor = NetworkMonitor(applicationContext)

        notificationHub = NotificationHub(
            serverUrl = SERVER_BASE_URL,
            tokenProvider = { tokenManager.getTokenBlocking() }
        )

        setContent {
            MaterialTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    val isConnected by networkMonitor.isConnected.collectAsState(initial = true)
                    val isServerAvailable by HttpClientFactory.serverAvailable.collectAsState()

                    when {
                        !isConnected -> {
                            NoInternetScreen(modifier = Modifier.fillMaxSize())
                        }
                        !isServerAvailable -> {
                            ServerErrorScreen(
                                modifier = Modifier.fillMaxSize(),
                                onRetry  = {
                                    HttpClientFactory.resetServerAvailable()
                                }
                            )
                        }
                        else -> {
                            MainContent(
                                context      = applicationContext,
                                pvpManager   = pvpManager,
                                tokenManager = tokenManager,
                                notificationHub = notificationHub,
                            )
                        }
                    }
                }
            }
        }
    }

    override fun onDestroy() {
        pvpManager.disconnect()
        notificationHub.disconnect()
        super.onDestroy()
    }

    companion object {
        private val SERVER_BASE_URL
            get() = AppConfig.serverUrl
    }
}

@Composable
private fun MainContent(
    context: Context,
    pvpManager: PvpManager,
    tokenManager: TokenManager,
    notificationHub: NotificationHub,
) {
    val rootNavController = rememberNavController()

    val profileViewModel: ProfileViewModel = viewModel()
    val learningViewModel: LearningViewModel = viewModel(
        factory = LearningViewModelFactory(context)
    )

    val notificationViewModel: NotificationViewModel = viewModel(
        factory = NotificationViewModelFactory(notificationHub)
    )
    val notificationState by notificationViewModel.state.collectAsState()

    val profileState by profileViewModel.state.collectAsState()
    val isLoggedIn by produceState(initialValue = false) {
        value = tokenManager.isLoggedIn()
    }

    LaunchedEffect(profileState.eloPoints, profileState.name) {
        learningViewModel.updateUserInfo(
            name = profileState.name,
            elo  = profileState.eloPoints
        )
    }

    LaunchedEffect(isLoggedIn) {
        android.util.Log.d("PvpManager", "isLoggedIn: $isLoggedIn")
        if (isLoggedIn) {
            pvpManager.connect()
            notificationHub.connect()
        } else {
            pvpManager.disconnect()
            notificationHub.disconnect()
        }
    }

    val myUserId = remember(isLoggedIn) {
        if (isLoggedIn) {
            tokenManager.getUserIdFromToken()
        } else {
            0
        }
    }

    val navBackStackEntry by rootNavController.currentBackStackEntryAsState()
    val currentRoute = navBackStackEntry?.destination?.route

    val inviteState by pvpManager.inviteState.collectAsState()
    if (currentRoute == PvpDestinations.LOBBY && inviteState !is InviteState.None) {
        InviteDialog(
            inviteState = inviteState,
            onAccept = { tournamentId -> pvpManager.acceptInvite(tournamentId) },
            onDecline = { tournamentId -> pvpManager.declineInvite(tournamentId) }
        )
    }

    NavHost(
        navController    = rootNavController,
        startDestination = RootDestinations.AUTH_GRAPH
    ) {
        composable(RootDestinations.AUTH_GRAPH) {
            val authNavController = rememberNavController()
            AuthNavGraph(
                navController = authNavController,
                onAuthFinished = {
                    rootNavController.navigate(RootDestinations.PROFILE_GRAPH) {
                        popUpTo(RootDestinations.AUTH_GRAPH) { inclusive = true }
                    }
                }
            )
        }

        composable(RootDestinations.PROFILE_GRAPH) {
            val profileNavController = rememberNavController()
            ProfileNavGraph(
                navController = profileNavController,
                profileViewModel = profileViewModel,
                unreadNotificationCount = notificationState.unreadCount,
                onLogoutNavigate = {
                    pvpManager.disconnect()
                    notificationHub.disconnect()
                    learningViewModel.updateUserInfo("Игрок", 500)
                    rootNavController.navigate(RootDestinations.AUTH_GRAPH) {
                        popUpTo(RootDestinations.PROFILE_GRAPH) { inclusive = true }
                    }
                },
                onGoToLobby = { rootNavController.navigate(LearningDestinations.GRAPH_ROUTE) },
                onGoToPvp = { rootNavController.navigate(PvpDestinations.GRAPH_ROUTE) },
                onGoToNotifications = {
                    rootNavController.navigate(RootDestinations.NOTIFICATIONS)
                }
            )
        }

        learningNavGraph(
            navController = rootNavController,
            viewModel = learningViewModel,
            onNavigateBack = {
                profileViewModel.refreshProfile()
                rootNavController.popBackStack()
            }
        )

        pvpNavGraph(
            navController = rootNavController,
            pvpManager = pvpManager,
            myUserId = myUserId,
            onNavigateBack = {
                profileViewModel.refreshProfile()
                rootNavController.navigate(RootDestinations.PROFILE_GRAPH) {
                    popUpTo(PvpDestinations.GRAPH_ROUTE) { inclusive = true }
                }
            }
        )

        composable(RootDestinations.NOTIFICATIONS) {
            NotificationScreen(
                viewModel = notificationViewModel,
                onBack = { rootNavController.popBackStack() }
            )
        }
    }
}

