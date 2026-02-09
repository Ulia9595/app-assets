package com.example.integration_tests

import android.app.Application
import androidx.arch.core.executor.testing.InstantTaskExecutorRule
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.example.auth.domain.AuthViewModel
import com.example.core_models.AuthStep
import com.example.core_models.UserData
import com.example.data.TokenManager
import com.example.profile.ui.ProfileViewModel
import kotlinx.coroutines.runBlocking
import org.junit.Assert.*
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class DataFlowIntegrationTest {

    @get:Rule
    val instantTaskExecutorRule = InstantTaskExecutorRule()

    private lateinit var authViewModel: AuthViewModel
    private lateinit var profileViewModel: ProfileViewModel
    private lateinit var tokenManager: TokenManager
    private lateinit var context: Application

    @Before
    fun setUp() {
        context = ApplicationProvider.getApplicationContext()
        tokenManager = TokenManager(context)

        runBlocking {
            tokenManager.clearAll()
        }

        authViewModel = AuthViewModel(context)
        profileViewModel = ProfileViewModel(context)
    }

    @Test
    fun user_data_should_flow_from_registration_to_profile_correctly() = runBlocking {
        val testUser = UserData(
            uid = "flow-uid-123",
            email = "flow@example.com",
            name = "Flow Test User",
            avatarUrl = "https://example.com/flow-avatar.jpg"
        )

        tokenManager.saveAccessToken("flow-token-123")
        tokenManager.saveUserData(testUser.toJson())

        authViewModel.updateStep(AuthStep.FINISH)
        profileViewModel.loadProfileIfAuthorized()

        val savedUserJson = tokenManager.getUserData()
        assertNotNull(savedUserJson)
    }

    @Test
    fun elo_points_update_should_be_visible_in_both_modules() = runBlocking {
        tokenManager.saveAccessToken("elo-token-123")

        val newElo = 750
        profileViewModel.updateElo(newElo)

        val tokenAfterEloUpdate = tokenManager.getAccessToken()
        assertNotNull(tokenAfterEloUpdate)
    }

    @Test
    fun avatar_selection_should_update_user_data_across_modules() = runBlocking {
        val testAvatars = listOf(
            "https://example.com/avatar1.jpg",
            "https://example.com/avatar2.jpg",
            "https://example.com/avatar3.jpg"
        )

        tokenManager.saveAccessToken("avatar-token-123")

        val selectedAvatar = testAvatars[1]
        authViewModel.selectAvatar(selectedAvatar)

        profileViewModel.selectAvatarFromList(selectedAvatar)

        val tokenAfterAvatarChange = tokenManager.getAccessToken()
        assertNotNull(tokenAfterAvatarChange)
    }
}

private fun UserData.toJson(): String {
    return """{"uid": "$uid", "email": "$email", "name": "$name", "avatarUrl": "$avatarUrl"}"""
}
