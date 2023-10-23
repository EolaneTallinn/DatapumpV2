--Copy of TraceFiles
SELECT TOP(100) ProductionDB.dbo.TraceFiles.Handle AS Handle,
	ProductionDB.dbo.TraceFiles.ChildID AS ChildID,
	ProductionDB.dbo.TraceFiles.SequenceID AS SequenceID,
	ProductionDB.dbo.TraceFiles.ParentID AS ParentID,
	ProductionDB.dbo.TraceFiles.ActivityLogFile_BO AS ActivityLogFile_BO,
	ProductionDB.dbo.TraceFiles.[Partition Date] AS PartitionDate,
	ProductionDB.dbo.TraceFiles.[Created Date] AS Created
INTO TracePartition
FROM ProductionDB.dbo.TraceFiles;

--Copy of item
CREATE TABLE Item
(
    Handle INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
    [SerialNb] NVARCHAR(255),
    [Created] DATETIME
);

--Copy of resource
SELECT TOP(100) ProductionDB.dbo.Resource.Handle AS ID,
	ProductionDB.dbo.Resource.Name AS Name,
	ProductionDB.dbo.Resource.Type AS DeviceType,
	Null AS Role,
	ProductionDB.dbo.Resource.Created AS Purchase,
	Null AS Status
INTO Resource
FROM ProductionDB.dbo.Resource;

--Copy of status
SELECT TOP(100) ProductionDB.dbo.Status.Handle AS Handle,
	ProductionDB.dbo.Status.Status AS Status,
	ProductionDB.dbo.Status.Description AS Description
INTO Status
FROM ProductionDB.dbo.Status;

--Copy of FileName
CREATE TABLE LogFile
(
    Handle INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
    [Name] NVARCHAR(255),
    [FileCreation] DATETIME NOT NULL,
    [Directory] NVARCHAR(255)
);

INSERT INTO LogFiLe ([Name], [FileCreation], [Directory], [DirectoryCreation])
SELECT Distinct TOP(100) 
	ProductionDB.dbo.FileName.Name AS Name,
	ProductionDB.dbo.FileName.Created AS FileCreation,
	ProductionDB.dbo.DirectoryName.Name AS Directory,
	ProductionDB.dbo.DirectoryName.Created AS DirectoryCreation
FROM ProductionDB.dbo.FileName
INNER JOIN ProductionDB.dbo.FileHash
ON ProductionDB.dbo.FileName.Handle = ProductionDB.dbo.FileHash.FileNameHandle
INNER JOIN ProductionDB.dbo.DirectoryName
ON ProductionDB.dbo.DirectoryName.Handle = ProductionDB.dbo.FileHash.DirectoryNameHandle;

--Copy of Attributes
SELECT TOP (100) [Handle]
      ,[Name]
INTO [Attributes]
FROM [ProductionDB].[dbo].[Attributes]
  
--Creation of ActivityAttributes (ActivityTraceField)
CREATE TABLE [ActivityAttributes]
	([Handle] int,
	[Name] varchar(255),
	AttributesHandle int,
	Value varchar(255));

--Creation of ActivityTrace
CREATE TABLE ActivityTrace
	(Handle INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
	ItemHandle int,
	Status varchar(20),
	ResourceName varchar(20),
	Operation varchar(15),
	Program varchar(50),
	LogFileHandle int,
	Created datetime)

--Creation of TestPlan
CREATE TABLE TestPlanStep
	([Handle] int,
	ActivityHandle int,
	StepName varchar(255),
	MeasUnit varchar(255),
	Created datetime)
	
--Creation of DataPump Log
CREATE TABLE DataPumpV2Log
(
    Handle INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
	[ResourceName] NVARCHAR(25),
	[Type] NVARCHAR(15),
	[Context] NVARCHAR(25),
    [Details] NVARCHAR(255),
    [Created] DATETIME
);