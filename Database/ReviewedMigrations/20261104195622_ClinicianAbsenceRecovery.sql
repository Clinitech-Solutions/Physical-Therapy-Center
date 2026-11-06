BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [DoctorAbsences] ADD [EndsAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [DoctorAbsences] ADD [HistoryJson] nvarchar(max) NOT NULL DEFAULT N'[]';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [DoctorAbsences] ADD [OperationKey] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [DoctorAbsences] ADD [StartsAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [DoctorAbsences] ADD [Version] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [DoctorAbsences] ADD [Withdrawn] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [AppointmentScheduleChanges] ADD [AbsenceId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [AppointmentScheduleChanges] ADD [Action] nvarchar(40) NOT NULL DEFAULT N'Reschedule';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [AppointmentScheduleChanges] ADD [ClinicianName] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [AppointmentScheduleChanges] ADD [ConsentMethod] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [AppointmentScheduleChanges] ADD [ConsentReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [AppointmentScheduleChanges] ADD [ConsentRecordedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [AppointmentScheduleChanges] ADD [ContactOutcome] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [AppointmentScheduleChanges] ADD [GenderException] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [AppointmentScheduleChanges] ADD [PreviousClinicianId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [AppointmentScheduleChanges] ADD [PreviousClinicianName] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DoctorAbsences_OperationKey] ON [DoctorAbsences] ([OperationKey]) WHERE [OperationKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    CREATE INDEX [IX_DoctorAbsences_StartsAt_EndsAt] ON [DoctorAbsences] ([StartsAt], [EndsAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    CREATE INDEX [IX_AppointmentScheduleChanges_AbsenceId] ON [AppointmentScheduleChanges] ([AbsenceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    CREATE INDEX [IX_AppointmentScheduleChanges_AppointmentId_AbsenceId_ChangedAt] ON [AppointmentScheduleChanges] ([AppointmentId], [AbsenceId], [ChangedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    ALTER TABLE [AppointmentScheduleChanges] ADD CONSTRAINT [FK_AppointmentScheduleChanges_DoctorAbsences_AbsenceId] FOREIGN KEY ([AbsenceId]) REFERENCES [DoctorAbsences] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104195622_ClinicianAbsenceRecovery'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104195622_ClinicianAbsenceRecovery', N'10.0.2');
END;

COMMIT;
GO

