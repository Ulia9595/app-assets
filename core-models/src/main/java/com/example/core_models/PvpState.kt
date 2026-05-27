package com.example.core_models

import com.example.core_models.dto.InviteReceivedDto
import com.example.core_models.dto.PvpQuestionDto
import com.example.core_models.dto.TournamentFinishedDto

sealed class PvpConnectionState {
    object Disconnected : PvpConnectionState()
    object Connecting   : PvpConnectionState()
    object Connected    : PvpConnectionState()
    data class Error(val message: String) : PvpConnectionState()
}

sealed class InviteState {
    object None : InviteState()
    data class Pending(val invite: InviteReceivedDto) : InviteState()
    data class AcceptedWaiting(val invite: InviteReceivedDto) : InviteState()
    data class OpponentAccepted(val invite: InviteReceivedDto, val opponentName: String) : InviteState()
}

sealed class PvpLobbyState {
    object Idle               : PvpLobbyState()
    object Searching          : PvpLobbyState()
    object InviteSent         : PvpLobbyState()
    object WaitingForOpponent : PvpLobbyState()
    data class InviteExpired(val reason: String) : PvpLobbyState()
}

data class TournamentState(
    val tournamentId: Int = 0,
    val opponentId: Int = 0,
    val opponentName: String = "",
    val opponentRating: Int = 0,
    val questions: List<PvpQuestionDto> = emptyList(),
    val currentQuestionIndex: Int = 0,
    val myAnswers: Map<Int, AnswerState> = emptyMap(),
    val myTotalScore: Int = 0,
    val timerSeconds: Int = 45,
    val isFinished: Boolean = false
) {
    val currentQuestion: PvpQuestionDto?
        get() = questions.getOrNull(currentQuestionIndex)

    val isLastQuestion: Boolean
        get() = currentQuestionIndex >= questions.size - 1

    val answeredCount: Int
        get() = myAnswers.size
}

data class AnswerState(
    val selectedOptionId: Int,
    val isCorrect: Boolean,
    val correctOptionId: Int,
    val pointsEarned: Int
)

data class TournamentResultState(
    val tournamentId: Int,
    val myUserId: Int,
    val result: TournamentFinishedDto,
    val myScore: Int,
    val opponentScore: Int,
    val myRatingDelta: Int,
    val opponentRatingDelta: Int,
    val outcome: MatchOutcome
)

enum class MatchOutcome { WIN, LOSE, DRAW }