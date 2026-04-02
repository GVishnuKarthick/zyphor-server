namespace ZyphorAPI.DTOs;

public class ResetPasswordDto
{
    public string PhoneNumber { get; set; }
    public string Otp { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}