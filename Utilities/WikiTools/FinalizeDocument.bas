Attribute VB_Name = "Module4"
Sub FinalizeDocument()
    Call RemoveLocalHyperlinks
    Call InsertTitlePage
    Call InsertTableOfContentsAtBeginning
End Sub
