package com.example.core_models.dto

data class InviteReceivedDto(
    val tournamentId: Int,
    val opponentId: Int,
    val opponentName: String,
    val opponentRating: Int,
    val topicName: String,
    val expiresInSeconds: Int = 60
)

data class TournamentStartedDto(
    val tournamentId: Int,
    val questions: List<PvpQuestionDto>
)

data class OpponentInfoDto(
    val opponentId: Int,
    val opponentRating: Int,
    val opponentName: String = ""
)

data class AnswerResultDto(
    val questionId: Int,
    val isCorrect: Boolean,
    val correctOptionId: Int,
    val points: Int
)

data class TournamentFinishedDto(
    val tournamentId: Int,
    val result: String,
    val scores: ScoresDto
)

data class ScoresDto(
    val player1: PlayerScoreDto,
    val player2: PlayerScoreDto
)

data class PlayerScoreDto(
    val userId: Int,
    val score: Int,
    val ratingDelta: Int
)

data class TournamentCancelledDto(
    val tournamentId: Int,
    val reason: String
)

data class PvpQuestionDto(
    val id: Int,
    val text: String,
    val difficulty: String,
    val points: Int,
    val options: List<PvpAnswerOptionDto>
)

data class PvpAnswerOptionDto(
    val id: Int,
    val text: String
)

data class SubmitAnswerRequest(
    val tournamentId: Int,
    val questionId: Int,
    val answerOptionId: Int
)

data class OpponentAcceptedDto(
    val tournamentId: Int,
    val opponentName: String
)