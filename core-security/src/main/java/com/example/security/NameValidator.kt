package com.example.security

class NameValidator {
    private val pattern =
        Regex("^(?=(.*[A-Za-z]){2,})[A-Za-z0-9_]+( [A-Za-z0-9_]+)?$")

    fun isValid(name: String): Boolean {
        val trimmed = name.trim()


        if (trimmed.length !in 2..30) {
            return false
        }

        val match = pattern.matches(trimmed)

        return match
    }
}