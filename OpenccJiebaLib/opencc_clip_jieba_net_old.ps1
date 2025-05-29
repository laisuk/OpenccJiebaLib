[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $Config = "auto",
    [string]
    $Punct = ""
)
Add-Type -Path "${PSScriptRoot}\bin\Release\netstandard2.0\OpenccJiebaLib.dll"

$Red = "`e[1;31m"
$Green = "`e[1;32m"
$Yellow = "`e[1;33m"
$Blue = "`e[1;34m"
$Reset = "`e[0m"
$ConfigList = @("s2t", "t2s", "s2tw", "tw2s", "s2twp", "tw2sp", "s2hk", "hk2s", "t2tw", "t2hk", "tw2t", "hk2t", "t2jp", "jp2t")

if (($Punct.ToLower() -eq "punct"))
{
    $Punctuation = $True
}
else
{
    $Punctuation = $False
}


Write-Host "Opencc-Clip-Jieba-Net version 1.0.0 Copyright (c) 2024 Bryan Lai"
if ($Config -eq "help")
{
    Write-Host "Usage: opencc_clip_jieba_net [s2t|t2s|s2tw|tw2s|s2twp|tw2sp|s2hk|hk2s|t2tw|tw2t|t2twp|tw2t|tw2tp|t2hk|hk2t|t2jp|jp2t|auto|help|seg|tag <punct>]`n"
    exit
}

if (-not $Config -in $ConfigList)
{
    $Config = "auto"
}

$InputText = Get-Clipboard -Raw
if ($InputText.Length -eq 0)
{
    Write-Host "$( $Red )Clipboard is empty.$( $Reset )"
    Exit
}

if ($Config -eq "s2t")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "s2t", $Punctuation)
    $InputEncode = "Simplified Chinese 简体"
    $OutputEncode = "Traditional Chinese 繁体"
}
elseif ($Config -eq "t2s")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "t2s", $Punctuation)
    $InputEncode = "Traditional Chinese 繁体"
    $OutputEncode = "Simplified Chinese 简体"
}
elseif ($Config -eq "s2tw")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "s2tw", $Punctuation)
    $InputEncode = "Simplified Chinese 简体"
    $OutputEncode = "Traditional Chinese 繁体/台湾"
}
elseif ($Config -eq "tw2s")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "tw2s", $Punctuation)
    $InputEncode = "Traditional Chinese 繁体/台湾"
    $OutputEncode = "Simplified Chinese 简体"
}
elseif ($Config -eq "s2hk")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "s2hk", $Punctuation)
    $InputEncode = "Simplified Chinese 简体"
    $OutputEncode = "Traditional Chinese 繁体/香港"
}
elseif ($Config -eq "hk2s")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "hk2s", $Punctuation)
    $InputEncode = "Traditional Chinese 繁体/香港"
    $OutputEncode = "Simplified Chinese 简体"
}
elseif ($Config -eq "s2twp")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "s2twp", $Punctuation)
    $InputEncode = "Simplified Chinese 简体"
    $OutputEncode = "Traditional Chinese 繁体/台湾"
}
elseif ($Config -eq "tw2sp")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "tw2sp", $Punctuation)
    $InputEncode = "Traditional Chinese 繁体/台湾"
    $OutputEncode = "Simplified Chinese 简体"
}
elseif ($Config -eq "t2tw")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "t2tw", $Punctuation)
    $InputEncode = "Simplified Chinese 繁体"
    $OutputEncode = "Traditional Chinese 繁体/台湾"
}
elseif ($Config -eq "tw2t")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "tw2t", $Punctuation)
    $InputEncode = "Traditional Chinese 繁体/台湾"
    $OutputEncode = "Simplified Chinese 繁体"
}
elseif ($Config -eq "t2twp")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "t2twp", $Punctuation)
    $InputEncode = "Simplified Chinese 繁体"
    $OutputEncode = "Traditional Chinese 繁体/台湾"
}
elseif ($Config -eq "tw2tp")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "tw2tp", $Punctuation)
    $InputEncode = "Traditional Chinese 繁体/台湾"
    $OutputEncode = "Simplified Chinese 简体"
}
elseif ($Config -eq "t2hk")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "t2hk", $Punctuation)
    $InputEncode = "Simplified Chinese 繁体"
    $OutputEncode = "Traditional Chinese 繁体/香港"
}
elseif ($Config -eq "hk2t")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "hk2t", $Punctuation)
    $InputEncode = "Traditional Chinese 繁体/香港"
    $OutputEncode = "Simplified Chinese 繁体"
}
elseif ($Config -eq "t2jp")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "t2jp", $Punctuation)
    $InputEncode = "Japanese Kyujitai 舊字體"
    $OutputEncode = "Japanese Shinjitai 新字体"
}
elseif ($Config -eq "jp2t")
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "jp2t", $Punctuation)
    $InputEncode = "Japanese Shinjitai 新字体"
    $OutputEncode = "Japanese Kyujitai 舊字體"
}
elseif ($Config -eq "auto")
{
    $ZhoCode = [OpenccJiebaLib.OpenccJieba]::new().ZhoCheck($InputText)

    if ($ZhoCode -eq 1)
    {
        $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "t2s", $Punctuation)
        $InputEncode = "Auto-Detect: Traditional Chinese 繁体"
        $OutputEncode = "Simplified Chinese 简体"
        $Config = "t2s (Auto)"
    }
    elseif ($ZhoCode -eq 2)
    {
        $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "s2t", $Punctuation)
        $InputEncode = "Auto-Detect: Simplified Chinese 简体"
        $OutputEncode = "Traditional Chinese 繁体"
        $Config = "s2t (Auto)"
    }
    else
    {
        $OutputText = $InputText
        $InputEncode = "Auto-Detect: Non-zho 其它"
        $OutputEncode = "Non-zho 其它"
        $Config = "none (auto)"
    }
}
elseif ($Config -eq "seg")
{
    $OutputText = [string]::Join("/",[OpenccJiebaLib.OpenccJieba]::new().JiebaCut($InputText, $true))
    $InputEncode = "Original Text"
    $OutputEncode = "Segmented Text"
}
elseif ($Config -eq "tag" -or $Config -eq "keywords")
{
    $OutputText = [string]::Join("/ ",[OpenccJiebaLib.OpenccJieba]::new().JiebaKeywordExtractTextRank($InputText, 30))
    $InputEncode = "Original Text"
    $OutputEncode = "Keywords"
    $SliceNumber = [math]::Min(200, $InputText.Length)
    $InputText = $SliceNumber -eq 200 ? $InputText.Substring(0, $SliceNumber) + "..." : $InputText.Substring(0, $SliceNumber)
}
else
{
    $OutputText = [OpenccJiebaLib.OpenccJieba]::new().Convert($InputText, "s2t")
    $InputEncode = "Simplified Chinese 简体"
    $OutputEncode = "Traditional Chinese 繁体"
    $Config = "Invalid '$( $Config )' - Reverted to default 's2t'"
}

Set-Clipboard -Value $OutputText
if ($OutputText.Length -gt 200)
{
    $DisplayInput = $InputText.Substring(0,[math]::Min(200, $InputText.Length))
    $DisplayOutput = $OutputText.Substring(0, 200)
    $etc = "..."
}
else
{
    $DisplayInput = $InputText
    $DisplayOutput = $OutputText.Length -eq 0 ? "$( $Red )<Empty>$( $Reset )" : $OutputText
    $etc = [string]::Empty
}

Write-Host "Configuration: $( $Blue )$( $Config ), $( $Punctuation )"
Write-Host "$( $Green )== Input ($( $InputEncode )) =="
Write-Host "$( $Yellow )$( $DisplayInput )$( $etc )`n"
Write-Host "$( $Green )== Output ($( $OutputEncode )) =="
Write-Host "$( $Yellow )$( $DisplayOutput )$( $etc )"
Write-Host ("{0}(Total: {1:N0} chars set to clipboard.)`n{2}" -f $Blue, $( $OutputText.Length ), $Reset)

