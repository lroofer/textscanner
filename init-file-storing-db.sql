CREATE TABLE IF NOT EXISTS "Files" (
    "Id" uuid NOT NULL,
    "FileName" text NOT NULL,
    "Hash" text NOT NULL,
    "Location" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Files" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Files_Hash" ON "Files" ("Hash");

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230522000000_InitialCreate', '9.0.0-preview.2.24128.4')
ON CONFLICT DO NOTHING;
