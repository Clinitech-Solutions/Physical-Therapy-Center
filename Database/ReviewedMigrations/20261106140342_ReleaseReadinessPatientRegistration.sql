BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106140342_ReleaseReadinessPatientRegistration'
)
BEGIN
    ALTER TABLE [Patients] ADD [RegistrationOperationKey] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106140342_ReleaseReadinessPatientRegistration'
)
BEGIN
    ALTER TABLE [Patients] ADD [RegistrationRequestHash] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106140342_ReleaseReadinessPatientRegistration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Patients_RegistrationOperationKey] ON [Patients] ([RegistrationOperationKey]) WHERE [RegistrationOperationKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106140342_ReleaseReadinessPatientRegistration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106140342_ReleaseReadinessPatientRegistration', N'10.0.2');
END;

COMMIT;
GO

