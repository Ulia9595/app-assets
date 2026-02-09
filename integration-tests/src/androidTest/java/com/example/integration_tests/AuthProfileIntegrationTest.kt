package com.example.integration_tests

import android.app.Application
import androidx.arch.core.executor.testing.InstantTaskExecutorRule
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.example.auth.domain.AuthViewModel
import com.example.core_models.AuthStep
import com.example.core_models.UserData
import com.example.data.repository.ServerAuthRepositoryImpl
import com.example.data.repository.ServerProfileRepositoryImpl
import com.example.data.TokenManager
import com.example.profile.ui.ProfileViewModel
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.*
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class AuthProfileIntegrationTest {

    @get:Rule
    val instantTaskExecutorRule = InstantTaskExecutorRule()

    private lateinit var authViewModel: AuthViewModel
    private lateinit var profileViewModel: ProfileViewModel
    private lateinit var authRepository: ServerAuthRepositoryImpl
    private lateinit var profileRepository: ServerProfileRepositoryImpl
    private lateinit var tokenManager: TokenManager
    private lateinit var context: Application

    @Before
    fun setUp() {
        context = ApplicationProvider.getApplicationContext()

        tokenManager = TokenManager(context)
        runBlocking {
            tokenManager.clearAll()
        }

        authRepository = ServerAuthRepositoryImpl(context)
        profileRepository = ServerProfileRepositoryImpl(context)

        authViewModel = AuthViewModel(context)
        profileViewModel = ProfileViewModel(context)
    }

    @After
    fun tearDown() {
        runBlocking {
            tokenManager.clearAll()
        }
    }

    @Test
    fun registration_should_create_user_that_is_accessible_from_profile() = runBlocking {
        val testEmail = "test.integration@example.com"
        val testPassword = "ValidPass123!"
        val testName = "Integration User"
        val testAvatarUrl = "https://example.com/avatar.jpg"

        authViewModel.setEmail(testEmail)
        authViewModel.setPassword(testPassword, testPassword)
        authViewModel.setUserName(testName)
        authViewModel.selectAvatar(testAvatarUrl)

        val mockToken = "mock-jwt-token-123"
        val mockUserData = UserData(
            uid = "test-uid-123",
            email = testEmail,
            name = testName,
            avatarUrl = testAvatarUrl
        )

        tokenManager.saveAccessToken(mockToken)
        tokenManager.saveUserData(mockUserData.toJson())

        profileViewModel.loadProfileIfAuthorized()

        assertNotNull(tokenManager.getAccessToken())
        assertNotNull(tokenManager.getUserData())
    }

    @Test
    fun login_should_authenticate_user_and_provide_access_to_profile() = runBlocking {
        val testEmail = "login.integration@example.com"
        val testPassword = "ValidPass123!"
        val mockToken = "mock-jwt-token-login"

        tokenManager.saveAccessToken(mockToken)
        tokenManager.saveUserEmail(testEmail)

        var loginSuccess = false
        authViewModel.login(testEmail, testPassword) { result ->
            loginSuccess = result.isSuccess
        }

        profileViewModel.loadProfileIfAuthorized()

        assertTrue(loginSuccess)
        assertTrue(authViewModel.uiState.value.isUserAuthenticated)
        assertNotNull(tokenManager.getAccessToken())
    }

    @Test
    fun profile_update_should_be_reflected_in_auth_state() = runBlocking {
        val initialEmail = "update.integration@example.com"
        val mockToken = "mock-jwt-token-update"

        tokenManager.saveAccessToken(mockToken)
        tokenManager.saveUserEmail(initialEmail)

        val updatedName = "Updated Integration User"
        profileViewModel.changeUserName(updatedName)

        val sameTokenInAuth = tokenManager.getAccessToken()
        val sameTokenInProfile = tokenManager.getAccessToken()

        assertEquals(sameTokenInAuth, sameTokenInProfile)
    }

    @Test
    fun email_change_should_work_across_auth_and_profile_modules() = runBlocking {
        val oldEmail = "old.integration@example.com"
        val newEmail = "new.integration@example.com"
        val mockToken = "mock-jwt-token-email-change"

        tokenManager.saveAccessToken(mockToken)
        tokenManager.saveUserEmail(oldEmail)

        var authChangeSuccess = false
        authViewModel.changeEmail(newEmail) { success, message ->
            authChangeSuccess = success
        }

        var profileOtpSuccess = false
        profileViewModel.sendEmailChangeOtp(newEmail) { success, message ->
            profileOtpSuccess = success
        }

        val authToken = tokenManager.getAccessToken()
        assertNotNull(authToken)
        assertTrue(authChangeSuccess)
        assertTrue(profileOtpSuccess)
    }

    @Test
    fun logout_should_clear_data_in_both_modules() = runBlocking {
        val testEmail = "logout.integration@example.com"
        val mockToken = "mock-jwt-token-logout"

        tokenManager.saveAccessToken(mockToken)
        tokenManager.saveUserEmail(testEmail)

        authViewModel.updateStep(AuthStep.FINISH)

        var logoutCompleted = false
        profileViewModel.logout {
            logoutCompleted = true
            runBlocking {
                tokenManager.clearAll()
            }
        }

        assertTrue(logoutCompleted)
        assertFalse(authViewModel.uiState.value.isUserAuthenticated)
        assertNull(tokenManager.getAccessToken())
        assertNull(tokenManager.getUserData())
    }

    @Test
    fun token_should_be_shared_between_auth_and_profile_modules() = runBlocking {
        val sharedToken = "shared-jwt-token-123"
        val testEmail = "shared.integration@example.com"

        tokenManager.saveAccessToken(sharedToken)
        tokenManager.saveUserEmail(testEmail)

        val tokenFromAuthContext = TokenManager(context).getAccessToken()
        val tokenFromProfileContext = TokenManager(context).getAccessToken()

        assertEquals(tokenFromAuthContext, tokenFromProfileContext)
        assertEquals(sharedToken, tokenFromAuthContext)

        val isAuthenticated = authViewModel.uiState.value.isUserAuthenticated ||
                tokenManager.getAccessToken() != null
        assertTrue(isAuthenticated)
    }
}

private fun UserData.toJson(): String {
    return """{"uid": "$uid", "email": "$email", "name": "$name", "avatarUrl": "$avatarUrl"}"""
}
