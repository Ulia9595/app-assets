package com.example.core_models

enum class PasswordStrength(val score: Int, val label: String) {
    EMPTY(0, ""),
    WEAK(1, "Слабый"),
    MEDIUM(2, "Средний"),
    STRONG(3, "Надежный")
}