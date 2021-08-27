namespace goOfflineE.Services.Contract
{
    using goOfflineE.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="ICommonService" />.
    /// </summary>
    public interface ICommonService
    {
        /// <summary>
        /// The Get.
        /// </summary>
        /// <param name="tentantId">The tentantId<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{Tenant}"/>.</returns>
        Task<Tenant> GetTenant();
    }
}
