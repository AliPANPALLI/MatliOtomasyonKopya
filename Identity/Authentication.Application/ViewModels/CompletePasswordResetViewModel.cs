namespace Authentication.Application.ViewModels;
public sealed class CompletePasswordResetViewModel
{
    public string ChallengeId { get; set; } = "";
    public string Code { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string NewPasswordAgain { get; set; } = "";
}
