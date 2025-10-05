Attribute VB_Name = "FinalizeDocument"
Sub FinalizeDocument()
    Call RemoveLocalHyperlinks.RemoveLocalHyperlinks
    Call InsertTableOfContents.InsertTableOfContentsAtBeginning
    Call InsertTitlePage.InsertTitlePage
End Sub
