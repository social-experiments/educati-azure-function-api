namespace goOfflineE.Services
{
    using goOfflineE.Helpers;
    using goOfflineE.Models;
    using goOfflineE.Repository;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="SettingService" />.
    /// </summary>
    public class SettingService : ISettingService
    {
        /// <summary>
        /// Defines the _tableStorage.
        /// </summary>
        private readonly ITableStorage _tableStorage;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingService"/> class.
        /// </summary>
        /// <param name="tableStorage">The tableStorage<see cref="ITableStorage"/>.</param>
        public SettingService(ITableStorage tableStorage)
        {
            _tableStorage = tableStorage;
        }

        /// <summary>
        /// The GetMenus.
        /// </summary>
        /// <param name="roleName">The roleName<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{IEnumerable{AssociateMenu}}"/>.</returns>
        public async Task<IEnumerable<AssociateMenu>> GetMenus(string roleName)
        {
            var allMenus = await _tableStorage.GetAllAsync<Entites.AssociateMenus>("AssociateMenu");
            var roleAssociateMenus = allMenus.Where(menu => menu.PartitionKey == roleName.Trim());

            return from menu in roleAssociateMenus
                   where menu.Active.GetValueOrDefault(false)
                   orderby menu.Id
                   select new AssociateMenu
                   {
                       Id = menu.Id,
                       MenuName = menu.MenuName,
                       RoleName = menu.PartitionKey
                   };
        }

        /// <summary>
        /// The UpdateMenus.
        /// </summary>
        /// <param name="roleName">The roleName<see cref="string"/>.</param>
        /// <param name="associateMenus">The associateMenus<see cref="List{AssociateMenu}"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task UpdateMenus(string roleName, List<AssociateMenu> associateMenus)
        {
            var allMenus = await _tableStorage.GetAllAsync<Entites.AssociateMenus>("AssociateMenu");
            var roleAssociateMenus = allMenus.Where(user => user.PartitionKey == roleName.Trim());

            foreach (var menu in associateMenus)
            {
                var associateMenu = roleAssociateMenus.FirstOrDefault(m => m.Id == menu.Id);
                if (associateMenu is null)
                {
                    try
                    {
                        var rowKey = Guid.NewGuid().ToString();
                        var newMenu = new Entites.AssociateMenus(menu.RoleName.Trim(), rowKey)
                        {
                            Id = menu.Id,
                            MenuName = menu.MenuName,
                            Active = menu.Active
                        };
                        await _tableStorage.AddAsync("AssociateMenu", newMenu);
                    }
                    catch (Exception ex)
                    {
                        throw new AppException("AssociateMenu Create Error: ", ex.InnerException);
                    }
                }
                else
                {
                    try
                    {
                        associateMenu.Active = menu.Active;
                        await _tableStorage.UpdateAsync("AssociateMenu", associateMenu);
                    }
                    catch (Exception ex)
                    {
                        throw new AppException("AssociateMenu Update Error: ", ex.InnerException);
                    }
                }
            }
        }

        /// <summary>
        /// The GetApplicationSetting.
        /// </summary>
        /// <returns>The <see cref="Task{IEnumerable{ApplicationSetting}}"/>.</returns>
        public async Task<IEnumerable<ApplicationSetting>> GetApplicationSetting()
        {
            var applicationSettings = await _tableStorage.GetAllAsync<Entites.ApplicationSettings>("ApplicationSettings");

            return from setting in applicationSettings
                   where setting.Active.GetValueOrDefault(false)
                   select new ApplicationSetting
                   {
                       Id = setting.RowKey,
                       SettingName = setting.SettingName,
                       StaticContent = JsonConvert.DeserializeObject<List<StaticContent>>(setting.StaticContent)
                   };
        }

        /// <summary>
        /// The SaveApplicationSetting.
        /// </summary>
        /// <param name="model">The model<see cref="ApplicationSetting"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task SaveApplicationSetting(ApplicationSetting model)
        {
            try
            {
                var applicationSettings = _tableStorage.GetAllAsync<Entites.ApplicationSettings>("ApplicationSettings").Result;
                var setting = (from settings in applicationSettings
                               where settings.SettingName?.ToLower() == model.SettingName.ToLower()
                               select settings).FirstOrDefault();
                if (model.StaticContent != null)
                {
                    foreach (var data in model.StaticContent)
                    {
                        data.Blob = "";
                    }
                }

                if (setting == null)
                {
                    var settingId = String.IsNullOrEmpty(model.Id) ? Guid.NewGuid().ToString() : model.Id;

                    var newSetting = new Entites.ApplicationSettings(model.TenantId, settingId)
                    {
                        RowKey = settingId,
                        PartitionKey = model.SettingName,
                        SettingName = model.SettingName,
                        Active = true,
                        UpdatedOn = DateTime.UtcNow,
                    };

                    if (model.StaticContent != null)
                    {
                        newSetting.StaticContent = JsonConvert.SerializeObject(model.StaticContent);
                    }

                    try
                    {
                        await _tableStorage.AddAsync("ApplicationSettings", newSetting);
                    }
                    catch (Exception ex)
                    {

                        throw new AppException("setting save error: ", ex.InnerException);
                    }
                }
                else
                {

                    try
                    {
                        if (model.StaticContent != null)
                        {
                            setting.StaticContent = JsonConvert.SerializeObject(model.StaticContent);
                        }

                        await _tableStorage.UpdateAsync("ApplicationSettings", setting);

                    }
                    catch (Exception ex)
                    {
                        throw new AppException("update setting error: ", ex.InnerException);
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
