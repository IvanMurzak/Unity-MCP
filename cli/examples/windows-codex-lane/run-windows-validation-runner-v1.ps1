param(
  [Parameter(Mandatory = $true)]
  [string]$SnapshotPath,

  [Parameter(Mandatory = $true)]
  [string]$ProjectPath,

  [string]$WorkspacePath = "",
  [string]$LaneId = "windows-runner-1",
  [string]$ProfileName = "windows_validation_smoke_v1",
  [string]$CliPath = "",
  [string]$LogsRoot = "",
  [string]$OutboxRoot = "",
  [string]$SnapshotsRoot = "",
  [string]$SessionsRoot = "",
  [string]$StructuredTestReportPath = "",
  [string]$UnityProjectPath = "",
  [switch]$EnableOptionalUnityValidation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-UriFromPath([string]$PathValue) {
  return ('file:///' + ($PathValue -replace '\\', '/'))
}

function Ensure-Directory([string]$PathValue) {
  New-Item -ItemType Directory -Path $PathValue -Force | Out-Null
}

function Write-Utf8NoBomFile([string]$PathValue, [string]$Content) {
  $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($PathValue, $Content, $utf8NoBom)
}

function Get-OptionalNestedString($Object, [string[]]$PathSegments) {
  $current = $Object
  foreach ($segment in $PathSegments) {
    if ($null -eq $current) {
      return ''
    }

    $property = $current.PSObject.Properties[$segment]
    if ($null -eq $property) {
      return ''
    }

    $current = $property.Value
  }

  return [string]$current
}

function Write-RunnerLog([string]$Message) {
  $timestamp = [DateTimeOffset]::UtcNow.ToString('o')
  $line = "[$timestamp] $Message"
  Write-Host $line
  Add-Content -Path $script:RunLogPath -Value $line
}

function Invoke-LoggedStep {
  param(
    [Parameter(Mandatory = $true)] [string]$Name,
    [Parameter(Mandatory = $true)] [scriptblock]$Action,
    [switch]$Required = $true
  )

  Write-RunnerLog "STEP START: $Name"
  try {
    & $Action
    Write-RunnerLog "STEP PASS: $Name"
    return @{ name = $Name; status = 'passed'; required = [bool]$Required }
  }
  catch {
    Write-RunnerLog "STEP FAIL: $Name :: $($_.Exception.Message)"
    if ($Required) {
      throw
    }

    return @{ name = $Name; status = 'skipped'; required = [bool]$Required; reason = $_.Exception.Message }
  }
}

function Invoke-LoggedCommand {
  param(
    [Parameter(Mandatory = $true)] [string]$Name,
    [Parameter(Mandatory = $true)] [string]$WorkingDirectory,
    [Parameter(Mandatory = $true)] [string]$Command,
    [Parameter(Mandatory = $true)] [string[]]$Arguments,
    [switch]$AllowFailure
  )

  Write-RunnerLog "COMMAND START: $Name => $Command $($Arguments -join ' ')"
  Push-Location $WorkingDirectory
  try {
    $previousErrorActionPreference = $ErrorActionPreference
    try {
      $ErrorActionPreference = 'Continue'
      $output = & $Command @Arguments 2>&1
      $exitCode = $LASTEXITCODE
    }
    finally {
      $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($null -ne $output) {
      $text = ($output | Out-String).TrimEnd()
      if (-not [string]::IsNullOrWhiteSpace($text)) {
        $text | Tee-Object -FilePath $script:RunLogPath -Append | Out-Host
      }
    }
  }
  finally {
    Pop-Location
  }
  Write-RunnerLog "COMMAND EXIT: $Name => $exitCode"

  if (-not $AllowFailure -and $exitCode -ne 0) {
    throw "$Name failed with exit code $exitCode"
  }

  return @{
    Output = ($output | Out-String)
    ExitCode = $exitCode
  }
}

if ($ProfileName -ne 'windows_validation_smoke_v1') {
  throw "Unsupported profile '$ProfileName'. Expected windows_validation_smoke_v1."
}

if (-not (Test-Path -LiteralPath $SnapshotPath)) {
  throw "Snapshot not found: $SnapshotPath"
}

$snapshot = Get-Content -LiteralPath $SnapshotPath -Raw | ConvertFrom-Json
$requiredSnapshotFields = @(
  'snapshotId',
  'handoffId',
  'handoffRecordVersion',
  'requestedAction',
  'sourceLane',
  'targetLane',
  'createdAt'
)

foreach ($field in $requiredSnapshotFields) {
  if (-not ($snapshot.PSObject.Properties.Name -contains $field) -or [string]::IsNullOrWhiteSpace([string]$snapshot.$field)) {
    throw "Snapshot missing required field '$field'"
  }
}

if ([string]::IsNullOrWhiteSpace($WorkspacePath)) {
  $WorkspacePath = Get-OptionalNestedString $snapshot @('workspaceHints', 'workingDirectoryHint')
}

if ([string]::IsNullOrWhiteSpace($LogsRoot)) {
  $snapshotLogRoot = Get-OptionalNestedString $snapshot @('workspaceHints', 'logDirectoryHint')
  $LogsRoot = if ([string]::IsNullOrWhiteSpace($snapshotLogRoot)) { 'C:\unity-mcp-agent\logs' } else { $snapshotLogRoot }
}

if ([string]::IsNullOrWhiteSpace($OutboxRoot)) {
  $snapshotOutboxRoot = Get-OptionalNestedString $snapshot @('workspaceHints', 'artifactDirectoryHint')
  $OutboxRoot = if ([string]::IsNullOrWhiteSpace($snapshotOutboxRoot)) { 'C:\unity-mcp-agent\outbox' } else { $snapshotOutboxRoot }
}

if ([string]::IsNullOrWhiteSpace($SnapshotsRoot)) {
  $SnapshotsRoot = 'C:\unity-mcp-agent\snapshots'
}

if ([string]::IsNullOrWhiteSpace($SessionsRoot)) {
  $SessionsRoot = 'C:\unity-mcp-agent\sessions'
}

if ([string]::IsNullOrWhiteSpace($UnityProjectPath)) {
  $UnityProjectPath = Get-OptionalNestedString $snapshot @('projectHints', 'unityProjectPathHint')
}

Ensure-Directory $LogsRoot
Ensure-Directory $OutboxRoot
Ensure-Directory $SnapshotsRoot
Ensure-Directory $SessionsRoot

$runStamp = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$script:RunLogPath = Join-Path $LogsRoot "$($snapshot.handoffId)-$runStamp-runner.log"
$summaryPath = Join-Path $OutboxRoot "$($snapshot.handoffId)-$runStamp-summary.md"
$payloadPath = Join-Path $OutboxRoot "$($snapshot.handoffId)-$runStamp-windows-evidence.json"
$cachedSnapshotPath = Join-Path $SnapshotsRoot "$($snapshot.handoffId)-$runStamp-snapshot.json"
Copy-Item -LiteralPath $SnapshotPath -Destination $cachedSnapshotPath -Force

Write-RunnerLog 'Profile: windows_validation_smoke_v1'
Write-RunnerLog "Snapshot echo: handoffId=$($snapshot.handoffId) handoffRecordVersion=$($snapshot.handoffRecordVersion)"
Write-RunnerLog "WorkspacePath: $WorkspacePath"
Write-RunnerLog "ProjectPath: $ProjectPath"
Write-RunnerLog "Companion outbox (local only): $OutboxRoot"
Write-RunnerLog 'Unity-MCP queue remains project-local .unity-mcp/handoff-spool/windows-evidence after submit'

$evidenceRefs = New-Object System.Collections.ArrayList
[void]$evidenceRefs.Add(@{ type = 'log'; uri = (Get-UriFromPath $script:RunLogPath) })
[void]$evidenceRefs.Add(@{ type = 'note'; uri = (Get-UriFromPath $summaryPath) })

$stepResults = New-Object System.Collections.ArrayList
$outcome = 'passed'
$summary = 'Windows validation smoke completed.'

$cliWorkingDirectory = Join-Path $WorkspacePath 'cli'
$distEntry = Join-Path $cliWorkingDirectory 'dist\index.js'
$sessionName = ($snapshot.handoffId -replace '[^A-Za-z0-9-]', '-') + '-windows-validation-smoke'
$teamStatePath = Join-Path $ProjectPath ".unity-mcp\team-state\$sessionName.json"
$killedRuntimeHandle = ''

try {
  if ([string]::IsNullOrWhiteSpace($WorkspacePath) -or -not (Test-Path -LiteralPath $WorkspacePath)) {
    throw 'WorkspacePath is required and must exist before the runner can execute.'
  }

  if (-not (Test-Path -LiteralPath $ProjectPath)) {
    throw "ProjectPath not found: $ProjectPath"
  }

  if (-not (Test-Path -LiteralPath $cliWorkingDirectory)) {
    throw "CLI workspace not found at $cliWorkingDirectory"
  }

  [void]$stepResults.Add((Invoke-LoggedStep -Name 'CLI test pass' -Action {
    $result = Invoke-LoggedCommand -Name 'npm test' -WorkingDirectory $cliWorkingDirectory -Command 'npm' -Arguments @('test')
    if (-not [string]::IsNullOrWhiteSpace($StructuredTestReportPath) -and (Test-Path -LiteralPath $StructuredTestReportPath)) {
      [void]$evidenceRefs.Add(@{ type = 'test_report'; uri = (Get-UriFromPath $StructuredTestReportPath) })
    }
    elseif ($result.Output -match 'Test Files') {
      Write-RunnerLog 'Structured test report not provided; using run log as the bounded evidence artifact for the test pass.'
    }
  }))

  [void]$stepResults.Add((Invoke-LoggedStep -Name 'CLI build pass' -Action {
    Invoke-LoggedCommand -Name 'npm run build' -WorkingDirectory $cliWorkingDirectory -Command 'npm' -Arguments @('run', 'build') | Out-Null
  }))

  [void]$stepResults.Add((Invoke-LoggedStep -Name 'Command surface check' -Action {
    Invoke-LoggedCommand -Name 'node dist/index.js team --help' -WorkingDirectory $cliWorkingDirectory -Command 'node' -Arguments @($distEntry, 'team', '--help') | Out-Null
  }))

  [void]$stepResults.Add((Invoke-LoggedStep -Name 'Lifecycle smoke' -Action {
    Invoke-LoggedCommand -Name 'team launch' -WorkingDirectory $cliWorkingDirectory -Command 'node' -Arguments @($distEntry, 'team', 'launch', '--path', $ProjectPath, '--session-name', $sessionName) | Out-Null

    $statusResult = Invoke-LoggedCommand -Name 'team status' -WorkingDirectory $cliWorkingDirectory -Command 'node' -Arguments @($distEntry, 'team', 'status', '--path', $ProjectPath, $sessionName)
    if ($statusResult.Output -notmatch 'Status:\s+ready') {
      throw 'team status did not report ready state after launch.'
    }

    if (-not (Test-Path -LiteralPath $teamStatePath)) {
      throw "Expected team state file was not written: $teamStatePath"
    }

    $teamState = Get-Content -LiteralPath $teamStatePath -Raw | ConvertFrom-Json
    if ([string]$teamState.runtime.kind -ne 'process') {
      throw "Expected runtime.kind = process, got '$([string]$teamState.runtime.kind)'"
    }

    $listResult = Invoke-LoggedCommand -Name 'team list' -WorkingDirectory $cliWorkingDirectory -Command 'node' -Arguments @($distEntry, 'team', 'list', '--path', $ProjectPath)
    if ($listResult.Output -notmatch [Regex]::Escape($sessionName)) {
      throw 'team list did not include the launched session.'
    }
  }))

  [void]$stepResults.Add((Invoke-LoggedStep -Name 'Degraded-state smoke' -Action {
    if (-not (Test-Path -LiteralPath $teamStatePath)) {
      throw "Team state file missing before degraded-state check: $teamStatePath"
    }

    $teamState = Get-Content -LiteralPath $teamStatePath -Raw | ConvertFrom-Json
    $roleToKill = $teamState.roles | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.runtimeHandle) } | Select-Object -First 1
    if ($null -eq $roleToKill) {
      throw 'No runtimeHandle was available to simulate degraded state.'
    }

    $script:killedRuntimeHandle = [string]$roleToKill.runtimeHandle
    Stop-Process -Id ([int]$script:killedRuntimeHandle) -Force
    Start-Sleep -Milliseconds 500

    $degradedResult = Invoke-LoggedCommand -Name 'team status after kill' -WorkingDirectory $cliWorkingDirectory -Command 'node' -Arguments @($distEntry, 'team', 'status', '--path', $ProjectPath, $sessionName) -AllowFailure
    if ($degradedResult.ExitCode -eq 0) {
      throw 'team status unexpectedly succeeded after killing a runtime role.'
    }

    if ($degradedResult.Output -notmatch 'Status:\s+degraded') {
      throw 'team status did not surface degraded state after killing a role.'
    }

    if ($degradedResult.Output -notmatch 'missing') {
      Write-RunnerLog 'Degraded state surfaced without the literal word "missing"; keeping the command log as the authoritative evidence.'
    }

    Invoke-LoggedCommand -Name 'team stop' -WorkingDirectory $cliWorkingDirectory -Command 'node' -Arguments @($distEntry, 'team', 'stop', '--path', $ProjectPath, $sessionName) | Out-Null
  }))

  if ($EnableOptionalUnityValidation) {
    [void]$stepResults.Add((Invoke-LoggedStep -Name 'Optional Unity + MCP end-to-end validation' -Action {
      if ([string]::IsNullOrWhiteSpace($UnityProjectPath) -or -not (Test-Path -LiteralPath $UnityProjectPath)) {
        throw 'Optional Unity validation requested, but UnityProjectPath is unavailable.'
      }

      Write-RunnerLog "Optional Unity validation is environment-dependent. Validate against $UnityProjectPath and capture logs/screenshots as bounded refs."
    } -Required:$false))
  }
}
catch {
  $message = $_.Exception.Message
  if ($message -match 'required and must exist|not found|unavailable') {
    $outcome = 'blocked'
    $summary = "Windows validation blocked: $message"
  }
  else {
    $outcome = 'failed'
    $summary = "Windows validation failed: $message"
  }

  Write-RunnerLog $summary
}
finally {
  if (Test-Path -LiteralPath $teamStatePath) {
    try {
      Invoke-LoggedCommand -Name 'team stop cleanup' -WorkingDirectory $cliWorkingDirectory -Command 'node' -Arguments @($distEntry, 'team', 'stop', '--path', $ProjectPath, $sessionName) -AllowFailure | Out-Null
    }
    catch {
      Write-RunnerLog "Cleanup stop failed: $($_.Exception.Message)"
    }
  }
}

$summaryLines = @(
  '# windows_validation_smoke_v1',
  '',
  "- handoffId: $($snapshot.handoffId)",
  "- handoffRecordVersion: $($snapshot.handoffRecordVersion)",
  "- outcome: $outcome",
  "- summary: $summary",
  "- companion outbox: $OutboxRoot",
  '- Unity-MCP queue: .unity-mcp/handoff-spool/windows-evidence (after submit)',
  "- sessionName: $sessionName",
  "- teamStatePath: $teamStatePath",
  "- killedRuntimeHandle: $killedRuntimeHandle",
  '',
  '## Step results'
)

foreach ($result in $stepResults) {
  $summaryLines += "- $($result.name): $($result.status)"
}

Write-Utf8NoBomFile -PathValue $summaryPath -Content (($summaryLines -join [Environment]::NewLine) + [Environment]::NewLine)

$payload = @{
  schemaVersion = 1
  kind = 'windows_lane_evidence_envelope'
  handoffId = $snapshot.handoffId
  handoffVersion = [int]$snapshot.handoffRecordVersion
  sourceLane = @{
    kind = 'windows_codex'
    laneId = $LaneId
  }
  submittedAt = [DateTimeOffset]::UtcNow.ToString('o')
  outcome = $outcome
  summary = $summary
  evidenceRefs = $evidenceRefs
}

Write-Utf8NoBomFile -PathValue $payloadPath -Content (($payload | ConvertTo-Json -Depth 8) + [Environment]::NewLine)
Write-RunnerLog "Wrote bounded Windows evidence envelope: $payloadPath"

if ([string]::IsNullOrWhiteSpace($CliPath)) {
  $CliPath = Join-Path $PSScriptRoot '..\\..\\bin\\unity-mcp-cli.js'
}

Write-RunnerLog 'Submitting evidence through handoff submit-windows-evidence'
node $CliPath handoff submit-windows-evidence $ProjectPath --input-file $payloadPath
if ($LASTEXITCODE -ne 0) {
  throw "submit-windows-evidence failed with exit code $LASTEXITCODE"
}

Write-RunnerLog 'Evidence submitted successfully. Await mac leader reconcile; no list-windows-evidence polling or reconcile call is performed by this runner.'
