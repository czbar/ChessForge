#
# Reads Master.txt, generates Template.empty.txt and Resources.resx from it.
#
# Reads the list of cultures and for each specified culture (ci), looks for Localized.[ci].txt. 
# If it exists generates Template.[ci].txt with any translation found in Localized.[ci].txt included. 
#
# Run in Developer PowerShell for VS
#

# Generate default Resources.resx
resgen Master.txt Resources.resx


# Generate default (empty) Template
./MasterTranslator -g

# for each culture string in CultureList.txt,
# merge with existing translations and generate Template.<ci>.txt
$file_data = Get-Content CultureList.txt

ForEach ($ci in $file_data)
{
	./MasterTranslator -g $ci
	$template_file = "Template." + $ci + ".txt"
	$resources_file = "Resources." + $ci + ".resx"
    resgen $template_file $resources_file
}


