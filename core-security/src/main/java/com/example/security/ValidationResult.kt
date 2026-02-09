package com.example.security

data class ValidationResult(
    val isValid: Boolean,
    val requirements: Map<String, Boolean> = emptyMap()
)