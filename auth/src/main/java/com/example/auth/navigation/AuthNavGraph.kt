package com.example.auth.navigation

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.lifecycle.viewmodel.compose.viewModel
import com.example.core_models.AuthStep
import com.example.auth.domain.AuthViewModel
import com.example.auth.ui.*
import android.util.Log

@Composable
fun AuthNavGraph(
    navController: NavHostController,
    onAuthFinished: () -> Unit
) {
    val authViewModel: AuthViewModel = viewModel()
    val uiState by authViewModel.uiState.collectAsState()

    LaunchedEffect(uiState.isUserAuthenticated) {
        if (uiState.isUserAuthenticated) {
            navController.navigate(AuthDestinations.Finish.route) {
                popUpTo(AuthDestinations.Welcome.route) { inclusive = true }
            }
        } else {
            Log.d("AUTH_NAV", "⚪ Пользователь НЕ авторизован, ждем...")
        }
    }

    NavHost(
        navController = navController,
        startDestination = AuthDestinations.Splash.route
    ) {
        composable(AuthDestinations.Splash.route) {
            LaunchedEffect(uiState.isUserAuthenticated) {
                if (uiState.isUserAuthenticated) {
                    onAuthFinished()
                } else {
                    navController.navigate(AuthDestinations.Welcome.route) {
                        popUpTo(AuthDestinations.Splash.route) { inclusive = true }
                    }
                }
            }
            SplashScreen()
        }

        composable(AuthDestinations.Welcome.route) {
            WelcomeScreen(
                onLoginClick = {
                    authViewModel.resetAllRegistrationData()
                    navController.navigate(AuthDestinations.Login.route)
                },
                onRegisterClick = {
                    authViewModel.resetAllRegistrationData()
                    authViewModel.updateStep(AuthStep.EMAIL)
                    navController.navigate(AuthDestinations.Email.route)
                }
            )
        }

        composable(AuthDestinations.Login.route) {
            LoginScreen(
                viewModel = authViewModel,
                onLoginSuccess = {
                    authViewModel.clearError()
                },
                onForgotPassword = {
                    navController.navigate(AuthDestinations.ForgotPassword.route)
                },
                onBack = {
                    authViewModel.clearError()
                    navController.navigate(AuthDestinations.Welcome.route) {
                        popUpTo(AuthDestinations.Login.route) { inclusive = true }
                    }
                }
            )
        }

        composable(AuthDestinations.ForgotPassword.route) {
            ForgotPasswordScreen(
                viewModel = authViewModel,
                onBack = {
                    authViewModel.clearError()
                    navController.popBackStack()
                },
                onSuccess = { message ->
                    navController.popBackStack()
                }
            )
        }

        composable(AuthDestinations.Email.route) {
            EmailScreen(
                viewModel = authViewModel,
                onNext = {
                    authViewModel.clearError()
                    authViewModel.updateStep(AuthStep.CODE)
                    navController.navigate(AuthDestinations.Code.route)
                },
                onBack = {
                    authViewModel.clearError()
                    authViewModel.resetAllRegistrationData()
                    navController.navigate(AuthDestinations.Welcome.route) {
                        popUpTo(AuthDestinations.Email.route) { inclusive = true }
                    }
                }
            )
        }

        composable(AuthDestinations.Code.route) {
            CodeScreen(
                viewModel = authViewModel,
                onNext = {
                    authViewModel.clearError()
                    authViewModel.updateStep(AuthStep.PASSWORD)
                    navController.navigate(AuthDestinations.Password.route)
                },
                onBack = {
                    authViewModel.clearError()
                    authViewModel.resetAllRegistrationData()
                    authViewModel.updateStep(AuthStep.EMAIL)
                    navController.navigate(AuthDestinations.Email.route) {
                        popUpTo(AuthDestinations.Code.route) { inclusive = true }
                    }
                }
            )
        }

        composable(AuthDestinations.Password.route) {
            PasswordScreen(
                viewModel = authViewModel,
                onNext = {
                    authViewModel.clearError()
                    authViewModel.updateStep(AuthStep.NAME_AVATAR)
                    navController.navigate(AuthDestinations.NameAvatar.route)
                },
                onBack = {
                    authViewModel.clearError()
                    authViewModel.resetAllRegistrationData()
                    authViewModel.updateStep(AuthStep.EMAIL)
                    navController.navigate(AuthDestinations.Email.route) {
                        popUpTo(AuthDestinations.Email.route) { inclusive = false }
                    }
                }
            )
        }

        composable(AuthDestinations.NameAvatar.route) {
            NameAvatarScreen(
                viewModel = authViewModel,
                onFinish = {
                    authViewModel.clearError()
                    authViewModel.finishRegistration(onSuccess = {
                    })
                },
                onBack = {
                    authViewModel.clearError()
                    authViewModel.updateStep(AuthStep.PASSWORD)
                    navController.popBackStack()
                }
            )
        }

        composable(AuthDestinations.Finish.route) {
            FinishScreen(
                onAnimationFinished = {
                    onAuthFinished()
                }
            )
        }
    }
}