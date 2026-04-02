using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ZyphorAPI.Data;
using ZyphorAPI.DTOs;
using ZyphorAPI.Models;
using ZyphorAPI.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
namespace ZyphorAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MongoDbContext _context;
    private readonly JwtService _jwtService;
    private readonly EmailService _emailService;

    public AuthController(
        MongoDbContext context,
        JwtService jwtService,
        EmailService emailService)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    // ================= REGISTER =================

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var existingUser = await _context.Users
            .Find(u => u.Email == dto.Email)
            .FirstOrDefaultAsync();

        if (existingUser != null)
            return BadRequest("Email already exists");

        var otp = new Random().Next(100000, 999999).ToString();

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            EmailOtp = otp,
            EmailOtpExpiry = DateTime.UtcNow.AddMinutes(10),
            IsEmailVerified = false,
            RefreshTokens = new List<RefreshToken>()
        };

        await _context.Users.InsertOneAsync(user);

       _ = Task.Run(() => _emailService.SendOtpEmail(user.Email, otp));

        return Ok("OTP sent to email. Please verify to complete registration.");
    }

    // ================= VERIFY EMAIL =================

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailDto dto)
    {
        var user = await _context.Users
            .Find(x => x.Email == dto.Email)
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound("User not found");

        if (user.EmailOtp != dto.Otp ||
            user.EmailOtpExpiry < DateTime.UtcNow)
            return BadRequest("Invalid or expired OTP");

        var update = Builders<User>.Update
            .Set(x => x.IsEmailVerified, true)
            .Unset(x => x.EmailOtp)
            .Unset(x => x.EmailOtpExpiry);

        await _context.Users.UpdateOneAsync(x => x.Id == user.Id, update);

        return Ok("Email verified successfully");
    }

    // ================= LOGIN =================

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users
            .Find(u => u.Email == dto.Email)
            .FirstOrDefaultAsync();

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        if (!user.IsEmailVerified)
            return BadRequest("Please verify your email first");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Revoked = false
        });

        await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

        return Ok(new
        {
            accessToken,
            refreshToken
        });
    }

    // ================= REFRESH TOKEN =================

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenDto dto)
    {
        var user = await _context.Users
            .Find(u => u.RefreshTokens
                .Any(rt => rt.Token == dto.RefreshToken && !rt.Revoked))
            .FirstOrDefaultAsync();

        if (user == null)
            return Unauthorized("Invalid refresh token");

        var oldToken = user.RefreshTokens
            .First(rt => rt.Token == dto.RefreshToken);

        if (oldToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized("Refresh token expired");

        oldToken.Revoked = true;

        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Revoked = false
        });

        await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken
        });
    }

    // ================= FORGOT PASSWORD =================

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var user = await _context.Users
            .Find(x => x.PhoneNumber == dto.PhoneNumber)
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound("User not found");

        var otp = new Random().Next(100000, 999999).ToString();

        var update = Builders<User>.Update
            .Set(x => x.PhoneOtp, otp)
            .Set(x => x.PhoneOtpExpiry, DateTime.UtcNow.AddMinutes(5));

        await _context.Users.UpdateOneAsync(x => x.Id == user.Id, update);

        // Integrate Twilio here
        Console.WriteLine($"Phone OTP: {otp}");

        return Ok("OTP sent to phone");
    }

    // ================= RESET PASSWORD =================

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest("Passwords do not match");

        var user = await _context.Users
            .Find(x => x.PhoneNumber == dto.PhoneNumber)
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound("User not found");

        if (user.PhoneOtp != dto.Otp ||
            user.PhoneOtpExpiry < DateTime.UtcNow)
            return BadRequest("Invalid or expired OTP");

        var update = Builders<User>.Update
            .Set(x => x.PasswordHash,
                BCrypt.Net.BCrypt.HashPassword(dto.NewPassword))
            .Unset(x => x.PhoneOtp)
            .Unset(x => x.PhoneOtpExpiry);

        await _context.Users.UpdateOneAsync(x => x.Id == user.Id, update);

        return Ok("Password reset successfully");
    }
    // ================= RESEND EMAIL OTP =================

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp(ResendOtpDto dto)
    {
    var user = await _context.Users
        .Find(x => x.Email == dto.Email)
        .FirstOrDefaultAsync();

    if (user == null)
        return NotFound("User not found");

    if (user.IsEmailVerified)
        return BadRequest("Email already verified");

    var otp = new Random().Next(100000, 999999).ToString();

    var update = Builders<User>.Update
        .Set(x => x.EmailOtp, otp)
        .Set(x => x.EmailOtpExpiry, DateTime.UtcNow.AddMinutes(10));

    await _context.Users.UpdateOneAsync(x => x.Id == user.Id, update);

    await _emailService.SendOtpEmail(user.Email, otp);

    return Ok("OTP resent successfully");
    }
    // ================= LOGOUT =================
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutDto dto)
    {
    var user = await _context.Users
        .Find(u => u.RefreshTokens
            .Any(rt => rt.Token == dto.RefreshToken && !rt.Revoked))
        .FirstOrDefaultAsync();

    if (user == null)
        return Unauthorized("Invalid refresh token");

    var token = user.RefreshTokens
        .First(rt => rt.Token == dto.RefreshToken);

    token.Revoked = true;

    await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

    return Ok("Logged out successfully");
    }
    
    [HttpGet("debug-users")]
public async Task<IActionResult> DebugUsers()
{
    var users = await _context.Users.Find(_ => true).ToListAsync();
    return Ok(users);
}

}