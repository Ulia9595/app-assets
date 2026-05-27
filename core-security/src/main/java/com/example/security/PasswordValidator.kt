package com.example.security

import com.example.core_models.PasswordStrength

class PasswordValidator {

    private val specialCharacters = "@#\$%^&+=!"

    fun getDetailedValidation(password: String): ValidationResult {
        val reqs = mapOf(
            "Минимум 8 символов" to (password.length >= 8),
            "Заглавная буква" to password.any { it.isUpperCase() },
            "Цифра" to password.any { it.isDigit() },
            "Спецсимвол: $specialCharacters" to password.any { specialCharacters.contains(it) }
        )

        return ValidationResult(
            isValid = reqs.values.all { it },
            requirements = reqs
        )
    }

    fun calculateStrength(password: String): PasswordStrength {
        if (password.isEmpty()) return PasswordStrength.EMPTY

        val metCount = getDetailedValidation(password)
            .requirements
            .values
            .count { it }

        return when {
            metCount <= 1 -> PasswordStrength.WEAK
            metCount <= 3 -> PasswordStrength.MEDIUM
            else -> PasswordStrength.STRONG
        }
    }

    fun isValid(password: String): Boolean =
        getDetailedValidation(password).isValid
}