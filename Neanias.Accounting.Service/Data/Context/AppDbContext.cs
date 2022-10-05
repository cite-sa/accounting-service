using Neanias.Accounting.Service.ErrorCode;
using Cite.Tools.Exception;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Neanias.Accounting.Service.Data.Context
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(
			DbContextOptions options,
			DbProviderConfig config,
			ErrorThesaurus errors) : base(options)
		{
			this._config = config;
			this._errors = errors;
		}

		protected readonly DbProviderConfig _config;
		protected readonly ErrorThesaurus _errors;

		public DbSet<ServiceResource> ServiceResources { get; set; }
		public DbSet<ServiceAction> ServiceActions { get; set; }
		public DbSet<ServiceUser> ServiceUsers { get; set; }
		public DbSet<UserRole> UserRoles { get; set; }
		public DbSet<Service> Services { get; set; }
		public DbSet<Metric> Metrics { get; set; }
		public DbSet<ForgetMe> ForgetMes { get; set; }
		public DbSet<TenantConfiguration> TenantConfigurations { get; set; }
		public DbSet<Tenant> Tenants { get; set; }
		public DbSet<User> Users { get; set; }
		public DbSet<UserProfile> UserProfiles { get; set; }
		public DbSet<QueueInbox> QueueInboxes { get; set; }
		public DbSet<QueueOutbox> QueueOutboxes { get; set; }
		public DbSet<StorageFile> StorageFiles { get; set; }
		public DbSet<VersionInfo> VersionInfos { get; set; }
		public DbSet<WhatYouKnowAboutMe> WhatYouKnowAboutMes { get; set; }
		public DbSet<ServiceSync> ServiceSyncs { get; set; }
		public DbSet<ServiceResetEntrySync> ServiceResetEntrySyncs { get; set; }
		public DbSet<UserSettings> UserSettings { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			switch (this._config.Provider)
			{
				case DbProviderConfig.DbProvider.SQLServer:
					{
						//Metric
						modelBuilder.Entity<Metric>().ToTable(this.PrefixTable("Metric"));
						modelBuilder.Entity<Metric>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<Metric>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<Metric>().Property(x => x.ServiceId).HasColumnName("Service");
						modelBuilder.Entity<Metric>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<Metric>().Property(x => x.Name).HasColumnName("Name");
						modelBuilder.Entity<Metric>().Property(x => x.Code).HasColumnName("Code");
						modelBuilder.Entity<Metric>().Property(x => x.Definition).HasColumnName("Definition");
						modelBuilder.Entity<Metric>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<Metric>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//Service
						modelBuilder.Entity<Service>().ToTable(this.PrefixTable("Service"));
						modelBuilder.Entity<Service>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<Service>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<Service>().Property(x => x.ParentId).HasColumnName("Parent");
						modelBuilder.Entity<Service>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<Service>().Property(x => x.Name).HasColumnName("Name");
						modelBuilder.Entity<Service>().Property(x => x.Code).HasColumnName("Code");
						modelBuilder.Entity<Service>().Property(x => x.Description).HasColumnName("Description");
						modelBuilder.Entity<Service>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<Service>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//UserRole
						modelBuilder.Entity<UserRole>().ToTable(this.PrefixTable("UserRole"));
						modelBuilder.Entity<UserRole>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<UserRole>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<UserRole>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<UserRole>().Property(x => x.Name).HasColumnName("Name");
						modelBuilder.Entity<UserRole>().Property(x => x.Rights).HasColumnName("Rights");
						modelBuilder.Entity<UserRole>().Property(x => x.Propagate).HasColumnName("Propagate");
						modelBuilder.Entity<UserRole>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<UserRole>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//ServiceUser
						modelBuilder.Entity<ServiceUser>().ToTable(this.PrefixTable("ServiceUser"));
						modelBuilder.Entity<ServiceUser>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<ServiceUser>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<ServiceUser>().Property(x => x.ServiceId).HasColumnName("Service");
						modelBuilder.Entity<ServiceUser>().Property(x => x.UserId).HasColumnName("User");
						modelBuilder.Entity<ServiceUser>().Property(x => x.RoleId).HasColumnName("Role");
						modelBuilder.Entity<ServiceUser>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<ServiceUser>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//ServiceResource
						modelBuilder.Entity<ServiceResource>().ToTable(this.PrefixTable("ServiceResource"));
						modelBuilder.Entity<ServiceResource>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<ServiceResource>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<ServiceResource>().Property(x => x.ParentId).HasColumnName("Parent");
						modelBuilder.Entity<ServiceResource>().Property(x => x.ServiceId).HasColumnName("Service");
						modelBuilder.Entity<ServiceResource>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<ServiceResource>().Property(x => x.Name).HasColumnName("Name");
						modelBuilder.Entity<ServiceResource>().Property(x => x.Code).HasColumnName("Code");
						modelBuilder.Entity<ServiceResource>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<ServiceResource>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//ServiceAction
						modelBuilder.Entity<ServiceAction>().ToTable(this.PrefixTable("ServiceAction"));
						modelBuilder.Entity<ServiceAction>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<ServiceAction>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<ServiceAction>().Property(x => x.ParentId).HasColumnName("Parent");
						modelBuilder.Entity<ServiceAction>().Property(x => x.ServiceId).HasColumnName("Service");
						modelBuilder.Entity<ServiceAction>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<ServiceAction>().Property(x => x.Name).HasColumnName("Name");
						modelBuilder.Entity<ServiceAction>().Property(x => x.Code).HasColumnName("Code");
						modelBuilder.Entity<ServiceAction>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<ServiceAction>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//ForgetMe
						modelBuilder.Entity<ForgetMe>().ToTable(this.PrefixTable("ForgetMe"));
						modelBuilder.Entity<ForgetMe>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<ForgetMe>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<ForgetMe>().Property(x => x.UserId).HasColumnName("User");
						modelBuilder.Entity<ForgetMe>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<ForgetMe>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						modelBuilder.Entity<ForgetMe>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<ForgetMe>().Property(x => x.State).HasColumnName("State");
						//ServiceSync
						modelBuilder.Entity<ServiceSync>().ToTable(this.PrefixTable("ServiceSync"));
						modelBuilder.Entity<ServiceSync>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<ServiceSync>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<ServiceSync>().Property(x => x.LastSyncAt).HasColumnName("LastSyncAt");
						modelBuilder.Entity<ServiceSync>().Property(x => x.LastSyncEntryTimestamp).HasColumnName("LastSyncEntryTimestamp");
						modelBuilder.Entity<ServiceSync>().Property(x => x.ServiceId).HasColumnName("ServiceId");
						modelBuilder.Entity<ServiceSync>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<ServiceSync>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						modelBuilder.Entity<ServiceSync>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<ServiceSync>().Property(x => x.Status).HasColumnName("status");
						//ServiceResetEntrySync
						modelBuilder.Entity<ServiceResetEntrySync>().ToTable(this.PrefixTable("ServiceResetEntrySync"));
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.LastSyncAt).HasColumnName("LastSyncAt");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.LastSyncEntryTimestamp).HasColumnName("LastSyncEntryTimestamp");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.LastSyncEntryId).HasColumnName("LastSyncEntryId");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.ServiceId).HasColumnName("ServiceId");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.Status).HasColumnName("status");
						//Tenant Configuration
						modelBuilder.Entity<TenantConfiguration>().ToTable(this.PrefixTable("TenantConfiguration"));
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.Type).HasColumnName("Type");
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.Value).HasColumnName("Value");
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//Tenant
						modelBuilder.Entity<Tenant>().ToTable(this.PrefixTable("Tenant"));
						modelBuilder.Entity<Tenant>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<Tenant>().Property(x => x.Code).HasColumnName("Code");
						modelBuilder.Entity<Tenant>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<Tenant>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<Tenant>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//User
						modelBuilder.Entity<User>().ToTable(this.PrefixTable("User"));
						modelBuilder.Entity<User>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<User>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<User>().Property(x => x.Subject).HasColumnName("Subject");
						modelBuilder.Entity<User>().Property(x => x.Name).HasColumnName("Name");
						modelBuilder.Entity<User>().Property(x => x.Email).HasColumnName("Email");
						modelBuilder.Entity<User>().Property(x => x.Issuer).HasColumnName("Issuer");
						modelBuilder.Entity<User>().Property(x => x.ProfileId).HasColumnName("Profile");
						modelBuilder.Entity<User>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<User>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<User>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//UserProfile
						modelBuilder.Entity<UserProfile>().ToTable(this.PrefixTable("UserProfile"));
						modelBuilder.Entity<UserProfile>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<UserProfile>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<UserProfile>().Property(x => x.Timezone).HasColumnName("Timezone");
						modelBuilder.Entity<UserProfile>().Property(x => x.Culture).HasColumnName("Culture");
						modelBuilder.Entity<UserProfile>().Property(x => x.Language).HasColumnName("Language");
						modelBuilder.Entity<UserProfile>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<UserProfile>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//UserSettings
						modelBuilder.Entity<UserSettings>().ToTable(this.PrefixTable("UserSettings"));
						modelBuilder.Entity<UserSettings>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<UserSettings>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<UserSettings>().Property(x => x.UserId).HasColumnName("User");
						modelBuilder.Entity<UserSettings>().Property(x => x.Name).HasColumnName("Name");
						modelBuilder.Entity<UserSettings>().Property(x => x.Key).HasColumnName("Key");
						modelBuilder.Entity<UserSettings>().Property(x => x.Type).HasColumnName("Type");
						modelBuilder.Entity<UserSettings>().Property(x => x.Value).HasColumnName("Value");
						modelBuilder.Entity<UserSettings>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<UserSettings>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//QueueInbox
						modelBuilder.Entity<QueueInbox>().ToTable(this.PrefixTable("QueueInbox"));
						modelBuilder.Entity<QueueInbox>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<QueueInbox>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<QueueInbox>().Property(x => x.Queue).HasColumnName("Queue");
						modelBuilder.Entity<QueueInbox>().Property(x => x.Exchange).HasColumnName("Exchange");
						modelBuilder.Entity<QueueInbox>().Property(x => x.Route).HasColumnName("Route");
						modelBuilder.Entity<QueueInbox>().Property(x => x.ApplicationId).HasColumnName("ApplicationId");
						modelBuilder.Entity<QueueInbox>().Property(x => x.MessageId).HasColumnName("MessageId");
						modelBuilder.Entity<QueueInbox>().Property(x => x.Message).HasColumnName("Message");
						modelBuilder.Entity<QueueInbox>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<QueueInbox>().Property(x => x.Status).HasColumnName("Status");
						modelBuilder.Entity<QueueInbox>().Property(x => x.RetryCount).HasColumnName("RetryCount");
						modelBuilder.Entity<QueueInbox>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<QueueInbox>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//QueueOutbox
						modelBuilder.Entity<QueueOutbox>().ToTable(this.PrefixTable("QueueOutbox"));
						modelBuilder.Entity<QueueOutbox>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.Exchange).HasColumnName("Exchange");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.Route).HasColumnName("Route");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.MessageId).HasColumnName("MessageId");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.Message).HasColumnName("Message");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.NotifyStatus).HasColumnName("Status");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.RetryCount).HasColumnName("RetryCount");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.ConfirmedAt).HasColumnName("ConfirmedAt");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						//StorageFile
						modelBuilder.Entity<StorageFile>().ToTable(this.PrefixTable("StorageFile"));
						modelBuilder.Entity<StorageFile>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<StorageFile>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<StorageFile>().Property(x => x.FileRef).HasColumnName("FileRef");
						modelBuilder.Entity<StorageFile>().Property(x => x.Name).HasColumnName("Name");
						modelBuilder.Entity<StorageFile>().Property(x => x.Extension).HasColumnName("Extension");
						modelBuilder.Entity<StorageFile>().Property(x => x.MimeType).HasColumnName("MimeType");
						modelBuilder.Entity<StorageFile>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<StorageFile>().Property(x => x.PurgeAt).HasColumnName("PurgeAt");
						modelBuilder.Entity<StorageFile>().Property(x => x.PurgedAt).HasColumnName("PurgedAt");
						//VersionInfo
						modelBuilder.Entity<VersionInfo>().ToTable(this.PrefixTable("VersionInfo"));
						modelBuilder.Entity<VersionInfo>().Property(x => x.Key).HasColumnName("Key");
						modelBuilder.Entity<VersionInfo>().Property(x => x.Version).HasColumnName("Version");
						modelBuilder.Entity<VersionInfo>().Property(x => x.ReleasedAt).HasColumnName("ReleasedAt");
						modelBuilder.Entity<VersionInfo>().Property(x => x.DeployedAt).HasColumnName("DeployedAt");
						modelBuilder.Entity<VersionInfo>().Property(x => x.Description).HasColumnName("Description");
						//WhatYouKnowAboutMe
						modelBuilder.Entity<WhatYouKnowAboutMe>().ToTable(this.PrefixTable("WhatYouKnowAboutMe"));
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.Id).HasColumnName("Id");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.TenantId).HasColumnName("Tenant");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.UserId).HasColumnName("User");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.StorageFileId).HasColumnName("StorageFile");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.CreatedAt).HasColumnName("CreatedAt");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.IsActive).HasColumnName("IsActive");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.State).HasColumnName("State");

						break;
					}
				case DbProviderConfig.DbProvider.PostgreSQL:
					{
						//Metric
						modelBuilder.Entity<Metric>().ToTable(this.PrefixTable("acc_metric"));
						modelBuilder.Entity<Metric>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<Metric>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<Metric>().Property(x => x.ServiceId).HasColumnName("service");
						modelBuilder.Entity<Metric>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<Metric>().Property(x => x.Name).HasColumnName("name");
						modelBuilder.Entity<Metric>().Property(x => x.Code).HasColumnName("code");
						modelBuilder.Entity<Metric>().Property(x => x.Definition).HasColumnName("definition");
						modelBuilder.Entity<Metric>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<Metric>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//Service
						modelBuilder.Entity<Service>().ToTable(this.PrefixTable("acc_service"));
						modelBuilder.Entity<Service>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<Service>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<Service>().Property(x => x.ParentId).HasColumnName("parent");
						modelBuilder.Entity<Service>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<Service>().Property(x => x.Name).HasColumnName("name");
						modelBuilder.Entity<Service>().Property(x => x.Code).HasColumnName("code");
						modelBuilder.Entity<Service>().Property(x => x.Description).HasColumnName("description");
						modelBuilder.Entity<Service>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<Service>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//UserRole
						modelBuilder.Entity<UserRole>().ToTable(this.PrefixTable("acc_user_role"));
						modelBuilder.Entity<UserRole>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<UserRole>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<UserRole>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<UserRole>().Property(x => x.Name).HasColumnName("name");
						modelBuilder.Entity<UserRole>().Property(x => x.Rights).HasColumnName("rights");
						modelBuilder.Entity<UserRole>().Property(x => x.Propagate).HasColumnName("propagate");
						modelBuilder.Entity<UserRole>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<UserRole>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//ServiceUser
						modelBuilder.Entity<ServiceUser>().ToTable(this.PrefixTable("acc_service_user"));
						modelBuilder.Entity<ServiceUser>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<ServiceUser>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<ServiceUser>().Property(x => x.ServiceId).HasColumnName("service");
						modelBuilder.Entity<ServiceUser>().Property(x => x.UserId).HasColumnName("user");
						modelBuilder.Entity<ServiceUser>().Property(x => x.RoleId).HasColumnName("role");
						modelBuilder.Entity<ServiceUser>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<ServiceUser>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//ServiceResource
						modelBuilder.Entity<ServiceResource>().ToTable(this.PrefixTable("acc_service_resource"));
						modelBuilder.Entity<ServiceResource>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<ServiceResource>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<ServiceResource>().Property(x => x.ParentId).HasColumnName("parent");
						modelBuilder.Entity<ServiceResource>().Property(x => x.ServiceId).HasColumnName("service");
						modelBuilder.Entity<ServiceResource>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<ServiceResource>().Property(x => x.Name).HasColumnName("name");
						modelBuilder.Entity<ServiceResource>().Property(x => x.Code).HasColumnName("code");
						modelBuilder.Entity<ServiceResource>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<ServiceResource>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//ServiceAction
						modelBuilder.Entity<ServiceAction>().ToTable(this.PrefixTable("acc_service_action"));
						modelBuilder.Entity<ServiceAction>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<ServiceAction>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<ServiceAction>().Property(x => x.ParentId).HasColumnName("parent");
						modelBuilder.Entity<ServiceAction>().Property(x => x.ServiceId).HasColumnName("service");
						modelBuilder.Entity<ServiceAction>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<ServiceAction>().Property(x => x.Name).HasColumnName("name");
						modelBuilder.Entity<ServiceAction>().Property(x => x.Code).HasColumnName("code");
						modelBuilder.Entity<ServiceAction>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<ServiceAction>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//ForgetMe
						modelBuilder.Entity<ForgetMe>().ToTable(this.PrefixTable("forget_me"));
						modelBuilder.Entity<ForgetMe>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<ForgetMe>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<ForgetMe>().Property(x => x.UserId).HasColumnName("user");
						modelBuilder.Entity<ForgetMe>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<ForgetMe>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						modelBuilder.Entity<ForgetMe>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<ForgetMe>().Property(x => x.State).HasColumnName("state");
						//ServiceSync
						modelBuilder.Entity<ServiceSync>().ToTable(this.PrefixTable("acc_service_sync"));
						modelBuilder.Entity<ServiceSync>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<ServiceSync>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<ServiceSync>().Property(x => x.LastSyncAt).HasColumnName("last_sync_at");
						modelBuilder.Entity<ServiceSync>().Property(x => x.LastSyncEntryTimestamp).HasColumnName("last_sync_entry_timestamp");
						modelBuilder.Entity<ServiceSync>().Property(x => x.ServiceId).HasColumnName("service");
						modelBuilder.Entity<ServiceSync>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<ServiceSync>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						modelBuilder.Entity<ServiceSync>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<ServiceSync>().Property(x => x.Status).HasColumnName("status");
						//ServiceResetEntrySync
						modelBuilder.Entity<ServiceResetEntrySync>().ToTable(this.PrefixTable("acc_service_reset_entry_sync"));
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.LastSyncAt).HasColumnName("last_sync_at");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.LastSyncEntryTimestamp).HasColumnName("last_sync_entry_timestamp");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.LastSyncEntryId).HasColumnName("last_sync_entry_id");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.ServiceId).HasColumnName("service");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.Status).HasColumnName("status");
						//Tenant Configuration
						modelBuilder.Entity<TenantConfiguration>().ToTable(this.PrefixTable("tenant_configuration"));
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.Type).HasColumnName("type");
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.Value).HasColumnName("value");
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<TenantConfiguration>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//Tenant
						modelBuilder.Entity<Tenant>().ToTable(this.PrefixTable("tenant"));
						modelBuilder.Entity<Tenant>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<Tenant>().Property(x => x.Code).HasColumnName("code");
						modelBuilder.Entity<Tenant>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<Tenant>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<Tenant>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//User
						modelBuilder.Entity<User>().ToTable(this.PrefixTable("user"));
						modelBuilder.Entity<User>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<User>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<User>().Property(x => x.Subject).HasColumnName("subject");
						modelBuilder.Entity<User>().Property(x => x.Email).HasColumnName("email");
						modelBuilder.Entity<User>().Property(x => x.Name).HasColumnName("name");
						modelBuilder.Entity<User>().Property(x => x.Issuer).HasColumnName("issuer");
						modelBuilder.Entity<User>().Property(x => x.ProfileId).HasColumnName("profile");
						modelBuilder.Entity<User>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<User>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<User>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//UserProfile
						modelBuilder.Entity<UserProfile>().ToTable(this.PrefixTable("user_profile"));
						modelBuilder.Entity<UserProfile>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<UserProfile>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<UserProfile>().Property(x => x.Timezone).HasColumnName("timezone");
						modelBuilder.Entity<UserProfile>().Property(x => x.Culture).HasColumnName("culture");
						modelBuilder.Entity<UserProfile>().Property(x => x.Language).HasColumnName("language");
						modelBuilder.Entity<UserProfile>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<UserProfile>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//UserSettings
						modelBuilder.Entity<UserSettings>().ToTable(this.PrefixTable("user_settings"));
						modelBuilder.Entity<UserSettings>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<UserSettings>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<UserSettings>().Property(x => x.UserId).HasColumnName("user");
						modelBuilder.Entity<UserSettings>().Property(x => x.Name).HasColumnName("name");
						modelBuilder.Entity<UserSettings>().Property(x => x.Key).HasColumnName("key");
						modelBuilder.Entity<UserSettings>().Property(x => x.Type).HasColumnName("type");
						modelBuilder.Entity<UserSettings>().Property(x => x.Value).HasColumnName("value");
						modelBuilder.Entity<UserSettings>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<UserSettings>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//QueueInbox
						modelBuilder.Entity<QueueInbox>().ToTable(this.PrefixTable("queue_inbox"));
						modelBuilder.Entity<QueueInbox>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<QueueInbox>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<QueueInbox>().Property(x => x.Queue).HasColumnName("queue");
						modelBuilder.Entity<QueueInbox>().Property(x => x.Exchange).HasColumnName("exchange");
						modelBuilder.Entity<QueueInbox>().Property(x => x.Route).HasColumnName("route");
						modelBuilder.Entity<QueueInbox>().Property(x => x.ApplicationId).HasColumnName("application_id");
						modelBuilder.Entity<QueueInbox>().Property(x => x.MessageId).HasColumnName("message_id");
						modelBuilder.Entity<QueueInbox>().Property(x => x.Message).HasColumnName("message");
						modelBuilder.Entity<QueueInbox>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<QueueInbox>().Property(x => x.Status).HasColumnName("status");
						modelBuilder.Entity<QueueInbox>().Property(x => x.RetryCount).HasColumnName("retry_count");
						modelBuilder.Entity<QueueInbox>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<QueueInbox>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//QueueOutbox
						modelBuilder.Entity<QueueOutbox>().ToTable(this.PrefixTable("queue_outbox"));
						modelBuilder.Entity<QueueOutbox>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.Exchange).HasColumnName("exchange");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.Route).HasColumnName("route");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.MessageId).HasColumnName("message_id");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.Message).HasColumnName("message");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.NotifyStatus).HasColumnName("status");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.RetryCount).HasColumnName("retry_count");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.PublishedAt).HasColumnName("published_at");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.ConfirmedAt).HasColumnName("confirmed_at");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<QueueOutbox>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						//StorageFile
						modelBuilder.Entity<StorageFile>().ToTable(this.PrefixTable("storage_file"));
						modelBuilder.Entity<StorageFile>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<StorageFile>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<StorageFile>().Property(x => x.FileRef).HasColumnName("file_ref");
						modelBuilder.Entity<StorageFile>().Property(x => x.Name).HasColumnName("name");
						modelBuilder.Entity<StorageFile>().Property(x => x.Extension).HasColumnName("extension");
						modelBuilder.Entity<StorageFile>().Property(x => x.MimeType).HasColumnName("mime_type");
						modelBuilder.Entity<StorageFile>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<StorageFile>().Property(x => x.PurgeAt).HasColumnName("purge_at");
						modelBuilder.Entity<StorageFile>().Property(x => x.PurgedAt).HasColumnName("purged_at");
						//VersionInfo
						modelBuilder.Entity<VersionInfo>().ToTable(this.PrefixTable("version_info"));
						modelBuilder.Entity<VersionInfo>().Property(x => x.Key).HasColumnName("key");
						modelBuilder.Entity<VersionInfo>().Property(x => x.Version).HasColumnName("version");
						modelBuilder.Entity<VersionInfo>().Property(x => x.ReleasedAt).HasColumnName("released_at");
						modelBuilder.Entity<VersionInfo>().Property(x => x.DeployedAt).HasColumnName("deployed_at");
						modelBuilder.Entity<VersionInfo>().Property(x => x.Description).HasColumnName("description");
						//WhatYouKnowAboutMe
						modelBuilder.Entity<WhatYouKnowAboutMe>().ToTable(this.PrefixTable("what_you_know_about_me"));
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.Id).HasColumnName("id");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.TenantId).HasColumnName("tenant");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.UserId).HasColumnName("user");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.StorageFileId).HasColumnName("storage_file");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.CreatedAt).HasColumnName("created_at");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.UpdatedAt).HasColumnName("updated_at");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.IsActive).HasColumnName("is_active");
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.State).HasColumnName("state");

						break;
					}
				default: throw new MyApplicationException(this._errors.SystemError.Code, this._errors.SystemError.Message);
			}

			modelBuilder.Entity<ForgetMe>().Property(x => x.UpdatedAt).IsConcurrencyToken();
			modelBuilder.Entity<QueueInbox>().Property(x => x.UpdatedAt).IsConcurrencyToken();
			modelBuilder.Entity<QueueOutbox>().Property(x => x.UpdatedAt).IsConcurrencyToken();
			modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.UpdatedAt).IsConcurrencyToken();

			DateTimeToTicksConverter dateTimeToTicksConverter = new DateTimeToTicksConverter();
			switch (this._config.Provider)
			{
				case DbProviderConfig.DbProvider.PostgreSQL:
					{
						modelBuilder.Entity<ForgetMe>().Property(x => x.UpdatedAt).HasConversion(dateTimeToTicksConverter);
						modelBuilder.Entity<QueueInbox>().Property(x => x.UpdatedAt).HasConversion(dateTimeToTicksConverter);
						modelBuilder.Entity<QueueOutbox>().Property(x => x.UpdatedAt).HasConversion(dateTimeToTicksConverter);
						modelBuilder.Entity<ServiceSync>().Property(x => x.UpdatedAt).HasConversion(dateTimeToTicksConverter);
						modelBuilder.Entity<ServiceResetEntrySync>().Property(x => x.UpdatedAt).HasConversion(dateTimeToTicksConverter);
						modelBuilder.Entity<WhatYouKnowAboutMe>().Property(x => x.UpdatedAt).HasConversion(dateTimeToTicksConverter);
						break;
					}
				case DbProviderConfig.DbProvider.SQLServer:
				default: break;
			}
		}

		private String PrefixTable(String name)
		{
			if (String.IsNullOrEmpty(this._config.TablePrefix)) return name;
			return $"{this._config.TablePrefix}{name}";
		}
	}
}
