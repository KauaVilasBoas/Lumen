using Lumen.Infrastructure.Security;
using Lumen.Domain.Security;
using FluentAssertions;
using NSubstitute;

namespace Lumen.UnitTests.Application.Security;

public sealed class PasswordValidatorTests
{
    private const string DefaultEmail = "alice@example.com";
    private const string DefaultUsername = "alice";
    private const string StrongPassword = "Str0ng!Passw0rd-2026";

    private readonly IPwnedPasswordsClient _pwnedPasswordsClient = Substitute.For<IPwnedPasswordsClient>();

    public PasswordValidatorTests()
    {
        _pwnedPasswordsClient.IsPwnedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithStrongUniquePassword_PassesAllRules()
    {
        var validator = new PasswordValidator(_pwnedPasswordsClient);

        var result = await validator.ValidatePasswordAsync(Context(StrongPassword));

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithElevenCharacterPassword_FailsLengthRule()
    {
        var validator = new PasswordValidator(_pwnedPasswordsClient);
        // 11 characters but otherwise meets every other rule.
        var password = "Aa1!aaaaaaa";

        var result = await validator.ValidatePasswordAsync(Context(password));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("A senha deve ter no mínimo 12 caracteres.");
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithTwelveCharacterPassword_PassesLengthRule()
    {
        var validator = new PasswordValidator(_pwnedPasswordsClient);
        // Exactly 12 characters, otherwise strong.
        var password = "Aa1!aaaaaaab";

        var result = await validator.ValidatePasswordAsync(Context(password));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithoutUppercase_FailsUppercaseRule()
    {
        var validator = new PasswordValidator(_pwnedPasswordsClient);

        var result = await validator.ValidatePasswordAsync(Context("str0ng!passw0rd-2026"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("A senha deve conter pelo menos uma letra maiúscula.");
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithoutLowercase_FailsLowercaseRule()
    {
        var validator = new PasswordValidator(_pwnedPasswordsClient);

        var result = await validator.ValidatePasswordAsync(Context("STR0NG!PASSW0RD-2026"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("A senha deve conter pelo menos uma letra minúscula.");
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithoutDigit_FailsDigitRule()
    {
        var validator = new PasswordValidator(_pwnedPasswordsClient);

        var result = await validator.ValidatePasswordAsync(Context("Strong!Password-MMXXVI"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("A senha deve conter pelo menos um dígito.");
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithoutSpecialCharacter_FailsSpecialRule()
    {
        var validator = new PasswordValidator(_pwnedPasswordsClient);

        var result = await validator.ValidatePasswordAsync(Context("Str0ngPassw0rd2026"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("A senha deve conter pelo menos um caractere especial.");
    }

    [Theory]
    [InlineData("alice@example.com")]
    [InlineData("ALICE@EXAMPLE.COM")]
    [InlineData("Alice@Example.com")]
    public async Task ValidatePasswordAsync_WhenPasswordEqualsEmailCaseInsensitive_FailsIdentityRule(string password)
    {
        var validator = new PasswordValidator(_pwnedPasswordsClient);

        var result = await validator.ValidatePasswordAsync(new PasswordValidationContext(password, DefaultEmail, DefaultUsername));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("A senha não pode ser igual ao seu email/username.");
    }

    [Theory]
    [InlineData("alice12345!A")]
    [InlineData("ALICE12345!A")]
    public async Task ValidatePasswordAsync_WhenPasswordEqualsUsernameCaseInsensitive_FailsIdentityRule(string password)
    {
        var validator = new PasswordValidator(_pwnedPasswordsClient);
        var username = "alice12345!A";

        var result = await validator.ValidatePasswordAsync(new PasswordValidationContext(password, DefaultEmail, username));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("A senha não pode ser igual ao seu email/username.");
    }

    [Fact]
    public async Task ValidatePasswordAsync_WhenStrongPasswordIsPwned_FailsHibpRule()
    {
        _pwnedPasswordsClient.IsPwnedAsync(StrongPassword, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var validator = new PasswordValidator(_pwnedPasswordsClient);

        var result = await validator.ValidatePasswordAsync(Context(StrongPassword));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Esta senha aparece em vazamentos públicos conhecidos. Escolha outra.");
    }

    [Fact]
    public async Task ValidatePasswordAsync_WhenStrongPasswordIsNotPwned_PassesAllRules()
    {
        _pwnedPasswordsClient.IsPwnedAsync(StrongPassword, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var validator = new PasswordValidator(_pwnedPasswordsClient);

        var result = await validator.ValidatePasswordAsync(Context(StrongPassword));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePasswordAsync_WhenStructuralRulesFail_DoesNotCallHibp()
    {
        var validator = new PasswordValidator(_pwnedPasswordsClient);

        // Too short, missing uppercase, digit and special — every structural rule fails.
        await validator.ValidatePasswordAsync(Context("short"));

        await _pwnedPasswordsClient.DidNotReceive().IsPwnedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidatePasswordAsync_AccumulatesAllStructuralErrors()
    {
        var validator = new PasswordValidator(_pwnedPasswordsClient);

        var result = await validator.ValidatePasswordAsync(Context("short"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().BeEquivalentTo(new[]
        {
            "A senha deve ter no mínimo 12 caracteres.",
            "A senha deve conter pelo menos uma letra maiúscula.",
            "A senha deve conter pelo menos um dígito.",
            "A senha deve conter pelo menos um caractere especial.",
        });
    }

    [Theory]
    [InlineData('!')]
    [InlineData('@')]
    [InlineData('#')]
    [InlineData('$')]
    [InlineData('^')]
    [InlineData('&')]
    [InlineData('*')]
    [InlineData('(')]
    [InlineData(')')]
    [InlineData('-')]
    [InlineData('_')]
    [InlineData('=')]
    [InlineData('+')]
    [InlineData('[')]
    [InlineData(']')]
    [InlineData('{')]
    [InlineData('}')]
    [InlineData(';')]
    [InlineData(':')]
    [InlineData('\'')]
    [InlineData('"')]
    [InlineData(',')]
    [InlineData('.')]
    [InlineData('<')]
    [InlineData('>')]
    [InlineData('/')]
    [InlineData('?')]
    [InlineData('\\')]
    [InlineData('|')]
    [InlineData('`')]
    [InlineData('~')]
    [InlineData('%')]
    public async Task ValidatePasswordAsync_AcceptsEachAllowedSpecialCharacter(char special)
    {
        var validator = new PasswordValidator(_pwnedPasswordsClient);
        var password = $"Strong0Password{special}xx";

        var result = await validator.ValidatePasswordAsync(Context(password));

        result.IsValid.Should().BeTrue($"'{special}' is in the allowed special-character set");
    }

    private static PasswordValidationContext Context(string password)
        => new(password, DefaultEmail, DefaultUsername);
}
