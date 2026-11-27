using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ChasingLight.Api.DTOs.Auth;
using ChasingLight.Api.DTOs.Profiles;
using ChasingLight.Api.DTOs.Records;

namespace ChasingLight.Api.Tests.Unit.Validation;

public class RequestDtoValidationTests
{
    private static List<ValidationResult> ValidateObjectProperties(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    private static List<ValidationResult> ValidateConstructorParameters<T>(params object?[] args)
    {
        var ctor = typeof(T).GetConstructors().Single();
        var parameters = ctor.GetParameters();
        var results = new List<ValidationResult>();

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var attrs = param.GetCustomAttributes<ValidationAttribute>().ToArray();
            if (attrs.Length == 0) continue;

            var context = new ValidationContext(args) { MemberName = param.Name };
            Validator.TryValidateValue(args[i]!, context, results, attrs);
        }

        return results;
    }

    [Theory]
    [InlineData(1, 1, (short)1, true)]
    [InlineData(120, 45, (short)3, true)]
    [InlineData(0, 10, (short)2, false)]
    [InlineData(10, 0, (short)2, false)]
    [InlineData(10, 5, (short)0, false)]
    [InlineData(10, 5, (short)4, false)]
    public void CompleteStageRequest_ValidatesRanges(int timeSeconds, int steps, short stars, bool expectedValid)
    {
        var dto = new CompleteStageRequest
        {
            StageId = Guid.NewGuid(),
            TimeDoneSeconds = timeSeconds,
            StepCount = steps,
            StarsEarned = stars
        };

        var errors = ValidateObjectProperties(dto);

        Assert.Equal(expectedValid, errors.Count == 0);
    }

    [Theory]
    [InlineData(1, 1, (short)1, true)]
    [InlineData(0, 10, (short)2, false)]
    [InlineData(10, 0, (short)2, false)]
    [InlineData(10, 5, (short)4, false)]
    public void SyncRecordItem_ValidatesRanges(int timeSeconds, int steps, short stars, bool expectedValid)
    {
        var item = new SyncRecordItem
        {
            StageId = Guid.NewGuid(),
            TimeDoneSeconds = timeSeconds,
            StepCount = steps,
            StarsEarned = stars
        };

        var errors = ValidateObjectProperties(item);

        Assert.Equal(expectedValid, errors.Count == 0);
    }

    [Theory]
    [InlineData("A", false)]
    [InlineData("AB", true)]
    [InlineData("", false)]
    public void UpdateProfileRequest_ValidatesDisplayNameLength(string displayName, bool expectedValid)
    {
        var errors = ValidateConstructorParameters<UpdateProfileRequest>(displayName);

        Assert.Equal(expectedValid, errors.Count == 0);
    }

    [Theory]
    [InlineData("user@example.com", "12345678", true)]
    [InlineData("invalid-email", "12345678", false)]
    [InlineData("user@example.com", "123456", false)]
    [InlineData("user@example.com", "123456789", false)]
    public void VerifyRecoveryOtpRequest_ValidatesEmailAndEightDigitOtp(string email, string otp, bool expectedValid)
    {
        var errors = ValidateConstructorParameters<VerifyRecoveryOtpRequest>(email, otp);

        Assert.Equal(expectedValid, errors.Count == 0);
    }
}
