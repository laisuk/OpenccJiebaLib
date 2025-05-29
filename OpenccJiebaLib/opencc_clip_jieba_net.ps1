[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $Config = "auto",
    [string]
    $Punct = ""
)

# Load the assembly
Add-Type -Path "${PSScriptRoot}\bin\Release\netstandard2.0\OpenccJiebaLib.dll"

# Define ANSI escape codes for colors
$Red = "`e[1;31m"
$Green = "`e[1;32m"
$Yellow = "`e[1;33m"
$Blue = "`e[1;34m"
$Reset = "`e[0m"

# Punctuation flag
$Punctuation = ($Punct.ToLower() -eq "punct")

Write-Host "Opencc-Clip-Jieba-Net version 1.0.0 Copyright (c) 2024 Bryan Lai"

# Handle help configuration
if ($Config -eq "help") {
    Write-Host "Usage: opencc_clip_jieba_net [s2t|t2s|s2tw|tw2s|s2twp|tw2sp|s2hk|hk2s|t2tw|tw2t|t2twp|t2t|tw2tp|t2hk|hk2t|t2jp|jp2t|auto|help|seg|tag <punct>]`n"
    exit
}

# Apply the alias: If Config is "keywords", treat it as "tag"
$Config = $Config -eq "keywords" ? "tag" : $Config

# Get input text from clipboard
$InputText = Get-Clipboard -Raw
if ($InputText.Length -eq 0) {
    Write-Host "$($Red)Clipboard is empty.$($Reset)"
    Exit
}

$OutputText = ""
$InputEncode = ""
$OutputEncode = ""

# Define conversion mappings
$ConversionMap = @{
    "s2t"   = @{ Input = "Simplified Chinese 简体"; Output = "Traditional Chinese 繁体" }
    "t2s"   = @{ Input = "Traditional Chinese 繁体"; Output = "Simplified Chinese 简体" }
    "s2tw"  = @{ Input = "Simplified Chinese 简体"; Output = "Traditional Chinese 繁体/台湾" }
    "tw2s"  = @{ Input = "Traditional Chinese 繁体/台湾"; Output = "Simplified Chinese 简体" }
    "s2hk"  = @{ Input = "Simplified Chinese 简体"; Output = "Traditional Chinese 繁体/香港" }
    "hk2s"  = @{ Input = "Traditional Chinese 繁体/香港"; Output = "Simplified Chinese 简体" }
    "s2twp" = @{ Input = "Simplified Chinese 简体"; Output = "Traditional Chinese 繁体/台湾" }
    "tw2sp" = @{ Input = "Traditional Chinese 繁体/台湾"; Output = "Simplified Chinese 简体" }
    "t2tw"  = @{ Input = "Simplified Chinese 繁体"; Output = "Traditional Chinese 繁体/台湾" }
    "tw2t"  = @{ Input = "Traditional Chinese 繁体/台湾"; Output = "Simplified Chinese 繁体" }
    "t2twp" = @{ Input = "Simplified Chinese 繁体"; Output = "Traditional Chinese 繁体/台湾" }
    "tw2tp" = @{ Input = "Traditional Chinese 繁体/台湾"; Output = "Simplified Chinese 简体" }
    "t2hk"  = @{ Input = "Simplified Chinese 繁体"; Output = "Traditional Chinese 繁体/香港" }
    "hk2t"  = @{ Input = "Traditional Chinese 繁体/香港"; Output = "Simplified Chinese 繁体" }
    "t2jp"  = @{ Input = "Japanese Kyujitai 舊字體"; Output = "Japanese Shinjitai 新字体" }
    "jp2t"  = @{ Input = "Japanese Shinjitai 新字体"; Output = "Japanese Kyujitai 舊字體" }
}

# Process configuration
switch ($Config) {
    "auto" {
        $ZhoCode = [OpenccJiebaLib.OpenccJieba]::new().ZhoCheck($InputText)
        if ($ZhoCode -eq 1) {
            $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "t2s", $Punctuation)
            $InputEncode = "Auto-Detect: Traditional Chinese 繁体"
            $OutputEncode = "Simplified Chinese 简体"
            $Config = "t2s (Auto)"
        } elseif ($ZhoCode -eq 2) {
            $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "s2t", $Punctuation)
            $InputEncode = "Auto-Detect: Simplified Chinese 简体"
            $OutputEncode = "Traditional Chinese 繁体"
            $Config = "s2t (Auto)"
        } else {
            $OutputText = $InputText
            $InputEncode = "Auto-Detect: Non-zho 其它"
            $OutputEncode = "Non-zho 其它"
            $Config = "none (auto)"
        }
    }
    "seg" {
        $OutputText = [string]::Join("/", [OpenccJiebaLib.OpenccJieba]::new().JiebaCut($InputText, $true))
        $InputEncode = "Original Text"
        $OutputEncode = "Segmented Text"
    }
    "tag" { # Now handles both "tag" and "keywords" due to the aliasing
        $OutputText = [string]::Join("/ ", [OpenccJiebaLib.OpenccJieba]::new().JiebaKeywordExtractTextRank($InputText, 30))
        $InputEncode = "Original Text"
        $OutputEncode = "Keywords"
        $SliceNumber = [math]::Min(200, $InputText.Length)
        $InputText = if ($SliceNumber -eq 200) { $InputText.Substring(0, $SliceNumber) + "..." } else { $InputText.Substring(0, $SliceNumber) }
    }
    Default {
        if ($ConversionMap.ContainsKey($Config)) {
            $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, $Config, $Punctuation)
            $InputEncode = $ConversionMap[$Config].Input
            $OutputEncode = $ConversionMap[$Config].Output
        } else {
            # Fallback for invalid configuration
            $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "s2t")
            $InputEncode = "Simplified Chinese 简体"
            $OutputEncode = "Traditional Chinese 繁体"
            $Config = "Invalid '$($Config)' - Reverted to default 's2t'"
        }
    }
}

Set-Clipboard -Value $OutputText

# Prepare output for display
$DisplayInput = $InputText
$DisplayOutput = $OutputText
$etc = [string]::Empty

if ($OutputText.Length -gt 200) {
    $DisplayInput = $InputText.Substring(0, [math]::Min(200, $InputText.Length))
    $DisplayOutput = $OutputText.Substring(0, 200)
    $etc = "..."
}

if ($DisplayOutput.Length -eq 0) {
    $DisplayOutput = "$($Red)<Empty>$($Reset)"
}

# Display results
Write-Host "Configuration: $($Blue)$($Config), $($Punctuation)"
Write-Host "$($Green)== Input ($($InputEncode)) =="
Write-Host "$($Yellow)$($DisplayInput)$($etc)`n"
Write-Host "$($Green)== Output ($($OutputEncode)) =="
Write-Host "$($Yellow)$($DisplayOutput)$($etc)"
Write-Host ("{0}(Total: {1:N0} chars set to clipboard.)`n{2}" -f $Blue, $($OutputText.Length), $Reset)