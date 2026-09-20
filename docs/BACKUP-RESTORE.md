# SQL Server backup and restore

> **Purpose:** Minimal operational backup plan for ElectricalStore.  
> **Authority:** Ops checklist — not automated in-repo.

## Principles

1. Backup **before** every production migration.  
2. Keep at least one copy **off the VPS** (object storage / another host).  
3. A backup without a **restore test** is incomplete.

## Recommended MVP schedule

| Item | Recommendation |
|------|----------------|
| Full backup | Daily (or before each release if low change rate) |
| Retention on-server | 7 days |
| Off-server copy | Daily sync; retain ≥ 14–30 days |
| Pre-migrate | Extra named backup (`pre-migrate-YYYYMMDD`) |

Exact tooling depends on hosting (SQL Server Agent, `BACKUP DATABASE`, managed SQL, etc.). This repo does not ship backup jobs.

## Example (self-hosted SQL Server)

```sql
-- Full backup (adjust paths/permissions for your host)
BACKUP DATABASE [ElectricalStore]
TO DISK = N'/var/backups/electricalstore/ElectricalStore_full.bak'
WITH INIT, COMPRESSION, CHECKSUM;
```

Copy `.bak` off-server immediately after success.

## Restore drill (required before launch)

Operator script (Windows Dev SQL example — **disposable** DB only):

```powershell
.\scripts\backup-restore-drill.ps1 `
  -ServerInstance "YOUR_INSTANCE" `
  -SourceDatabase "ElectricalStore.Db"
```

Success criteria printed as `RESTORE DRILL PASSED`:

1. BACKUP source DB  
2. RESTORE into `ElectricalStore_RestoreDrill` (never overwrites source)  
3. Verify `dbo.Orders` + `__AppMigrationsHistory`  
4. DROP disposable DB  

Record date of last successful drill in ops notes. Documentation alone does **not** clear the release blocker.

## Rollback expectations

- Schema migrations are generally **forward-only**; rollback = restore DB to pre-migrate backup + redeploy previous app bits.  
- Do not assume EF `Down` migrations are exercised in Production.

## Related

- Migration order: Permixa IAM first, then App (`docs/DEVELOPMENT-GUIDE.md`)  
- Production topology: [PRODUCTION.md](./PRODUCTION.md)  
- Production must not auto-migrate on startup
