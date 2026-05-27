package com.example.learning.navigation

import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavGraphBuilder
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.composable
import androidx.navigation.navArgument
import androidx.navigation.navigation
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import com.example.learning.LearningViewModel
import com.example.learning.TaskViewModel
import com.example.learning.TaskViewModelFactory
import com.example.learning.ui.LevelDetailScreen
import com.example.learning.ui.LevelsScreen
import com.example.learning.ui.LobbyScreen
import com.example.learning.ui.TaskScreen
import com.example.learning.ui.TheoryScreen

object LearningDestinations {
    const val GRAPH_ROUTE  = "learning"
    const val LOBBY        = "lobby"
    const val LEVELS       = "levels/{topicId}/{topicName}"
    const val LEVEL_DETAIL = "level_detail/{levelId}"
    const val THEORY       = "theory/{title}/{content}"
    const val TASK         = "task/{taskId}"

    fun levelsRoute(topicId: Int, topicName: String) =
        "levels/$topicId/${topicName.encode()}"
    fun levelDetailRoute(levelId: Int) = "level_detail/$levelId"
    fun theoryRoute(title: String, content: String) =
        "theory/${title.encode()}/${content.encode()}"
    fun taskRoute(taskId: Int) = "task/$taskId"

    private fun String.encode() = java.net.URLEncoder.encode(this, "UTF-8")
}

fun NavGraphBuilder.learningNavGraph(
    navController: NavHostController,
    viewModel: LearningViewModel,
    onNavigateBack: () -> Unit,
    onEloRefresh: () -> Unit = {}
) {
    navigation(
        startDestination = LearningDestinations.LOBBY,
        route = LearningDestinations.GRAPH_ROUTE
    ) {

        composable(LearningDestinations.LOBBY) {
            androidx.compose.runtime.LaunchedEffect(Unit) {
                onEloRefresh()
            }
            LobbyScreen(
                viewModel = viewModel,
                onTopicClick = { topicId, topicName ->
                    navController.navigate(LearningDestinations.levelsRoute(topicId, topicName))
                },
                onBack = onNavigateBack
            )
        }

        composable(
            route = LearningDestinations.LEVELS,
            arguments = listOf(
                navArgument("topicId")   { type = NavType.IntType },
                navArgument("topicName") { type = NavType.StringType }
            )
        ) { backStack ->
            val topicId   = backStack.arguments?.getInt("topicId") ?: return@composable
            val topicName = backStack.arguments?.getString("topicName")?.decode() ?: ""
            LevelsScreen(
                viewModel = viewModel,
                topicId = topicId,
                topicName = topicName,
                onLevelClick = { levelId ->
                    navController.navigate(LearningDestinations.levelDetailRoute(levelId))
                },
                onBack = { navController.popBackStack() }
            )
        }

        composable(
            route = LearningDestinations.LEVEL_DETAIL,
            arguments = listOf(navArgument("levelId") { type = NavType.IntType })
        ) { backStack ->
            val levelId = backStack.arguments?.getInt("levelId") ?: return@composable
            LevelDetailScreen(
                viewModel = viewModel,
                levelId = levelId,
                onBack = { navController.popBackStack() },
                onOpenTheory = { title, content ->
                    navController.navigate(LearningDestinations.theoryRoute(title, content))
                },
                onOpenTask = { taskId ->
                    navController.navigate(LearningDestinations.taskRoute(taskId))
                }
            )
        }

        composable(
            route = LearningDestinations.THEORY,
            arguments = listOf(
                navArgument("title")   { type = NavType.StringType },
                navArgument("content") { type = NavType.StringType }
            )
        ) { backStack ->
            val title   = backStack.arguments?.getString("title")?.decode()   ?: ""
            val content = backStack.arguments?.getString("content")?.decode() ?: ""
            val userName by viewModel.userName.collectAsState()
            val userElo  by viewModel.userElo.collectAsState()
            TheoryScreen(
                title    = title,
                content  = content,
                userName = userName,
                userElo  = userElo,
                onBack   = { navController.popBackStack() }
            )
        }

        composable(
            route = LearningDestinations.TASK,
            arguments = listOf(navArgument("taskId") { type = NavType.IntType })
        ) { backStack ->
            val taskId = backStack.arguments?.getInt("taskId") ?: return@composable
            val taskViewModel: TaskViewModel = viewModel(
                factory = TaskViewModelFactory(navController.context)
            )
            val userName by viewModel.userName.collectAsState()
            val userElo  by viewModel.userElo.collectAsState()
            TaskScreen(
                viewModel    = taskViewModel,
                taskId       = taskId,
                userName     = userName,
                userElo      = userElo,
                onBack       = { navController.popBackStack() },
                onTaskSolved = {
                    onEloRefresh()
                    navController.popBackStack()
                }
            )
        }
    }
}

private fun String.decode() = java.net.URLDecoder.decode(this, "UTF-8")