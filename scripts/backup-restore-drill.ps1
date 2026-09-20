# PowerShell restore drill against a DISPOSABLE database.
# Does NOT overwrite the source database.
#
# Example:
#   .\scripts\backup-restore-drill.ps1 `
#     -ServerInstance "DESKTOP-30CDIBP\MSSQLSERVER22" `
#     -SourceDatabase "ElectricalStore.Db"

param(
  [Parameter(Mandatory = $true)]
  [string] $ServerInstance,

  [Parameter(Mandatory = $true)]
  [string] $SourceDatabase,

  [string] $BackupDir = "",
  [string] $DrillDatabase = "ElectricalStore_RestoreDrill"
)

$ErrorActionPreference = "Stop"

function Get-SqlScalar([string] $Query) {
  $raw = sqlcmd -S $ServerInstance -E -C -h -1 -W -Q "SET NOCOUNT ON; $Query"
  if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed: $Query" }
  return ($raw | Where-Object { $_ -and $_.Trim() -ne "" } | Select-Object -First 1).Trim()
}

if ([string]::IsNullOrWhiteSpace($BackupDir)) {
  # Use instance default backup folder (SQL service can write here). Do not create subfolders.
  $BackupDir = Get-SqlScalar "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(260));"
}

if (-not (Test-Path -LiteralPath $BackupDir)) {
  throw "BackupDir does not exist or is inaccessible: $BackupDir"
}

$bak = Join-Path $BackupDir ("{0}-ElectricalStore-restore-drill.bak" -f $SourceDatabase)
$bakSql = $bak.Replace("'", "''")
$dataDir = Get-SqlScalar "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(260));"
$logDir = Get-SqlScalar "SELECT CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS nvarchar(260));"
if ([string]::IsNullOrWhiteSpace($dataDir)) { $dataDir = $BackupDir }
if ([string]::IsNullOrWhiteSpace($logDir)) { $logDir = $BackupDir }

Write-Host "1) BACKUP [$SourceDatabase] → $bak"
sqlcmd -S $ServerInstance -E -C -b -Q "BACKUP DATABASE [$SourceDatabase] TO DISK = N'$bakSql' WITH INIT, COMPRESSION, CHECKSUM;"
if ($LASTEXITCODE -ne 0) { throw "BACKUP failed" }

Write-Host "2) DROP disposable DB if present: [$DrillDatabase]"
sqlcmd -S $ServerInstance -E -C -b -Q @"
IF DB_ID(N'$DrillDatabase') IS NOT NULL
BEGIN
  ALTER DATABASE [$DrillDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE [$DrillDatabase];
END
"@

Write-Host "3) Resolve logical file names + RESTORE"
$fileListSql = @"
SET NOCOUNT ON;
DECLARE @fl TABLE (
  LogicalName nvarchar(128),
  PhysicalName nvarchar(260),
  Type char(1),
  FileGroupName nvarchar(128) NULL,
  Size numeric(20,0),
  MaxSize numeric(20,0),
  FileId bigint,
  CreateLSN numeric(25,0),
  DropLSN numeric(25,0) NULL,
  UniqueId uniqueidentifier,
  ReadOnlyLSN numeric(25,0) NULL,
  ReadWriteLSN numeric(25,0) NULL,
  BackupSizeInBytes bigint,
  SourceBlockSize int,
  FileGroupId int,
  LogGroupGUID uniqueidentifier NULL,
  DifferentialBaseLSN numeric(25,0) NULL,
  DifferentialBaseGUID uniqueidentifier NULL,
  IsReadOnly bit,
  IsPresent bit,
  TDEThumbprint varbinary(32) NULL,
  SnapshotUrl nvarchar(360) NULL
);
INSERT INTO @fl EXEC(N'RESTORE FILELISTONLY FROM DISK = N''$bakSql''');
SELECT LogicalName + N'|' + Type FROM @fl;
"@

$fileLines = @(sqlcmd -S $ServerInstance -E -C -h -1 -W -b -Q $fileListSql | Where-Object { $_ -and $_.Trim() -ne "" })
if ($LASTEXITCODE -ne 0 -or $fileLines.Count -lt 2) { throw "FILELISTONLY failed" }

$dataLogical = ($fileLines | Where-Object { $_ -match '\|D$' } | Select-Object -First 1).Split("|")[0]
$logLogical = ($fileLines | Where-Object { $_ -match '\|L$' } | Select-Object -First 1).Split("|")[0]
$dataPath = Join-Path $dataDir "$DrillDatabase.mdf"
$logPath = Join-Path $logDir "$DrillDatabase`_log.ldf"

sqlcmd -S $ServerInstance -E -C -b -Q @"
RESTORE DATABASE [$DrillDatabase]
FROM DISK = N'$bakSql'
WITH REPLACE, RECOVERY, CHECKSUM,
  MOVE N'$dataLogical' TO N'$($dataPath.Replace("'","''"))',
  MOVE N'$logLogical' TO N'$($logPath.Replace("'","''"))';
"@
if ($LASTEXITCODE -ne 0) { throw "RESTORE failed" }

Write-Host "4) VERIFY critical objects"
sqlcmd -S $ServerInstance -E -C -b -d $DrillDatabase -Q @"
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.Orders') IS NULL THROW 50001, 'Orders missing', 1;
IF OBJECT_ID(N'dbo.__AppMigrationsHistory') IS NULL THROW 50002, '__AppMigrationsHistory missing', 1;
SELECT COUNT(*) AS OrderRowCount FROM dbo.Orders;
"@
if ($LASTEXITCODE -ne 0) { throw "VERIFY failed" }

Write-Host "5) DROP disposable DB + remove drill .bak"
sqlcmd -S $ServerInstance -E -C -b -Q @"
ALTER DATABASE [$DrillDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [$DrillDatabase];
"@
Remove-Item -LiteralPath $bak -Force -ErrorAction SilentlyContinue

Write-Host "RESTORE DRILL PASSED"
exit 0
