Attribute VB_Name = "Module3"

Sub InsertTableOfContentsAtBeginning()
    Dim doc As Document
    Set doc = ActiveDocument

    ' Move cursor to the start of the document
    'Selection.HomeKey Unit:=wdStory

    ' Insert a page break to separate TOC from the rest (optional)
    'Selection.InsertBreak Type:=wdPageBreak

    ' Insert Table of Contents
    doc.TablesOfContents.Add Range:=Selection.Range, _
        UseHeadingStyles:=True, _
        UpperHeadingLevel:=1, _
        LowerHeadingLevel:=3, _
        UseFields:=True, _
        RightAlignPageNumbers:=True, _
        IncludePageNumbers:=True, _
        AddedStyles:="", _
        UseHyperlinks:=True, _
        HidePageNumbersInWeb:=False

    ' Update the TOC to show entries
    doc.TablesOfContents(1).Update
End Sub

