Public Class CodeLine
    Public number As Integer
    Public content As String
    Public nextLine As CodeLine
    Public branchLine As CodeLine
    Public breakpoint As Boolean

    Sub New(ByRef n As Integer, ByRef code As String)
        number = n
        content = code
        'Me.nextIdx = 0
        nextLine = Nothing
        breakpoint = False
    End Sub

    Sub New(ByRef n As Integer, ByRef code As String, ByRef nextLn As CodeLine)
        number = n
        content = code
        nextLine = nextLn
        breakpoint = False
    End Sub

    Function GetComplete() As String
        Return number.ToString & " " & content
    End Function

End Class
