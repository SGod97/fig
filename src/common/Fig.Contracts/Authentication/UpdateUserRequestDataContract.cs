namespace Fig.Contracts.Authentication
{
    public class UpdateUserRequestDataContract
    {
        public string? Username { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public Role? Role { get; set; }
        
        public string? ClientFilter { get; set; }

        public string? Password { get; set; }
    }
}