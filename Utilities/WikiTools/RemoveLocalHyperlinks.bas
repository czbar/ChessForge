Attribute VB_Name = "Module1"
Sub RemoveLocalHyperlinks()
    Dim h As Hyperlink
    Dim i As Long
    
    ' Loop backwards to avoid skipping items when removing
    For i = ActiveDocument.Hyperlinks.Count To 1 Step -1
        Set h = ActiveDocument.Hyperlinks(i)
        
        ' Check if hyperlink does NOT start with http/https
        If Not (LCase(h.Address) Like "http*") Then
            h.Delete
        End If
    Next i
End Sub


