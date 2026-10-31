BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031122754_AppointmentScheduleAudit'
)
BEGIN
    CREATE TABLE [AppointmentScheduleChanges] (
        [Id] int NOT NULL IDENTITY,
        [AppointmentId] int NOT NULL,
        [PatientId] int NOT NULL,
        [TreatmentPlanId] int NULL,
        [OperationKey] nvarchar(100) NOT NULL,
        [RequestHash] nvarchar(64) NOT NULL,
        [ActorId] nvarchar(450) NOT NULL,
        [ActorName] nvarchar(150) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [PreviousStart] datetime2 NOT NULL,
        [PreviousEnd] datetime2 NOT NULL,
        [PreviousRoomId] int NULL,
        [PreviousRoomName] nvarchar(100) NOT NULL,
        [NewStart] datetime2 NOT NULL,
        [NewEnd] datetime2 NOT NULL,
        [NewRoomId] int NOT NULL,
        [NewRoomName] nvarchar(100) NOT NULL,
        [ClinicianId] nvarchar(450) NOT NULL,
        [Reason] nvarchar(500) NULL,
        CONSTRAINT [PK_AppointmentScheduleChanges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppointmentScheduleChanges_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AppointmentScheduleChanges_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031122754_AppointmentScheduleAudit'
)
BEGIN
    CREATE INDEX [IX_AppointmentScheduleChanges_AppointmentId] ON [AppointmentScheduleChanges] ([AppointmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031122754_AppointmentScheduleAudit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AppointmentScheduleChanges_OperationKey] ON [AppointmentScheduleChanges] ([OperationKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031122754_AppointmentScheduleAudit'
)
BEGIN
    CREATE INDEX [IX_AppointmentScheduleChanges_PatientId_TreatmentPlanId_ChangedAt] ON [AppointmentScheduleChanges] ([PatientId], [TreatmentPlanId], [ChangedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031122754_AppointmentScheduleAudit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031122754_AppointmentScheduleAudit', N'10.0.2');
END;

COMMIT;
GO

