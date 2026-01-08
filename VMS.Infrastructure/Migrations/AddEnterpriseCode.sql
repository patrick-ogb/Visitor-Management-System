-- Migration: Add Enterprise Code Column
-- Run this script against your database to add the Code column

-- Add Code column to Enterprises table
ALTER TABLE [Enterprises]
ADD [Code] NVARCHAR(50) NULL;

-- Create index on Code column
CREATE INDEX [IX_Enterprises_Code] ON [Enterprises] ([Code]);













