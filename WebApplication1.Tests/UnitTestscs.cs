using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WebApplication1.Services;
using Xunit;

namespace WebApplication1.Tests.Unit
{
    public class PasswordValidatorTests
    {
        private readonly PasswordValidator _validator = new();

        [Fact]
        [Trait("Category", "Unit")]
        public void ValidPassword_AllRequirementsMet_IsValid()
        {
            var result = _validator.GetDetailedValidation("TestPass1!");

            result.IsValid.Should().BeTrue();
            result.Requirements.Should().AllSatisfy(r => r.Value.Should().BeTrue());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Password_TooShort_IsNotValid()
        {
            var result = _validator.GetDetailedValidation("Te1!");

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Password_NoUppercase_IsNotValid()
        {
            var result = _validator.GetDetailedValidation("testpass1!");

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Password_NoDigit_IsNotValid()
        {
            var result = _validator.GetDetailedValidation("TestPass!");

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Password_NoSpecialChar_IsNotValid()
        {
            var result = _validator.GetDetailedValidation("TestPass1");

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void EmptyPassword_Strength_IsEmpty()
        {
            var strength = _validator.CalculateStrength("");

            strength.Should().Be(PasswordStrength.Empty);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void WeakPassword_OnlyLowercase_Strength_IsWeak()
        {
            var strength = _validator.CalculateStrength("weakpassword");

            strength.Should().Be(PasswordStrength.Weak);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void StrongPassword_AllCriteria_Strength_IsStrong()
        {
            var strength = _validator.CalculateStrength("TestPass1!");

            strength.Should().Be(PasswordStrength.Strong);
        }
    }

    public class CodeSafetyValidatorTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void SafeCode_HelloWorld_IsAllowed()
        {
            var result = CodeSafetyValidator.Validate("fun main() { println(\"Hello\") }");

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void DangerousCode_SystemExit_IsBlocked()
        {
            var result = CodeSafetyValidator.Validate("fun main() { System.exit(0) }");

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void DangerousCode_RuntimeExec_IsBlocked()
        {
            var result = CodeSafetyValidator.Validate("fun main() { Runtime.getRuntime().exec(\"ls\") }");

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void DangerousCode_ProcessBuilder_IsBlocked()
        {
            var result = CodeSafetyValidator.Validate(
                "fun main() { ProcessBuilder(\"rm\", \"-rf\").start() }");

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void DangerousCode_FileWrite_IsBlocked()
        {
            var result = CodeSafetyValidator.Validate(
                "import java.io.File\nfun main() { File(\"/etc/passwd\").writeText(\"hacked\") }");

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Code_ExceedsMaxLength_IsBlocked()
        {
            var longCode = new string('a', 10_001);

            var result = CodeSafetyValidator.Validate(longCode);

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Code_ExactlyAtMaxLength_IsAllowed()
        {
            var code = "fun main() { println(\"ok\") }";
            code += new string(' ', 10_000 - code.Length);

            var result = CodeSafetyValidator.Validate(code);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void DangerousCode_Reflection_IsBlocked()
        {
            var result = CodeSafetyValidator.Validate(
                "fun main() { Class.forName(\"java.lang.Runtime\") }");

            result.IsValid.Should().BeFalse();
        }
    }

    public class MatchmakingServiceTests
    {
        private static MatchmakingEntry MakeEntry(int userId, int rating, int topicId = 1,
            string connId = "conn") =>
            new(userId, rating, topicId, connId, DateTime.UtcNow);

        [Fact]
        [Trait("Category", "Unit")]
        public void SinglePlayer_Enqueue_ReturnsNull_NoOpponent()
        {
            var svc = new MatchmakingService();
            var entry = MakeEntry(1, 500);

            var opponent = svc.Enqueue(entry);

            opponent.Should().BeNull();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void TwoPlayers_SameTopic_SimilarRating_AreMatched()
        {
            var svc = new MatchmakingService();
            var p1 = MakeEntry(1, 500, topicId: 1);
            var p2 = MakeEntry(2, 520, topicId: 1);

            svc.Enqueue(p1);
            var opponent = svc.Enqueue(p2);

            opponent.Should().NotBeNull();
            opponent!.UserId.Should().Be(1);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void TwoPlayers_DifferentTopics_AreNotMatched()
        {
            var svc = new MatchmakingService();
            svc.Enqueue(MakeEntry(1, 500, topicId: 1));
            var opponent = svc.Enqueue(MakeEntry(2, 510, topicId: 2));

            opponent.Should().BeNull();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void TwoPlayers_RatingDiffAbove200_AreNotMatched()
        {
            var svc = new MatchmakingService();
            svc.Enqueue(MakeEntry(1, 500, topicId: 1));
            var opponent = svc.Enqueue(MakeEntry(2, 701, topicId: 1));

            opponent.Should().BeNull();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void TwoPlayers_RatingDiffExactly200_AreMatched()
        {
            var svc = new MatchmakingService();
            svc.Enqueue(MakeEntry(1, 500, topicId: 1));
            var opponent = svc.Enqueue(MakeEntry(2, 700, topicId: 1));

            opponent.Should().NotBeNull();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void AfterMatch_BothPlayersRemovedFromQueue()
        {
            var svc = new MatchmakingService();
            svc.Enqueue(MakeEntry(1, 500, topicId: 1));
            svc.Enqueue(MakeEntry(2, 510, topicId: 1));

            var opponent = svc.Enqueue(MakeEntry(3, 500, topicId: 1));

            opponent.Should().BeNull();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Remove_PlayerInQueue_IsQueued_ReturnsFalse()
        {
            var svc = new MatchmakingService();
            svc.Enqueue(MakeEntry(1, 500, topicId: 1));

            svc.Remove(1);

            svc.IsQueued(1).Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void IsQueued_AfterEnqueue_ReturnsTrue()
        {
            var svc = new MatchmakingService();
            svc.Enqueue(MakeEntry(42, 600, topicId: 1));

            svc.IsQueued(42).Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void IsQueued_NotInQueue_ReturnsFalse()
        {
            var svc = new MatchmakingService();

            svc.IsQueued(99).Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void UpdateConnectionId_PlayerInQueue_ConnectionUpdated()
        {
            var svc = new MatchmakingService();
            svc.Enqueue(MakeEntry(1, 500, connId: "old-conn"));

            svc.UpdateConnectionId(1, "new-conn");

            var opponent = svc.Enqueue(MakeEntry(2, 510, topicId: 1, connId: "p2-conn"));
            opponent!.ConnectionId.Should().Be("new-conn");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ConcurrentEnqueue_TwoPlayers_NoRaceCondition()
        {
            var svc = new MatchmakingService();
            int matchCount = 0;

            var t1 = Task.Run(() =>
            {
                var result = svc.Enqueue(MakeEntry(1, 500, topicId: 1, connId: "c1"));
                if (result != null) Interlocked.Increment(ref matchCount);
            });

            var t2 = Task.Run(() =>
            {
                var result = svc.Enqueue(MakeEntry(2, 500, topicId: 1, connId: "c2"));
                if (result != null) Interlocked.Increment(ref matchCount);
            });

            Task.WaitAll(t1, t2);

            matchCount.Should().Be(1);
        }
    }

    public class JwtServiceTests
    {
        private static JwtService CreateService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:Key"] = "8e7a6b5c4d3e2f1a9b8c7d6e5f4a3b2c1d0e9f8a7b6c5d4e3f2a1b0c9d8e7f6a5",
                    ["JwtSettings:Issuer"] = "GameAuthServer",
                    ["JwtSettings:Audience"] = "GameAuthApp",
                    ["JwtSettings:ExpireDays"] = "7"
                })
                .Build();

            return new JwtService(config);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GenerateToken_ReturnsNonEmptyString()
        {
            var svc = CreateService();
            var token = svc.GenerateToken(1, "test@yandex.ru", "uid-123", "player");

            token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GenerateToken_ValidToken_CanBeValidated()
        {
            var svc = CreateService();
            var token = svc.GenerateToken(1, "test@yandex.ru", "uid-123", "player");
            var principal = svc.ValidateToken(token);

            principal.Should().NotBeNull();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ValidateToken_InvalidToken_ReturnsNull()
        {
            var svc = CreateService();
            var principal = svc.ValidateToken("invalid.token.value");

            principal.Should().BeNull();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetUserIdFromToken_ValidToken_ReturnsCorrectId()
        {
            var svc = CreateService();
            var token = svc.GenerateToken(42, "test@yandex.ru", "uid-42", "player");
            var userId = svc.GetUserIdFromToken(token);

            userId.Should().Be(42);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetUserIdFromToken_InvalidToken_ReturnsNull()
        {
            var svc = CreateService();
            var userId = svc.GetUserIdFromToken("garbage");

            userId.Should().BeNull();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GeneratedToken_ContainsCorrectRole()
        {
            var svc = CreateService();
            var token = svc.GenerateToken(1, "admin@yandex.ru", "uid-1", "admin");
            var principal = svc.ValidateToken(token);
            var role = principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            role.Should().Be("admin");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetUserRoleFromToken_ValidToken_ReturnsRole()
        {
            var svc = CreateService();
            var token = svc.GenerateToken(5, "u@yandex.ru", "uid-5", "player");
            var role = svc.GetUserRoleFromToken(token);

            role.Should().Be("player");
        }
    }

    public class KotlinErrorTranslatorTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void EmptyError_ReturnsEmpty()
        {
            var result = KotlinErrorTranslator.Translate("", "", null, null);

            result.Should().NotBeNull();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void UnresolvedReference_IsTranslated_ToRussian()
        {
            var error = "error: unresolved reference: println2";

            var result = KotlinErrorTranslator.Translate(error, "", null, null);

            result.Should().NotBe(error);
            result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void TypeMismatch_IsTranslated()
        {
            var error = "error: type mismatch: inferred type is String but Int was expected";

            var result = KotlinErrorTranslator.Translate(error, "", null, null);

            result.Should().NotBe(error);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void UnknownError_ReturnsOriginalText()
        {
            var error = "some completely unknown compiler message xyz";

            var result = KotlinErrorTranslator.Translate(error, "", null, null);

            result.Should().NotBeNullOrEmpty();
        }
    }
}