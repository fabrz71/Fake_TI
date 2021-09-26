Public MustInherit Class SSS
    Protected machine As TI99

    Sub New(ByRef m As TI99)
        machine = m
    End Sub

    Public MustOverride Sub Init()
    Public MustOverride Sub EndActivity()
    Public MustOverride Function GetRomName() As String
End Class
