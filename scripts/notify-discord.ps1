param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("SUCCESS", "FAILED", "TEST")]
    [string]$Status,

    [string]$Tag = "",
    [string]$Branch = "master",
    [string]$ZipPath = "",
    [string]$ErrorReason = "",
    [string]$LogFile = "",
    [string]$WebhookUrl = ""
)

if (-not $WebhookUrl) {
    $WebhookUrl = $env:DISCORD_WEBHOOK_URL
}

if (-not $WebhookUrl) {
    Write-Warning "Discord webhook URL is not specified."
    exit 0
}

$repo = "aptmara/CreatorKousien"
$codeBlock = [string][char]96 + [char]96 + [char]96

if ($Status -eq "SUCCESS" -or $Status -eq "TEST") {
    $zipName = if ($ZipPath) { [System.IO.Path]::GetFileName($ZipPath) } else { "CreatorKousien_$Tag.zip" }
    $sizeStr = "N/A"
    if ($ZipPath -and (Test-Path $ZipPath)) {
        $bytes = (Get-Item $ZipPath).Length
        $sizeStr = "$([math]::Round($bytes / 1MB, 2)) MB"
    }

    $releaseUrl = "https://github.com/$repo/releases/tag/$Tag"
    $downloadUrl = "https://github.com/$repo/releases/download/$Tag/$zipName"

    $title = if ($Status -eq "TEST") { "【テスト通知】CreatorKousien ビルド・リリース成功" } else { "【CreatorKousien ビルド・リリース成功】" }

    $msgLines = @(
        ":white_check_mark: **$title**",
        "・バージョン (タグ): $Tag",
        "・ブランチ: $Branch",
        "・成果物: $zipName ($sizeStr)",
        "・配布先 (Release): $releaseUrl",
        "・直接ダウンロード: $downloadUrl"
    )
    $msg = $msgLines -join "`n"
} else {
    $msgLines = [System.Collections.Generic.List[string]]::new()
    $msgLines.Add(":x: **【CreatorKousien ビルド・リリース失敗】**")
    $msgLines.Add("・失敗ステップ / 理由: $ErrorReason")
    $msgLines.Add("・ブランチ: $Branch")

    if ($LogFile -and (Test-Path $LogFile)) {
        try {
            $lines = Get-Content -Path $LogFile -ErrorAction SilentlyContinue
            if ($lines) {
                $matched = $lines | Select-String -Pattern "error", "Exception", "Build failed", "Failed" -CaseSensitive:$false | Select-Object -Last 6
                $snippet = ""
                if ($matched) {
                    $snippet = ($matched | ForEach-Object { $_.Line.Trim() }) -join "`n"
                } else {
                    $snippet = ($lines | Select-Object -Last 8) -join "`n"
                }
                if ($snippet.Length -gt 900) {
                    $snippet = $snippet.Substring($snippet.Length - 900)
                }
                if ($snippet) {
                    $msgLines.Add("・ログ抜粋 (エラー周辺):")
                    $msgLines.Add($codeBlock + "text")
                    $msgLines.Add($snippet)
                    $msgLines.Add($codeBlock)
                }
            }
        } catch {
        }
    }
    $msg = $msgLines -join "`n"
}

$payload = @{
    content = $msg
}

$json = $payload | ConvertTo-Json
$bytes = [System.Text.Encoding]::UTF8.GetBytes($json)

try {
    $response = Invoke-RestMethod -Uri $WebhookUrl -Method Post -ContentType "application/json; charset=utf-8" -Body $bytes
    Write-Host "[INFO] Discord notification sent successfully."
} catch {
    Write-Warning "Failed to send Discord notification: $($_.Exception.Message)"
}
