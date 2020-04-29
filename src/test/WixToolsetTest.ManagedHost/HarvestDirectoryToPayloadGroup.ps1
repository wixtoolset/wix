param([string]$RootFolder, [string]$HarvestFolder, [string]$OutputFile)

function harvestFileToPayload {
    param([System.IO.FileInfo]$file, [string]$rootFolder, [string]$harvestFolder)

    $sourceFile = $file.FullName.Substring($rootFolder.Length + 1)
    $name = $sourceFile.Substring($harvestFolder.Length + 1)
    $payloadContents = "<Payload SourceFile='$sourceFile' Name='$name' />"
    $payloadContents
}

function harvestDirectoryToPayloadGroup {
    param([string]$rootFolder, [string]$harvestFolder, [string]$outputFile)
    
    $beginFileContents = @"
<?xml version="1.0" encoding="utf-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <PayloadGroup Id='
"@

    $endFileContents = @"
    </PayloadGroup>
  </Fragment>
</Wix>
"@

    $fileContents = $beginFileContents
    $payloadGroupId = $harvestFolder.Replace("\", ".")
    $fileContents += "$payloadGroupId'>" + [System.Environment]::NewLine
    
    $targetFolder = [System.IO.Path]::Combine($rootFolder, $harvestFolder)
    Get-ChildItem -Path $targetFolder -Recurse -File | ForEach-Object {
        $fileContents += '      ' + (harvestFileToPayload -file $_ -rootFolder $rootFolder -harvestFolder $harvestFolder) + [System.Environment]::NewLine
    }

    $fileContents += $endFileContents

    [System.IO.File]::WriteAllText($outputFile, $fileContents)
}

harvestDirectoryToPayloadGroup -rootFolder $RootFolder -harvestFolder $HarvestFolder -outputFile $OutputFile