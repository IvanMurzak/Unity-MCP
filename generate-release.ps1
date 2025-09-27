param(
    [Parameter(Mandatory = $true)]
    [string]$VersionFrom,

    [Parameter(Mandatory = $true)]
    [string]$VersionTo
)

# Get repository URL from git remote
$repoUrl = (git remote get-url origin) -replace '\.git$', ''
if ($repoUrl -match '^git@github\.com:(.+)') {
    $repoUrl = "https://github.com/$($matches[1])"
}

$filename = "release_${VersionFrom}_to_${VersionTo}.md"

# Clear existing release.md if it exists
if (Test-Path $filename) {
    Remove-Item $filename
}

# Add comparison section
Add-Content -Path $filename -Value "## Comparison"
Add-Content -Path $filename -Value "See every change: [Compare $VersionFrom...$VersionTo]($repoUrl/compare/$VersionFrom...$VersionTo)"
Add-Content -Path $filename -Value ""
Add-Content -Path $filename -Value "---"
Add-Content -Path $filename -Value ""

# Add commit summary section
Add-Content -Path $filename -Value "## Commit Summary (Newest → Oldest)"

# Get commit SHAs from previous version to target version
$commits = git log --pretty=format:'%H' "$VersionFrom..$VersionTo"

foreach ($sha in $commits) {
    # Get username via GitHub API with retry logic
    $repoPath = ($repoUrl -replace 'https://github.com/', '')
    $username = $null

    $commitData = gh api "repos/$repoPath/commits/$sha" --jq '.author.login // .commit.author.name' 2>$null
    if ($commitData -and $commitData.Trim() -ne '' -and -not $commitData.StartsWith('{')) {
        $username = $commitData
    }
    elseif ($commitData -and $commitData.StartsWith('{')) {
        Write-Host "GitHub API error for commit $sha`: $commitData" -ForegroundColor Red
    }

    # Fallback to git commit author name if all GitHub API attempts fail
    if (-not $username) {
        $username = git log -1 --pretty=format:'%an' $sha
        Write-Host "Using git fallback for commit $sha" -ForegroundColor Yellow
        continue;
    }

    # Skip commit if we still couldn't get a valid username
    if (-not $username -or $username.StartsWith('{')) {
        $message = git log -1 --pretty=format:'%s' $sha
        Write-Host "ERROR: Failed to get author for commit. Skipping: $repoUrl/commit/$sha - $message" -ForegroundColor Red
        continue
    }

    # Get commit message and short SHA
    $message = git log -1 --pretty=format:'%s' $sha
    $shortSha = git log -1 --pretty=format:'%h' $sha | ForEach-Object { $_.Substring(0, 6) }

    # Add commit line to release.md
    Add-Content -Path $filename -Value "- [``$shortSha``]($repoUrl/commit/$sha) — $message by @$username"
}

Write-Host "Release notes generated successfully in $filename"