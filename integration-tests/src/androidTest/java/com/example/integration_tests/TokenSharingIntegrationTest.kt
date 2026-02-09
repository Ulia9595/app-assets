package com.example.integration_tests

import android.app.Application
import androidx.arch.core.executor.testing.InstantTaskExecutorRule
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.example.data.TokenManager
import kotlinx.coroutines.runBlocking
import org.junit.Assert.*
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class TokenSharingIntegrationTest {

    @get:Rule
    val instantTaskExecutorRule = InstantTaskExecutorRule()

    private lateinit var tokenManager: TokenManager
    private lateinit var context: Application

    @Before
    fun setUp() {
        context = ApplicationProvider.getApplicationContext()
        tokenManager = TokenManager(context)

        runBlocking {
            tokenManager.clearAll()
        }
    }

    @Test
    fun token_saved_by_auth_should_be_accessible_by_profile() = runBlocking {
        val authToken = "auth-saved-token-xyz"
        val userEmail = "token@example.com"
        val userData = """{"uid": "token-uid", "email": "$userEmail", "name": "Token User"}"""

        tokenManager.saveAccessToken(authToken)
        tokenManager.saveUserEmail(userEmail)
        tokenManager.saveUserData(userData)

        val tokenForProfile = tokenManager.getAccessToken()
        val emailForProfile = tokenManager.getUserEmail()
        val dataForProfile = tokenManager.getUserData()

        assertEquals(authToken, tokenForProfile)
        assertEquals(userEmail, emailForProfile)
        assertEquals(userData, dataForProfile)
    }

    @Test
    fun clearing_token_in_one_module_should_affect_both() = runBlocking {
        val sharedToken = "shared-token-123"
        tokenManager.saveAccessToken(sharedToken)

        tokenManager.clearAll()

        val tokenAfterClear = tokenManager.getAccessToken()
        val userDataAfterClear = tokenManager.getUserData()
        val isLoggedIn = tokenManager.isLoggedIn()

        assertNull(tokenAfterClear)
        assertNull(userDataAfterClear)
        assertFalse(isLoggedIn)
    }
}
