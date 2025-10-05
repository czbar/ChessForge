Attribute VB_Name = "InsertTitlePage"
Sub InsertTitlePage()
Attribute InsertTitlePage.VB_ProcData.VB_Invoke_Func = "Normal.InsertTitlePage.InsertTitlePage"
'
' InsertTitlePage Macro
'
'
    Selection.HomeKey Unit:=wdStory
    Selection.TypeParagraph
    Selection.InlineShapes.AddPicture FileName:= _
        "C:\GitHub\ChessForge.wiki\images\ChessForgeTitle.png" _
        , LinkToFile:=False, SaveWithDocument:=True
    'Selection.ParagraphFormat.Alignment = wdAlignParagraphCenter
    Selection.TypeParagraph
    Selection.TypeParagraph
    Selection.TypeParagraph
    'Selection.InsertBreak Type:=wdPageBreak
End Sub
