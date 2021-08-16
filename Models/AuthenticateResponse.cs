namespace goOfflineE.Models
{
    /// <summary>
    /// Defines the <see cref="AuthenticateResponse" />.
    /// </summary>
    public class AuthenticateResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticateResponse"/> class.
        /// </summary>
        public AuthenticateResponse()
        {
            Tenant = new Tenant();
        }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the FirstName.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the LastName.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the Email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the EnrolmentNo.
        /// </summary>
        public string EnrolmentNo { get; set; }

        /// <summary>
        /// Gets or sets the Role.
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the Token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the SchoolId.
        /// </summary>
        public string SchoolId { get; set; }

        /// <summary>
        /// Gets or sets the ClassId.
        /// </summary>
        public string ClassId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ForceChangePasswordNextLogin.
        /// </summary>
        public bool ForceChangePasswordNextLogin { get; set; }

        /// <summary>
        /// Gets or sets the Tenant.
        /// </summary>
        public Tenant Tenant { get; set; }
    }
}
