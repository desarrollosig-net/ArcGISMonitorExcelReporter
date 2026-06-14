$file = '.github/workflows/release.yml'
$content = Get-Content $file -Raw

# Add marker file creation after LastDatePrefix line
$old_section = '          Set-Content -Path "$projectDir\LastDatePrefix.txt" -Value $date     -NoNewline

		  Write-Host "Version: $version"'

$new_section = '          Set-Content -Path "$projectDir\LastDatePrefix.txt" -Value $date     -NoNewline

		  # Create marker file to prevent MSBuild from incrementing build number
		  # This ensures that multiple platform builds (win-x64, linux-x64) all use the same build number
		  Set-Content -Path "$projectDir\BuildNumberFromCI.txt" -Value "1" -NoNewline

		  Write-Host "Version: $version"'

$content = $content.Replace($old_section, $new_section)

# Add marker restoration before linux-x64 build
$old_linux = '          --output publish/win-x64

	  - name: Publish linux-x64'

$new_linux = '          --output publish/win-x64

	  - name: Restore CI marker for linux-x64 build
		shell: pwsh
		run: |
		  $projectDir = "ArcGISMonitorExcelReporter"
		  Set-Content -Path "$projectDir\BuildNumberFromCI.txt" -Value "1" -NoNewline

	  - name: Publish linux-x64'

$content = $content.Replace($old_linux, $new_linux)

Set-Content $file -Value $content
Write-Host "Updated release.yml with CI marker logic"
