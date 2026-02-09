package com.example.data

import android.content.Context
import android.util.Log
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.flow.map

private val Context.dataStore by preferencesDataStore(name = "auth_data_store")

class TokenManager(private val context: Context) {

    companion object {
        private val ACCESS_TOKEN_KEY = stringPreferencesKey("access_token")
        private val REFRESH_TOKEN_KEY = stringPreferencesKey("refresh_token")
        private val USER_DATA_KEY = stringPreferencesKey("user_data")
        private val IS_LOGGED_IN_KEY = booleanPreferencesKey("is_logged_in")
        private val USER_EMAIL_KEY = stringPreferencesKey("user_email")
    }

    private fun normalizeToken(token: String): String =
        token.replace("Bearer", "", ignoreCase = true)
            .replace("\"", "")
            .trim()

    private fun isValidJwt(token: String): Boolean =
        token.count { it == '.' } == 2

    suspend fun saveAccessToken(token: String) {
        val normalized = normalizeToken(token)

        if (!isValidJwt(normalized)) {
            Log.e("TOKEN_MANAGER", "Попытка сохранить НЕ JWT access-token: $normalized")
            return
        }

        context.dataStore.edit { preferences ->
            preferences[ACCESS_TOKEN_KEY] = normalized
            preferences[IS_LOGGED_IN_KEY] = true
        }

        Log.d("TOKEN_MANAGER", "Access token сохранён: '${normalized.take(20)}...'")
    }

    suspend fun saveRefreshToken(token: String) {
        val normalized = normalizeToken(token)

        context.dataStore.edit { preferences ->
            preferences[REFRESH_TOKEN_KEY] = normalized
        }

        Log.d("TOKEN_MANAGER", "Refresh token сохранён: '${normalized.take(20)}...'")
    }

    suspend fun getAccessToken(): String? {
        val rawToken = context.dataStore.data
            .map { it[ACCESS_TOKEN_KEY] }
            .first()
        if (rawToken.isNullOrBlank()) {
            Log.e("TOKEN_MANAGER", "Access token отсутствует")
            return null
        }

        val normalized = normalizeToken(rawToken)

        if (!isValidJwt(normalized)) {
            Log.e("TOKEN_MANAGER", "В DataStore лежит битый access-token: $normalized")
            return null
        }
        Log.d("TOKEN_MANAGER", "Access token получен: '${normalized.take(20)}...'")
        return normalized
    }

    suspend fun getRefreshToken(): String? {
        val rawToken = context.dataStore.data
            .map { it[REFRESH_TOKEN_KEY] }
            .firstOrNull()
        if (rawToken.isNullOrBlank()) {
            Log.e("TOKEN_MANAGER", "Refresh token отсутствует")
            return null
        }

        val normalized = normalizeToken(rawToken)
        Log.d("TOKEN_MANAGER", "Refresh token получен: '${normalized.take(20)}...'")
        return normalized
    }

    suspend fun saveUserData(userDataJson: String) {
        context.dataStore.edit { preferences ->
            preferences[USER_DATA_KEY] = userDataJson
            preferences[IS_LOGGED_IN_KEY] = true
        }
        Log.d("TOKEN_MANAGER", "Данные пользователя сохранены")
    }

    suspend fun getUserData(): String? =
        context.dataStore.data.map { it[USER_DATA_KEY] }.firstOrNull()

    suspend fun saveUserEmail(email: String) {
        context.dataStore.edit { preferences ->
            preferences[USER_EMAIL_KEY] = email
        }
    }

    suspend fun getUserEmail(): String? =
        context.dataStore.data.map { it[USER_EMAIL_KEY] }.firstOrNull()

    suspend fun clearAll() {
        context.dataStore.edit { it.clear() }
    }

    suspend fun isLoggedIn(): Boolean =
        context.dataStore.data.map { it[IS_LOGGED_IN_KEY] ?: false }.first()
}
