package com.example.security

import android.util.Patterns

interface EmailValidator {
    fun isValid(email: String): Boolean
}

class YandexEmailValidator : EmailValidator {
    override fun isValid(email: String): Boolean =
        Patterns.EMAIL_ADDRESS.matcher(email).matches() &&
                (email.endsWith("@yandex.ru") ||
                        email.endsWith("@ya.ru") ||
                        email.endsWith("@yandex.ua") ||
                        email.endsWith("@yandex.kz") ||
                        email.endsWith("@yandex.by") ||
                        email.endsWith("@yandex.com"))
}