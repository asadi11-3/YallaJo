namespace Auth.Application.Interfaces;

public interface IOtpService
{
    string Generate();
    string Hash(string plainOtp);
    bool Verify(string plainOtp, string hashedOtp);
}
