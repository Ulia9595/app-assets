package com.example.core_ui

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.example.core_models.PasswordStrength

@Composable
fun PasswordStrengthIndicator(strength: PasswordStrength) {
    val color = when (strength) {
        PasswordStrength.EMPTY -> Color.LightGray
        PasswordStrength.WEAK -> Color.Red
        PasswordStrength.MEDIUM -> Color(0xFFFFC107)
        PasswordStrength.STRONG -> Color(0xFF4CAF50)
    }

    Column(modifier = Modifier.fillMaxWidth().padding(vertical = 8.dp)) {
        LinearProgressIndicator(
            progress = { if (strength.score == 0) 0f else strength.score / 3f },
            modifier = Modifier.fillMaxWidth().height(6.dp),
            color = color,
            trackColor = MaterialTheme.colorScheme.surfaceVariant
        )
        if (strength != PasswordStrength.EMPTY) {
            Text(
                text = "Сложность: ${strength.label}",
                style = MaterialTheme.typography.labelSmall,
                color = color,
                modifier = Modifier.padding(top = 4.dp)
            )
        }
    }
}