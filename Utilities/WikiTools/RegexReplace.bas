Attribute VB_Name = "Module2"
Sub MultiRegexReplacePreserveFormatting()
    Dim rng As Range
    Dim patterns As Variant, replacements As Variant
    Dim i As Long
    Dim re As Object
    
    ' Define regex patterns and replacements
    patterns = Array("([WSGHK])(x?[a-h1-8]{1,2}[1-8])")
    replacements = Array("[$1]$2")
    
    ' Loop story ranges
    For Each rng In ActiveDocument.StoryRanges
        For i = LBound(patterns) To UBound(patterns)
            Set re = CreateObject("VBScript.RegExp")
            re.Pattern = patterns(i)
            re.Global = True
            re.IgnoreCase = True
            Call ProcessRangeWithRegex(rng, re, replacements(i))
        Next i
        
        While Not (rng.NextStoryRange Is Nothing)
            Set rng = rng.NextStoryRange
            For i = LBound(patterns) To UBound(patterns)
                Set re = CreateObject("VBScript.RegExp")
                re.Pattern = patterns(i)
                re.Global = True
                re.IgnoreCase = True
                Call ProcessRangeWithRegex(rng, re, replacements(i))
            Next i
        Wend
    Next rng
End Sub


Private Sub ProcessRangeWithRegex(rng As Range, re As Object, ByVal replacement As String)
    Dim subRng As Range
    Dim matches As Object
    Dim match As Object
    
    ' Work backwards through matches so offsets don’t shift
    Set matches = re.Execute(rng.Text)
    Dim i As Long
    For i = matches.Count - 1 To 0 Step -1
        Set match = matches(i)
        
        ' Create a subrange exactly around the match
        Set subRng = rng.Duplicate
        subRng.Start = rng.Start + match.FirstIndex
        subRng.End = subRng.Start + match.Length
        
        ' Replace text inside that range (preserves formatting outside)
        subRng.Text = re.Replace(match.Value, replacement)
    Next i
End Sub
