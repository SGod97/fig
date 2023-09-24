﻿using System.Diagnostics;
using Fig.Web.Facades;
using Fig.Web.Models.Configuration;
using Fig.Web.Notifications;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages
{
    public partial class Configuration
    {
        private bool _isMigrationInProgress = false;
        
        [Inject]
        private IConfigurationFacade ConfigurationFacade { get; set; } = null!;
        
        [Inject]
        private NotificationService NotificationService { get; set; } = null!;

        [Inject]
        private INotificationFactory NotificationFactory { get; set; } = null!;

        private FigConfigurationModel ConfigurationModel => ConfigurationFacade.ConfigurationModel;

        protected override async Task OnInitializedAsync()
        {
            await ConfigurationFacade.LoadConfiguration();
            await base.OnInitializedAsync();
        }

        private void OnConfigurationValueChanged()
        {
            ConfigurationFacade.SaveConfiguration();
        }

        private async Task MigrateEncryptedData()
        {
            _isMigrationInProgress = true;
            try
            {
                var watch = Stopwatch.StartNew();
                await ConfigurationFacade.MigrateEncryptedData();
                NotificationService.Notify(NotificationFactory.Success("Migration Complete", $"Completed in {watch.ElapsedMilliseconds.Milliseconds()}"));
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationFactory.Failure("Migration Failed", ex.Message));
            }
            finally
            {
                _isMigrationInProgress = false;
            }
        }
    }
}
