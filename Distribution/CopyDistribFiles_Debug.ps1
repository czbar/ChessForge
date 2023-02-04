# Remove previous release files (supress errors)
Remove-Item -Recurse -Force .\Temp 2>$null
Remove-Item -Recurse -Force .\ReleaseDebug 2>$null

# Create required directory structure
New-Item -Type dir .\Temp\Resources\Sounds
New-Item -Type dir .\Temp\pl
New-Item -Type dir .\ReleaseDebug

# Copy all release items from the list fo files
foreach($line in Get-Content ".\List of Files.txt") 
{
	if (![string]::IsNullOrEmpty($line))
	{
		write $line
		Copy-Item ..\ChessForge\bin\Debug\$line -Destination .\Temp\$line
	}
}

# Copy all distribution Workbooks
Copy-Item ..\Workbooks\Distribution\* -Destination .\Temp

# Copy localized resources
Copy-Item ..\ChessForge\bin\Debug\pl\* -Destination .\Temp\pl

# Copy readme's
Copy-Item ..\LICENSE.MD -Destination .\Temp
Copy-Item ..\README.md -Destination .\Temp

Set-Location .\Temp 
# Create the FULL zip file
Compress-Archive * -DestinationPath ..\ReleaseDebug\ChessForgeFullDebug.zip

Set-Location .. 

# Delete files meant for full release only
foreach($line in Get-Content ".\Files in Full Zip Only.txt") 
{
	if (![string]::IsNullOrEmpty($line))
	{
		write "removing: " + $line
		Remove-Item .\Temp\$line
	}
}

Set-Location .\Temp 

# Create the zip file without Srockfish
Compress-Archive * -DestinationPath ..\ReleaseDebug\ChessForgeDebug.zip

Set-Location .. 

# Clear Temp directory
Remove-Item -Recurse -Force .\Temp
