# Caminhos de origem
$databaseName = "AdministraAoImoveis"
$backupRoot = "D:\\Imobiliaria\\Backups"
$attachmentsPath = "D:\\Imobiliaria\\Arquivos"

# Arquivo de saída
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupFolder = Join-Path $backupRoot $timestamp
New-Item -ItemType Directory -Force -Path $backupFolder | Out-Null

# Backup do banco LocalDB
$sqlBackup = Join-Path $backupFolder "$databaseName.bak"
& sqlcmd -S "(localdb)\\MSSQLLocalDB" -Q "BACKUP DATABASE [$databaseName] TO DISK = '$sqlBackup' WITH INIT"

# Cópia dos anexos
$attachmentsBackup = Join-Path $backupFolder "Arquivos"
Copy-Item -Path $attachmentsPath -Destination $attachmentsBackup -Recurse -Force

# Registro simples em log
$logLine = "Backup executado em $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss') para $databaseName"
Add-Content -Path (Join-Path $backupRoot "backup.log") -Value $logLine
