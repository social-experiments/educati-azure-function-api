namespace goOfflineE.Entites
{
    using System;

    /// <summary>
    /// Defines the <see cref="User" />.
    /// </summary>
    public class User : BaseEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// </summary>
        /// <param name="schoolId">The schoolId<see cref="string"/>.</param>
        /// <param name="userId">The userId<see cref="string"/>.</param>
        public User(string schoolId, string userId)
        {
            this.PartitionKey = schoolId;
            this.RowKey = userId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// </summary>
        public User()
        {
        }

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
        /// Gets or sets the PasswordHash.
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// Gets or sets the Role.
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the Verified.
        /// </summary>
        public DateTime? Verified { get; set; }

        /// <summary>
        /// Gets a value indicating whether IsVerified.
        /// </summary>
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;

        /// <summary>
        /// Gets or sets the PasswordReset.
        /// </summary>
        public DateTime? PasswordReset { get; set; }

        /// <summary>
        /// Gets or sets the ProfileStoragePath.
        /// </summary>
        public string ProfileStoragePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ForceChangePasswordNextLogin.
        /// </summary>
        public bool ForceChangePasswordNextLogin { get; set; }

        /// <summary>
        /// Gets or sets the NotificationToken.
        /// </summary>
        public string NotificationToken { get; set; }

        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the EnrolmentNo.
        /// </summary>
        public string EnrolmentNo { get; set; }
    }
}
