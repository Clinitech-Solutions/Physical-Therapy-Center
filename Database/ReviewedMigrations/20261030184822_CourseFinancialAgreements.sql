BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    ALTER TABLE [Payments] ADD [CollectionMethod] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    ALTER TABLE [Payments] ADD [InsuranceClaimId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    ALTER TABLE [Payments] ADD [PayerType] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'AppointmentId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Invoices] ALTER COLUMN [AppointmentId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    ALTER TABLE [Invoices] ADD [CourseAgreementId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    ALTER TABLE [Invoices] ADD [DeliveredAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    ALTER TABLE [Invoices] ADD [TreatmentPlanSessionId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    ALTER TABLE [BookingBatches] ADD [CourseAgreementId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE TABLE [TreatmentCourseAgreements] (
        [Id] int NOT NULL IDENTITY,
        [PatientId] int NOT NULL,
        [TreatmentPlanId] int NOT NULL,
        [FundingType] nvarchar(20) NOT NULL,
        [PricingMode] nvarchar(20) NOT NULL,
        [PrescribedSessions] int NOT NULL,
        [CoveredSessions] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [DiscountType] nvarchar(20) NOT NULL,
        [DiscountValue] decimal(18,2) NOT NULL,
        [DiscountAmount] decimal(18,2) NOT NULL,
        [GrossAmount] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [PatientAmount] decimal(18,2) NOT NULL,
        [InsuranceAmount] decimal(18,2) NOT NULL,
        [CopayPercentage] decimal(18,2) NOT NULL,
        [InsuranceCompanyId] int NULL,
        [ApprovedSessions] int NULL,
        [ApprovalReference] nvarchar(100) NULL,
        [ApprovalSnapshotJson] nvarchar(max) NULL,
        [InstallmentsJson] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_TreatmentCourseAgreements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TreatmentCourseAgreements_InsuranceCompanies_InsuranceCompanyId] FOREIGN KEY ([InsuranceCompanyId]) REFERENCES [InsuranceCompanies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TreatmentCourseAgreements_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TreatmentCourseAgreements_TreatmentPlans_TreatmentPlanId] FOREIGN KEY ([TreatmentPlanId]) REFERENCES [TreatmentPlans] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE TABLE [InsuranceClaims] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [CourseAgreementId] int NOT NULL,
        [TreatmentPlanSessionId] int NOT NULL,
        [PatientId] int NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [ApprovalReference] nvarchar(100) NOT NULL,
        [IdempotencyKey] nvarchar(100) NOT NULL,
        [SubmissionReference] nvarchar(150) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [SubmittedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_InsuranceClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InsuranceClaims_InsuranceCompanies_InsuranceCompanyId] FOREIGN KEY ([InsuranceCompanyId]) REFERENCES [InsuranceCompanies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InsuranceClaims_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InsuranceClaims_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InsuranceClaims_TreatmentCourseAgreements_CourseAgreementId] FOREIGN KEY ([CourseAgreementId]) REFERENCES [TreatmentCourseAgreements] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InsuranceClaims_TreatmentPlanSessions_TreatmentPlanSessionId] FOREIGN KEY ([TreatmentPlanSessionId]) REFERENCES [TreatmentPlanSessions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE INDEX [IX_Payments_InsuranceClaimId] ON [Payments] ([InsuranceClaimId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Invoices_CourseAgreementId_TreatmentPlanSessionId] ON [Invoices] ([CourseAgreementId], [TreatmentPlanSessionId]) WHERE [CourseAgreementId] IS NOT NULL AND [TreatmentPlanSessionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE INDEX [IX_Invoices_TreatmentPlanSessionId] ON [Invoices] ([TreatmentPlanSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE INDEX [IX_BookingBatches_CourseAgreementId] ON [BookingBatches] ([CourseAgreementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE INDEX [IX_InsuranceClaims_CourseAgreementId] ON [InsuranceClaims] ([CourseAgreementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceClaims_IdempotencyKey] ON [InsuranceClaims] ([IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE INDEX [IX_InsuranceClaims_InsuranceCompanyId] ON [InsuranceClaims] ([InsuranceCompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceClaims_InvoiceId] ON [InsuranceClaims] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE INDEX [IX_InsuranceClaims_PatientId] ON [InsuranceClaims] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE INDEX [IX_InsuranceClaims_TreatmentPlanSessionId] ON [InsuranceClaims] ([TreatmentPlanSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE INDEX [IX_TreatmentCourseAgreements_InsuranceCompanyId] ON [TreatmentCourseAgreements] ([InsuranceCompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE INDEX [IX_TreatmentCourseAgreements_PatientId] ON [TreatmentCourseAgreements] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TreatmentCourseAgreements_TreatmentPlanId] ON [TreatmentCourseAgreements] ([TreatmentPlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    ALTER TABLE [BookingBatches] ADD CONSTRAINT [FK_BookingBatches_TreatmentCourseAgreements_CourseAgreementId] FOREIGN KEY ([CourseAgreementId]) REFERENCES [TreatmentCourseAgreements] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    ALTER TABLE [Invoices] ADD CONSTRAINT [FK_Invoices_TreatmentCourseAgreements_CourseAgreementId] FOREIGN KEY ([CourseAgreementId]) REFERENCES [TreatmentCourseAgreements] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    ALTER TABLE [Invoices] ADD CONSTRAINT [FK_Invoices_TreatmentPlanSessions_TreatmentPlanSessionId] FOREIGN KEY ([TreatmentPlanSessionId]) REFERENCES [TreatmentPlanSessions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_InsuranceClaims_InsuranceClaimId] FOREIGN KEY ([InsuranceClaimId]) REFERENCES [InsuranceClaims] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184822_CourseFinancialAgreements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030184822_CourseFinancialAgreements', N'10.0.2');
END;

COMMIT;
GO

