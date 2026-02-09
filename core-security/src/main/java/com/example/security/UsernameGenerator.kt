package com.example.security

import net.datafaker.Faker
import java.util.Locale

object UsernameGenerator {
    private val faker = Faker(Locale.ENGLISH)

    fun generate(): String {
        val w1 = faker.superhero().descriptor().replace(" ", "")
        val w2 = faker.animal().name().replace(" ", "")
        val number = (1..99).random()

        val rawName = if ((0..1).random() == 0) {
            "$w1$number $w2"
        } else {
            "$w1 $w2$number"
        }

        val finalName = rawName.split(" ").joinToString(" ") { word ->
            word.replaceFirstChar { if (it.isLowerCase()) it.titlecase(Locale.ENGLISH) else it.toString() }
        }

        return if (finalName.length > 30) {
            finalName.take(30).trim()
        } else {
            finalName
        }
    }
}