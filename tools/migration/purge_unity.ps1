$ErrorActionPreference = 'Stop'
$workspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$auditRoot = Join-Path $workspaceRoot 'staging/migration_audit'

function Checked-Path([string]$Path) {
    $resolved = [IO.Path]::GetFullPath($Path)
    $prefix = $workspaceRoot + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or
        $resolved.StartsWith((Join-Path $workspaceRoot '.git'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Migration path outside workspace or inside Git metadata: $resolved"
    }
    if (Test-Path -LiteralPath $resolved) {
        $resolved = (Resolve-Path -LiteralPath $resolved).Path
        if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Resolved path escapes workspace: $resolved" }
        if ((Get-Item -LiteralPath $resolved -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Inspect reparse point separately: $resolved" }
    }
    return $resolved
}

function Merge-Move([string]$Source, [string]$Destination) {
    $sourcePath = Checked-Path $Source
    $destinationPath = Checked-Path $Destination
    if (-not (Test-Path -LiteralPath $sourcePath)) { return }
    if (Test-Path -LiteralPath $destinationPath) {
        if ((Get-Item -LiteralPath $sourcePath).PSIsContainer -and (Get-Item -LiteralPath $destinationPath).PSIsContainer) {
            foreach ($child in Get-ChildItem -LiteralPath $sourcePath -Force) {
                Merge-Move $child.FullName (Join-Path $destinationPath $child.Name)
            }
            return
        }
        throw "Refusing to overwrite migration destination: $destinationPath"
    }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destinationPath) | Out-Null
    Move-Item -LiteralPath $sourcePath -Destination $destinationPath
}

function Rename-Case([string]$Source, [string]$Name) {
    $sourcePath = Checked-Path $Source
    if (-not (Test-Path -LiteralPath $sourcePath)) { return }
    $temporary = Checked-Path ($sourcePath + '__migration_case')
    if (Test-Path -LiteralPath $temporary) { throw "Case-rename temporary already exists: $temporary" }
    $destination = Checked-Path (Join-Path (Split-Path -Parent $sourcePath) $Name)
    Move-Item -LiteralPath $sourcePath -Destination $temporary
    Move-Item -LiteralPath $temporary -Destination $destination
}

function Remove-Checked([string]$Path) {
    $target = Checked-Path $Path
    if (Test-Path -LiteralPath $target) {
        $item = Get-Item -LiteralPath $target -Force
        $script:removed.Add($target.Substring($workspaceRoot.Length+1))
        if ($item.PSIsContainer) { Remove-Item -LiteralPath $target -Recurse -Force }
        else { Remove-Item -LiteralPath $target -Force }
    }
}

$copyReport = Get-Content -LiteralPath (Join-Path $auditRoot 'copy_verification.json') -Raw | ConvertFrom-Json
$inventory = Get-Content -LiteralPath (Join-Path $auditRoot 'preservation_inventory.json') -Raw | ConvertFrom-Json
if (-not $copyReport.passed -or $inventory.serialized_reference_count -lt 1) { throw 'Preservation and serialized-field capture must finish before removal' }
$archiveSha = (& git -C $workspaceRoot rev-parse refs/remotes/origin/archive/unity-pre-migration).Trim()
if ($LASTEXITCODE -ne 0 -or -not $archiveSha) { throw 'Pushed backup branch is required' }

# Promote the existing Godot project; all paths are checked before recursive moves.
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $workspaceRoot 'Godot/assets/models') -Force) {
    $category = if ($file.Name.StartsWith('l1a1_', [StringComparison]::OrdinalIgnoreCase)) { 'environments' } else { 'characters' }
    Merge-Move $file.FullName (Join-Path $workspaceRoot ('assets/'+$category+'/'+$file.Name))
}
Merge-Move (Join-Path $workspaceRoot 'Godot/assets/world') (Join-Path $workspaceRoot 'assets/environments')
foreach ($name in @('audio','ui')) { Merge-Move (Join-Path $workspaceRoot "Godot/assets/$name") (Join-Path $workspaceRoot "assets/$name") }
Merge-Move (Join-Path $workspaceRoot 'assets/audio/dialogue_manifest.json') (Join-Path $workspaceRoot 'assets/audio/dialogue/dialogue_manifest.json')
Merge-Move (Join-Path $workspaceRoot 'Godot/scripts') (Join-Path $workspaceRoot 'src')
Merge-Move (Join-Path $workspaceRoot 'Godot/scenes') (Join-Path $workspaceRoot 'scenes')
Merge-Move (Join-Path $workspaceRoot 'Godot/project.godot') (Join-Path $workspaceRoot 'project.godot')
Merge-Move (Join-Path $workspaceRoot 'Godot/.godot') (Join-Path $workspaceRoot '.godot')
foreach ($name in @('export_templates','feature_profiles','script_templates','text_editor_themes')) {
    Merge-Move (Join-Path $workspaceRoot "Godot/$name") (Join-Path $workspaceRoot "staging/raw_extracted/godot_editor_preferences/$name")
}
Merge-Move (Join-Path $workspaceRoot 'LocalReference') (Join-Path $workspaceRoot 'staging/raw_extracted')
Merge-Move (Join-Path $workspaceRoot 'TestResults') (Join-Path $workspaceRoot 'staging/validation')
Rename-Case (Join-Path $workspaceRoot 'Docs') 'docs'
Merge-Move (Join-Path $workspaceRoot 'research_notes') (Join-Path $workspaceRoot 'docs/research_notes')
Merge-Move (Join-Path $workspaceRoot 'reports') (Join-Path $workspaceRoot 'docs/reports')
foreach ($name in @('Spider.Man.2000','Spider.Man.2000.zip')) {
    Merge-Move (Join-Path $workspaceRoot $name) (Join-Path $workspaceRoot "staging/raw_extracted/install_media/$name")
}
Rename-Case (Join-Path $workspaceRoot 'Tools') 'tools'
Rename-Case (Join-Path $workspaceRoot 'tools/Reference') 'reference'
Rename-Case (Join-Path $workspaceRoot 'tools/ThirdParty') 'third_party'

# Complete byte-for-byte verification before deleting the first Unity directory.
$python = Join-Path $workspaceRoot 'tools/third_party/audio-python/Scripts/python.exe'
& $python (Join-Path $workspaceRoot 'tools/migration/preserve_unity_data.py') verify
if ($LASTEXITCODE -ne 0) { throw 'Final preserved-data verification failed; Unity files have not been deleted' }

$script:removed = [Collections.Generic.List[string]]::new()
foreach ($relative in @('Game','Builds','Logs','tools/Unity','tools/Build','tools/UnityEditor',
        'tools/UnityDownloads','tools/third_party/unity-cli','Library','Temp','Obj','Build',
        'UserSettings','MemoryCaptures','ProjectSettings','Packages')) {
    Remove-Checked (Join-Path $workspaceRoot $relative)
}
$serializedExtensions = @('.meta','.unity','.prefab','.mat','.asset','.anim','.controller',
    '.overridecontroller','.mixer','.playable','.asmdef','.asmref','.inputactions','.uxml','.uss')
foreach ($file in Get-ChildItem -LiteralPath $workspaceRoot -Recurse -File -Force) {
    if ($file.FullName.StartsWith((Join-Path $workspaceRoot '.git'), [StringComparison]::OrdinalIgnoreCase)) { continue }
    if ($file.Extension.ToLowerInvariant() -in $serializedExtensions -or $file.Name -like 'Assembly-CSharp*.csproj' -or $file.Extension -in @('.sln','.slnx')) {
        Remove-Checked $file.FullName
    }
}
$remaining = @(Get-ChildItem -LiteralPath (Join-Path $workspaceRoot 'Godot') -Recurse -File -Force)
if ($remaining.Count -ne 0) { throw 'Unexpected files remain under the old Godot wrapper; inspect before removing it' }
Remove-Checked (Join-Path $workspaceRoot 'Godot')
$report = [ordered]@{ backup_sha=$archiveSha; removed=$script:removed; count=$script:removed.Count; finishedUtc=[DateTime]::UtcNow.ToString('o') }
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $auditRoot 'purge_operations.json') -Encoding utf8
Write-Output "Removed $($script:removed.Count) Unity directories/files after preservation verification."
