package com.example.security

import org.apache.commons.lang3.StringUtils

class ProfanityFilter {
    private val forbiddenRoots = setOf(
        "fuck", "shit", "bitch", "asshol", "bastard",
        "cunt", "dick", "pussy", "fag", "slut", "whore"
    )

    fun containsProfanity(text: String): Boolean {
        if (StringUtils.isBlank(text)) return false

        val normalizedText = prepareText(text)

        return forbiddenRoots.any { root ->
            StringUtils.containsIgnoreCase(normalizedText, root)
        }
    }

    fun maskProfanity(text: String): String {
        if (StringUtils.isBlank(text)) return text

        var filteredText = text
        forbiddenRoots.forEach { root ->
            val replacement = "*".repeat(root.length)
            filteredText = StringUtils.replaceIgnoreCase(filteredText, root, replacement)
        }
        return filteredText
    }

    private fun prepareText(text: String): String {
        return text.lowercase()
            .replace("0", "o")
            .replace("1", "i")
            .replace("3", "e")
            .replace("4", "a")
            .replace("5", "s")
            .replace("7", "t")
            .replace("8", "b")
            .replace(Regex("[^a-z]"), "")
    }
}