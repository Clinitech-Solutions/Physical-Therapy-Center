BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    ALTER TABLE [Payments] ADD [InsuranceChequeReceiptId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    ALTER TABLE [Payments] ADD [InsuranceServiceDeliveredAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    ALTER TABLE [Payments] ADD [InsuranceServiceMonth] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    ALTER TABLE [Payments] ADD [InsurerBalanceBefore] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    ALTER TABLE [InsuranceClaims] ADD [SubmissionBatchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    CREATE TABLE [InsuranceChequeReceipts] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceCompanyId] int NOT NULL,
        [IdempotencyKey] nvarchar(90) NOT NULL,
        [RequestHash] nvarchar(64) NOT NULL,
        [ChequeIdentityHash] nvarchar(64) NOT NULL,
        [ChequeNumber] nvarchar(100) NOT NULL,
        [BankName] nvarchar(150) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ReceivedOn] date NOT NULL,
        [ChequeDate] date NULL,
        [Notes] nvarchar(1000) NULL,
        [RecordedBy] nvarchar(450) NOT NULL,
        [RecordedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceChequeReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InsuranceChequeReceipts_InsuranceCompanies_InsuranceCompanyId] FOREIGN KEY ([InsuranceCompanyId]) REFERENCES [InsuranceCompanies] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    CREATE TABLE [InsuranceClaimSubmissions] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceCompanyId] int NOT NULL,
        [ServiceMonth] date NOT NULL,
        [IdempotencyKey] nvarchar(90) NOT NULL,
        [RequestHash] nvarchar(64) NOT NULL,
        [Reference] nvarchar(150) NOT NULL,
        [RecordedBy] nvarchar(450) NOT NULL,
        [RecordedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaimSubmissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InsuranceClaimSubmissions_InsuranceCompanies_InsuranceCompanyId] FOREIGN KEY ([InsuranceCompanyId]) REFERENCES [InsuranceCompanies] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Payments_InsuranceChequeReceiptId_InsuranceClaimId] ON [Payments] ([InsuranceChequeReceiptId], [InsuranceClaimId]) WHERE [InsuranceChequeReceiptId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    CREATE INDEX [IX_InsuranceClaims_SubmissionBatchId] ON [InsuranceClaims] ([SubmissionBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceChequeReceipts_ChequeIdentityHash] ON [InsuranceChequeReceipts] ([ChequeIdentityHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceChequeReceipts_IdempotencyKey] ON [InsuranceChequeReceipts] ([IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    CREATE INDEX [IX_InsuranceChequeReceipts_InsuranceCompanyId_ReceivedOn] ON [InsuranceChequeReceipts] ([InsuranceCompanyId], [ReceivedOn]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceClaimSubmissions_IdempotencyKey] ON [InsuranceClaimSubmissions] ([IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    CREATE INDEX [IX_InsuranceClaimSubmissions_InsuranceCompanyId_ServiceMonth] ON [InsuranceClaimSubmissions] ([InsuranceCompanyId], [ServiceMonth]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    ALTER TABLE [InsuranceClaims] ADD CONSTRAINT [FK_InsuranceClaims_InsuranceClaimSubmissions_SubmissionBatchId] FOREIGN KEY ([SubmissionBatchId]) REFERENCES [InsuranceClaimSubmissions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_InsuranceChequeReceipts_InsuranceChequeReceiptId] FOREIGN KEY ([InsuranceChequeReceiptId]) REFERENCES [InsuranceChequeReceipts] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106160811_InsuranceChequeReceipts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106160811_InsuranceChequeReceipts', N'10.0.2');
END;

COMMIT;
GO

