IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [AttendanceProfiles] (
        [Id] int NOT NULL IDENTITY,
        [ProfileName] nvarchar(max) NOT NULL,
        [ExpectedClockIn] time NOT NULL,
        [ExpectedClockOut] time NOT NULL,
        [RequiredWorkHours] float NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AttendanceProfiles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [JobRoles] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [DepartmentId] int NOT NULL,
        [RequiredQualificationsJson] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_JobRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [PointsRules] (
        [Id] int NOT NULL IDENTITY,
        [RuleKey] nvarchar(max) NOT NULL,
        [EventType] nvarchar(max) NOT NULL,
        [ConditionExpression] nvarchar(max) NOT NULL,
        [ActionType] nvarchar(max) NOT NULL,
        [PointValue] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PointsRules] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [RewardItems] (
        [Id] int NOT NULL IDENTITY,
        [RewardName] nvarchar(max) NOT NULL,
        [CostInPoints] int NOT NULL,
        [AvailableStock] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RewardItems] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [PermissionsJson] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [Sites] (
        [Id] int NOT NULL IDENTITY,
        [SiteName] nvarchar(100) NOT NULL,
        [Latitude] float(9) NOT NULL,
        [Longitude] float(9) NOT NULL,
        [MacAddressWhitelistJson] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Sites] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [FirstName] nvarchar(50) NOT NULL,
        [LastName] nvarchar(50) NOT NULL,
        [Email] varchar(150) NOT NULL,
        [PhoneNumber] varchar(20) NULL,
        [JobRoleId] int NOT NULL,
        [RoleId] int NOT NULL,
        [SiteId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_JobRoles_JobRoleId] FOREIGN KEY ([JobRoleId]) REFERENCES [JobRoles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Sites_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [Sites] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [DisciplinaryViolations] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Severity] varchar(20) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_DisciplinaryViolations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DisciplinaryViolations_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [EmployeeDocuments] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [StorageUrl] varchar(500) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EmployeeDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeDocuments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [PointsTransactions] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [PointsRuleId] int NULL,
        [Amount] int NOT NULL,
        [TransactionType] varchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PointsTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PointsTransactions_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PointsTransactions_PointsRules_PointsRuleId] FOREIGN KEY ([PointsRuleId]) REFERENCES [PointsRules] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [RewardRedemptions] (
        [Id] int NOT NULL IDENTITY,
        [RewardItemId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [VoucherCode] varchar(100) NOT NULL,
        [RedeemedAt] datetimeoffset NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RewardRedemptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RewardRedemptions_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RewardRedemptions_RewardItems_RewardItemId] FOREIGN KEY ([RewardItemId]) REFERENCES [RewardItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [ShiftEntities] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NULL,
        [SiteId] int NOT NULL,
        [JobRoleId] int NOT NULL,
        [StartTime] datetimeoffset NOT NULL,
        [EndTime] datetimeoffset NOT NULL,
        [IsPublished] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ShiftEntities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShiftEntities_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShiftEntities_JobRoles_JobRoleId] FOREIGN KEY ([JobRoleId]) REFERENCES [JobRoles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShiftEntities_Sites_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [Sites] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE TABLE [ShiftClaims] (
        [Id] int NOT NULL IDENTITY,
        [ShiftId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [Status] varchar(20) NOT NULL,
        [OvertimeJustification] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ShiftClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShiftClaims_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShiftClaims_ShiftEntities_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [ShiftEntities] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DisciplinaryViolations_EmployeeId] ON [DisciplinaryViolations] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeDocuments_EmployeeId] ON [EmployeeDocuments] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_Email] ON [Employees] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_JobRoleId] ON [Employees] ([JobRoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_RoleId] ON [Employees] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_SiteId] ON [Employees] ([SiteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PointsTransactions_EmployeeId] ON [PointsTransactions] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PointsTransactions_PointsRuleId] ON [PointsTransactions] ([PointsRuleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RewardRedemptions_EmployeeId] ON [RewardRedemptions] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RewardRedemptions_RewardItemId] ON [RewardRedemptions] ([RewardItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RewardRedemptions_VoucherCode] ON [RewardRedemptions] ([VoucherCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ShiftClaims_EmployeeId] ON [ShiftClaims] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ShiftClaims_ShiftId] ON [ShiftClaims] ([ShiftId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ShiftEntities_EmployeeId] ON [ShiftEntities] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ShiftEntities_JobRoleId] ON [ShiftEntities] ([JobRoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ShiftEntities_SiteId] ON [ShiftEntities] ([SiteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813020025_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813020025_InitialCreate', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Sites] ADD [EmployeeId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [AttendanceType] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [Birthdate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [DirectManagerId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [EmployeeCode] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [ExperienceYears] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [Gender] varchar(20) NOT NULL DEFAULT '';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [JobType] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [JoinDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [OfflineWorkdaysJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [OnlineWorkdaysJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [PasswordHash] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [ProfilePhotoUrl] varchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD [SeniorityLevel] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [DisciplinaryViolations] ADD [ActionDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [DisciplinaryViolations] ADD [ActionDescription] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [DisciplinaryViolations] ADD [ActionTakenById] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [DisciplinaryViolations] ADD [ActionType] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [DisciplinaryViolations] ADD [DocumentUrl] varchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [DisciplinaryViolations] ADD [ReportedById] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [DisciplinaryViolations] ADD [Status] varchar(30) NOT NULL DEFAULT '';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [DisciplinaryViolations] ADD [ViolationType] varchar(30) NOT NULL DEFAULT '';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [DisciplinaryViolations] ADD [WitnessesJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE TABLE [EmployeeAchievements] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [BadgeType] nvarchar(100) NOT NULL,
        [AwardedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EmployeeAchievements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeAchievements_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE TABLE [EmployeeSites] (
        [EmployeeId] int NOT NULL,
        [SiteId] int NOT NULL,
        CONSTRAINT [PK_EmployeeSites] PRIMARY KEY ([EmployeeId], [SiteId]),
        CONSTRAINT [FK_EmployeeSites_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmployeeSites_Sites_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [Sites] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE TABLE [EmployeeTasks] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [TaskName] nvarchar(200) NOT NULL,
        [Status] varchar(30) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EmployeeTasks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeTasks_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE TABLE [PayrollProfiles] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [SalaryType] varchar(20) NOT NULL,
        [PayoutPeriod] nvarchar(50) NOT NULL,
        [PayoutDay] int NOT NULL,
        [WorkWeekStart] int NOT NULL,
        [WorkWeekEnd] int NOT NULL,
        [PaymentAmount] decimal(18,2) NOT NULL,
        [OvertimeThresholdHours] decimal(18,2) NOT NULL,
        [OvertimeHourlyRate] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PayrollProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollProfiles_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE TABLE [PerformanceMetrics] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Target] decimal(18,2) NOT NULL,
        [Weight] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PerformanceMetrics] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE TABLE [PerformanceSubmissions] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [MetricId] int NOT NULL,
        [AchievedPercent] decimal(18,2) NOT NULL,
        [Score] decimal(18,2) NOT NULL,
        [SubmissionDate] datetime2 NOT NULL,
        [Feedback] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PerformanceSubmissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PerformanceSubmissions_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PerformanceSubmissions_PerformanceMetrics_MetricId] FOREIGN KEY ([MetricId]) REFERENCES [PerformanceMetrics] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE INDEX [IX_Sites_EmployeeId] ON [Sites] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE INDEX [IX_Employees_DirectManagerId] ON [Employees] ([DirectManagerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE INDEX [IX_DisciplinaryViolations_ActionTakenById] ON [DisciplinaryViolations] ([ActionTakenById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE INDEX [IX_DisciplinaryViolations_ReportedById] ON [DisciplinaryViolations] ([ReportedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE INDEX [IX_EmployeeAchievements_EmployeeId] ON [EmployeeAchievements] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE INDEX [IX_EmployeeSites_SiteId] ON [EmployeeSites] ([SiteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE INDEX [IX_EmployeeTasks_EmployeeId] ON [EmployeeTasks] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PayrollProfiles_EmployeeId] ON [PayrollProfiles] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE INDEX [IX_PerformanceSubmissions_EmployeeId] ON [PerformanceSubmissions] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    CREATE INDEX [IX_PerformanceSubmissions_MetricId] ON [PerformanceSubmissions] ([MetricId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [DisciplinaryViolations] ADD CONSTRAINT [FK_DisciplinaryViolations_Employees_ActionTakenById] FOREIGN KEY ([ActionTakenById]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [DisciplinaryViolations] ADD CONSTRAINT [FK_DisciplinaryViolations_Employees_ReportedById] FOREIGN KEY ([ReportedById]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_Employees_DirectManagerId] FOREIGN KEY ([DirectManagerId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    ALTER TABLE [Sites] ADD CONSTRAINT [FK_Sites_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818121853_AddNewEntitiesAndRelationships'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260818121853_AddNewEntitiesAndRelationships', N'10.0.10');
END;

COMMIT;
GO

