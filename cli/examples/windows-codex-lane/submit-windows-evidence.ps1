param(
  [Parameter(Mandatory = $true)]
  [string]$ProjectPath,

  [Parameter(Mandatory = $true)]
  [string]$HandoffId,

  [Parameter(Mandatory = $true)]
  [int]$HandoffVersion,

  [string]$LaneId = "windows-runner-1",
  [ValidateSet("passed", "failed", "blocked")]
  [string]$Outcome = "passed",
  [string]$Summary = "Windows validation completed.",
  [string]$LogPath = "C:\\unity-mcp-agent\\logs\\worker-1.log",
  [string]$TestReportPath = "C:\\unity-mcp-agent\\outbox\\vitest.xml",
  [string]$CliPath = ""
)

$tempDir = Join-Path $env:TEMP "unity-mcp-windows-evidence"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
$payloadPath = Join-Path $tempDir "$($HandoffId)-windows-evidence.json"

$payload = @{
  schemaVersion = 1
  kind = "windows_lane_evidence_envelope"
  handoffId = $HandoffId
  handoffVersion = $HandoffVersion
  sourceLane = @{
    kind = "windows_codex"
    laneId = $LaneId
  }
  submittedAt = [DateTimeOffset]::UtcNow.ToString("o")
  outcome = $Outcome
  summary = $Summary
  evidenceRefs = @(
    @{
      type = "log"
      uri = ("file:///" + ($LogPath -replace "\\", "/"))
    },
    @{
      type = "test_report"
      uri = ("file:///" + ($TestReportPath -replace "\\", "/"))
    }
  )
}

$payload | ConvertTo-Json -Depth 6 | Set-Content -Path $payloadPath -Encoding UTF8

Write-Host "Wrote bounded Windows evidence envelope: $payloadPath"
Write-Host "Queueing into Unity-MCP..."

if ([string]::IsNullOrWhiteSpace($CliPath)) {
  $CliPath = Join-Path $PSScriptRoot "..\\..\\bin\\unity-mcp-cli.js"
}

node $CliPath handoff submit-windows-evidence $ProjectPath --input-file $payloadPath
