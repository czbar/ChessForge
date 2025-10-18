Attribute VB_Name = "FinalizeDocument"
Sub FinalizeDocument()
    Call RemoveLocalHyperlinks.RemoveLocalHyperlinks
    Call InsertTableOfContents.InsertTableOfContentsAtBeginning
    Call InsertTitlePage.InsertTitlePage
    Call InsertPageNumbers
    Call InsertPageNumbers
End Sub

Private Sub InsertPageNumbers()
    ' Adds page numbers to the footer of each page
    With ActiveDocument.Sections(1).Footers(wdHeaderFooterPrimary)
        .PageNumbers.Add _
            PageNumberAlignment:=wdAlignParagraphCenter, _
            FirstPage:=True
    End With
End Sub


