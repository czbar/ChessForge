Attribute VB_Name = "InsertTitlePage"
Sub InsertTitlePage()
Attribute InsertTitlePage.VB_ProcData.VB_Invoke_Func = "Normal.InsertTitlePage.InsertTitlePage"
'
' InsertTitlePage Macro
'
'
    Dim doc As Document
    Set doc = ActiveDocument
    
    ' Move cursor to the start of the document
    Selection.HomeKey Unit:=wdStory
    Selection.TypeParagraph
    Selection.InlineShapes.AddPicture FileName:= _
        "C:\GitHub\ChessForge.wiki\images\ChessForgeTitle.png" _
        , LinkToFile:=False, SaveWithDocument:=True
    Selection.TypeParagraph
    Selection.TypeParagraph
    
    Selection.TypeParagraph
    Selection.Font.Size = 36
    Selection.Font.Color = -704593921
    Selection.ParagraphFormat.Alignment = wdAlignParagraphCenter
    Selection.TypeText Text:="User's Manual"
    Selection.TypeParagraph
    
    Selection.InsertBreak Type:=wdPageBreak
    
    Selection.Font.Size = 20
    Selection.Font.Color = -738148353
    Selection.ParagraphFormat.Alignment = wdAlignParagraphLeft
    Selection.TypeText Text:="Table of Content"
    Selection.TypeParagraph

    'Update the TOC after inserting title
    doc.TablesOfContents(1).Update

End Sub
