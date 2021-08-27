namespace goOfflineE.Services
{
    using AutoMapper;
    using goOfflineE.Models;
    using goOfflineE.Repository;
    using goOfflineE.Services.Contract;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="CommonService" />.
    /// </summary>
    public class CommonService : ICommonService
    {
        /// <summary>
        /// Defines the _httpContextAccessor.
        /// </summary>
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Defines the _tableStorage.
        /// </summary>
        private readonly ITableStorage _tableStorage;

        /// <summary>
        /// Defines the _mapper.
        /// </summary>
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonService"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The httpContextAccessor<see cref="IHttpContextAccessor"/>.</param>
        /// <param name="tableStorage">The tableStorage<see cref="ITableStorage"/>.</param>
        /// <param name="mapper">The mapper<see cref="IMapper"/>.</param>
        public CommonService(
            IHttpContextAccessor httpContextAccessor,
            ITableStorage tableStorage,
             IMapper mapper
            )
        {
            _tableStorage = tableStorage;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
        }

        /// <summary>
        /// The GetTenant.
        /// </summary>
        /// <returns>The <see cref="Task{Tenant}"/>.</returns>
        public async Task<Tenant> GetTenant()
        {
            var request = _httpContextAccessor.HttpContext.Request;
            if (request != null && request.Headers.ContainsKey("Tenant"))
            {
                string tenantId = request.Headers["Tenant"].ToString();

                if (!String.IsNullOrEmpty(tenantId))
                {
                    var dataTentants = await _tableStorage.GetAllAsync<Entites.Tenant>("Tenants");
                    var tenant = dataTentants.ToList().FirstOrDefault(t => t.RowKey == tenantId);
                    return _mapper.Map<Tenant>(tenant);
                }
            }

            return null;
        }
    }
}
