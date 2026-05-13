param(
    [string]$ExecutableDirectory = "C:\Users\rened\Documents\Grid3-YouTube-Accessibility-Addon\src\YouTubeControl\bin\Debug\net10.0-windows",
    [string]$ExecutableName = "YouTubeControl.exe",
    [int]$InitialWaitSeconds = 15,
    [int]$NormalDelaySeconds = 5,
    [int]$HomeDelaySeconds = 7,
    [int]$LeaderStartAttempts = 5,
    [int]$LeaderStartValidationSeconds = 2,
    [switch]$StopOnFailure
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$exePath = Join-Path $ExecutableDirectory $ExecutableName
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Executable not found: $exePath"
}

$processName = [System.IO.Path]::GetFileNameWithoutExtension($ExecutableName)

function Get-AppProcesses {
    param([string]$Name)
    return Get-Process -Name $Name -ErrorAction SilentlyContinue
}

function Stop-AppProcesses {
    param(
        [string]$Name,
        [int]$MaxAttempts = 3
    )

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        $running = Get-AppProcesses -Name $Name
        if (-not $running) {
            if ($attempt -eq 1) {
                Write-Host "[INFO] No running process found."
            } else {
                Write-Host "[INFO] Process cleanup verified."
            }

            return
        }

        $ids = ($running | Select-Object -ExpandProperty Id) -join ", "
        Write-Host ("[INFO] Stop attempt {0}/{1} for PID(s): {2}" -f $attempt, $MaxAttempts, $ids)

        $running | Stop-Process -Force -ErrorAction SilentlyContinue

        foreach ($processId in ($running | Select-Object -ExpandProperty Id)) {
            try {
                Wait-Process -Id $processId -Timeout 5 -ErrorAction Stop
            }
            catch {
                Start-Sleep -Milliseconds 300
            }
        }
    }

    $remaining = Get-AppProcesses -Name $Name
    if ($remaining) {
        $remainingIds = ($remaining | Select-Object -ExpandProperty Id) -join ", "
        throw "Failed to stop existing process(es): $remainingIds"
    }
}

function Start-Leader {
    param(
        [string]$ExePath,
        [string]$Name,
        [int]$MaxAttempts,
        [int]$ValidationSeconds
    )

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        $leader = Start-Process -FilePath $ExePath -NoNewWindow -PassThru
        Write-Host ("[INFO] Started no-arg process PID: {0} (attempt {1}/{2})" -f $leader.Id, $attempt, $MaxAttempts)

        Start-Sleep -Seconds $ValidationSeconds
        $running = Get-Process -Id $leader.Id -ErrorAction SilentlyContinue
        if ($running) {
            Write-Host "[INFO] Leader process is running and ready."
            return
        }

        Write-Host "[WARN] Started process exited too quickly. Retrying leader start..."
        Stop-AppProcesses -Name $Name -MaxAttempts 2
        Start-Sleep -Milliseconds 300
    }

    throw "Failed to start a persistent leader process after $MaxAttempts attempts."
}

function Get-ActionName {
    param([string]$Command)

    $firstColon = $Command.IndexOf(':')
    if ($firstColon -lt 0) {
        return $Command.Trim().ToLowerInvariant()
    }

    return $Command.Substring(0, $firstColon).Trim().ToLowerInvariant()
}

# Full regression flow that covers all supported actions in realistic contexts.
$testSteps = @(
    [pscustomobject]@{ Label = "Home warmup"; Command = "home" },
    [pscustomobject]@{ Label = "Home navigate down"; Command = "down" },
    [pscustomobject]@{ Label = "Home navigate up"; Command = "up" },
    [pscustomobject]@{ Label = "Home select video"; Command = "enter" },
    [pscustomobject]@{ Label = "Regular video play_pause on"; Command = "play_pause" },
    [pscustomobject]@{ Label = "Regular video play_pause off"; Command = "play_pause" },
    [pscustomobject]@{ Label = "Regular video like on"; Command = "like" },
    [pscustomobject]@{ Label = "Regular video like off"; Command = "like" },
    [pscustomobject]@{ Label = "Regular video fullscreen on"; Command = "fullscreen" },
    [pscustomobject]@{ Label = "Regular video fullscreen off"; Command = "fullscreen" },
    [pscustomobject]@{ Label = "Regular video toggle command"; Command = "toggle" },
    [pscustomobject]@{ Label = "Regular video refresh"; Command = "refresh" },
    [pscustomobject]@{ Label = "Back from regular video"; Command = "back" },

    [pscustomobject]@{ Label = "Open Shorts page"; Command = "open:https://www.youtube.com/shorts" },
    [pscustomobject]@{ Label = "Shorts next video"; Command = "down" },
    [pscustomobject]@{ Label = "Shorts play_pause on"; Command = "play_pause" },
    [pscustomobject]@{ Label = "Shorts play_pause off"; Command = "play_pause" },
    [pscustomobject]@{ Label = "Shorts like on"; Command = "like" },
    [pscustomobject]@{ Label = "Shorts like off"; Command = "like" },
    [pscustomobject]@{ Label = "Shorts fullscreen on"; Command = "fullscreen" },
    [pscustomobject]@{ Label = "Shorts fullscreen off"; Command = "fullscreen" },
    [pscustomobject]@{ Label = "Return home"; Command = "home" },

    [pscustomobject]@{ Label = "Search results"; Command = "search:kids songs" },
    [pscustomobject]@{ Label = "Search navigate"; Command = "down" },
    [pscustomobject]@{ Label = "Search open item"; Command = "enter" },
    [pscustomobject]@{ Label = "Back from search video"; Command = "back" },

    [pscustomobject]@{ Label = "Exit command"; Command = "exit" },
    [pscustomobject]@{ Label = "Home after restart"; Command = "home" },
    [pscustomobject]@{ Label = "Stop alias command"; Command = "stop" }
)

$expectedActions = @(
    "home", "up", "down", "enter", "back", "play_pause", "fullscreen", "toggle", "like", "search", "open", "exit", "stop", "refresh"
)

$coveredActions = $testSteps |
    ForEach-Object { Get-ActionName -Command $_.Command } |
    Select-Object -Unique

$missingActions = @($expectedActions | Where-Object { $_ -notin $coveredActions })
if ($missingActions.Count -gt 0) {
    throw "Test sequence is missing actions: $($missingActions -join ', ')"
}

Write-Host "[INFO] Stopping previous $ExecutableName processes if running..."
Stop-AppProcesses -Name $processName

Push-Location $ExecutableDirectory
try {
    Write-Host "[INFO] Starting no-arg process..."
    Start-Leader -ExePath $exePath -Name $processName -MaxAttempts $LeaderStartAttempts -ValidationSeconds $LeaderStartValidationSeconds

    Write-Host ("[INFO] Waiting {0} seconds before first command..." -f $InitialWaitSeconds)
    Start-Sleep -Seconds $InitialWaitSeconds

    $results = New-Object System.Collections.Generic.List[object]

    for ($i = 0; $i -lt $testSteps.Count; $i++) {
        $step = $testSteps[$i]
        $command = $step.Command
        $action = Get-ActionName -Command $command

        Write-Host ("[{0:HH:mm:ss}] Step {1}/{2} | {3} | .\\{4} {5}" -f (Get-Date), ($i + 1), $testSteps.Count, $step.Label, $ExecutableName, $command)

        $proc = Start-Process -FilePath $exePath -ArgumentList $command -NoNewWindow -Wait -PassThru
        $exitCode = $proc.ExitCode

        $results.Add([pscustomobject]@{
            Step = $i + 1
            Label = $step.Label
            Action = $action
            Command = $command
            ExitCode = $exitCode
        }) | Out-Null

        Write-Host ("  -> exit code: {0}" -f $exitCode)

        if ($StopOnFailure -and $exitCode -ne 0) {
            throw "Command '$command' failed with exit code $exitCode at step $($i + 1)."
        }

        $isTerminalAction = $action -in @("exit", "stop")

        if ($isTerminalAction -and $i -lt ($testSteps.Count - 1)) {
            Write-Host ("  -> restarting leader after {0}" -f $action)
            Stop-AppProcesses -Name $processName
            Start-Leader -ExePath $exePath -Name $processName -MaxAttempts $LeaderStartAttempts -ValidationSeconds $LeaderStartValidationSeconds
        }

        if ($i -lt ($testSteps.Count - 1)) {
            $delaySeconds = if ($action -eq "home") { $HomeDelaySeconds } else { $NormalDelaySeconds }
            Write-Host ("  -> waiting {0}s" -f $delaySeconds)
            Start-Sleep -Seconds $delaySeconds
        }
    }

    Write-Host ""
    Write-Host "=== Command Summary ==="
    $results | Format-Table -AutoSize
}
finally {
    Pop-Location
}
