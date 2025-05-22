-- Создаем таблицу для результатов анализа
CREATE TABLE IF NOT EXISTS "FileAnalysisResults" (
    "Id" uuid NOT NULL,
    "FileId" uuid NOT NULL,
    "FileName" text NOT NULL,
    "ParagraphCount" integer NOT NULL,
    "WordCount" integer NOT NULL,
    "CharacterCount" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "IsError" boolean NOT NULL DEFAULT false,
    "ErrorMessage" text,
    CONSTRAINT "PK_FileAnalysisResults" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_FileAnalysisResults_FileId" ON "FileAnalysisResults" ("FileId");

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230522000000_InitialAnalysisCreate', '9.0.0-preview.2.24128.4')
ON CONFLICT DO NOTHING;
