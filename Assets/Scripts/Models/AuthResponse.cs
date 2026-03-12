using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class AuthResponse
    {
        public string accessToken;
        public string accessTokenExpiresAt;
        public string refreshToken;
        public string refreshTokenExpiresAt;
    }
}
