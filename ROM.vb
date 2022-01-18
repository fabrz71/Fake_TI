Public MustInherit Class ROM
    Protected machine As TI99

    Sub New(ByRef m As TI99)
        machine = m
    End Sub

    Public MustOverride Sub Start()
    Public MustOverride Sub Quit()
    Public MustOverride Function GetRomName() As String
    'Public MustOverride Function KeyPressed(k As Keys)
    'Public MustOverride Function KeyReleased(k As Keys)

End Class
