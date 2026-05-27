package com.example.pvp.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.SportsEsports
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.example.core_models.InviteState
import com.example.core_models.dto.InviteReceivedDto

@Composable
fun InviteDialog(
    inviteState: InviteState,
    onAccept: (tournamentId: Int) -> Unit,
    onDecline: (tournamentId: Int) -> Unit
) {
    val invite = when (inviteState) {
        is InviteState.Pending -> inviteState.invite
        is InviteState.AcceptedWaiting -> inviteState.invite
        is InviteState.OpponentAccepted -> inviteState.invite
        else -> return
    }

    var secondsLeft by remember(invite.tournamentId) {
        mutableIntStateOf(invite.expiresInSeconds)
    }

    LaunchedEffect(invite.tournamentId) {
        while (secondsLeft > 0) {
            kotlinx.coroutines.delay(1000)
            secondsLeft--
        }
        if (inviteState is InviteState.Pending) {
            onDecline(invite.tournamentId)
        }
    }

    AlertDialog(
        onDismissRequest = {  },
        shape = RoundedCornerShape(24.dp),
        title = {
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier.fillMaxWidth()
            ) {
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .size(64.dp)
                        .background(MaterialTheme.colorScheme.primaryContainer, CircleShape)
                ) {
                    Icon(
                        Icons.Default.SportsEsports,
                        contentDescription = null,
                        modifier = Modifier.size(32.dp),
                        tint = MaterialTheme.colorScheme.primary
                    )
                }
                Spacer(Modifier.height(12.dp))
                Text(
                    "Вызов на дуэль!",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold
                )
            }
        },
        text = {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                OpponentInfoSection(invite, inviteState)
                Spacer(Modifier.height(16.dp))
                CountdownTimer(secondsLeft = secondsLeft, total = invite.expiresInSeconds)
            }
        },
        confirmButton = {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 8.dp, vertical = 4.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                val isAcceptedByMe = inviteState is InviteState.AcceptedWaiting

                Button(
                    onClick = { onAccept(invite.tournamentId) },
                    enabled = !isAcceptedByMe,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(52.dp),
                    shape = RoundedCornerShape(12.dp),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = if (isAcceptedByMe) Color.Gray else Color(0xFF4CAF50)
                    )
                ) {
                    if (isAcceptedByMe) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(20.dp),
                            color = Color.White,
                            strokeWidth = 2.dp
                        )
                        Spacer(Modifier.width(12.dp))
                        Text("Ожидание соперника...")
                    } else {
                        Text("Принять", fontWeight = FontWeight.SemiBold)
                    }
                }

                if (!isAcceptedByMe) {
                    OutlinedButton(
                        onClick = { onDecline(invite.tournamentId) },
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(48.dp),
                        shape = RoundedCornerShape(12.dp)
                    ) {
                        Text("Отклонить")
                    }
                }
            }
        },
        dismissButton = {}
    )
}

@Composable
private fun OpponentInfoSection(invite: InviteReceivedDto, state: InviteState) {
    Card(
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surfaceVariant
        ),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                invite.opponentName.ifEmpty { "Игрок #${invite.opponentId}" },
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.SemiBold
            )

            if (state is InviteState.OpponentAccepted) {
                Spacer(Modifier.height(4.dp))
                Text(
                    "ГОТОВ К БОЮ",
                    style = MaterialTheme.typography.labelSmall,
                    color = Color(0xFF4CAF50),
                    fontWeight = FontWeight.Black
                )
            }

            Spacer(Modifier.height(4.dp))
            Text(
                "${invite.opponentRating} ELO",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.primary
            )
            Spacer(Modifier.height(8.dp))
            Text(
                "Тема: ${invite.topicName}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                textAlign = TextAlign.Center
            )
        }
    }
}

@Composable
private fun CountdownTimer(secondsLeft: Int, total: Int) {
    val progress = secondsLeft.toFloat() / total
    val color = when {
        secondsLeft > 30 -> MaterialTheme.colorScheme.primary
        secondsLeft > 10 -> Color(0xFFF57C00)
        else             -> MaterialTheme.colorScheme.error
    }

    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(
            "$secondsLeft с",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold,
            color = color
        )
        Spacer(Modifier.height(6.dp))
        LinearProgressIndicator(
            progress = { progress },
            modifier = Modifier
                .fillMaxWidth()
                .height(6.dp),
            color = color,
            trackColor = MaterialTheme.colorScheme.outlineVariant
        )
    }
}