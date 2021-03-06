$packageName = 'markdownmonster'
$fileType = 'exe'
$url = 'https://github.com/RickStrahl/MarkdownMonsterReleases/raw/master/v1.5/MarkdownMonsterSetup-1.5.8.exe'

$silentArgs = '/VERYSILENT'
$validExitCodes = @(0)

Install-ChocolateyPackage "packageName" "$fileType" "$silentArgs" "$url"  -validExitCodes  $validExitCodes  -checksum "E200599929694AD9FBA2D0BB92CEFB99757A0CC84ADA1176C1F7D3580E253FEC" -checksumType "sha256"
