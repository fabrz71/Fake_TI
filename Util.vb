Enum MsgType
    INFO
    WARNING
    ERR
End Enum

Module Util
    Public exitOnError As Boolean = True
    Public outputLevel As MsgType = MsgType.INFO

    Public Sub Outp(t As MsgType, ByRef source As String, ByRef msg As String)
        Dim s As String
        Select Case t
            Case MsgType.ERR
                s = "ERROR: "
            Case MsgType.WARNING
                s = "WARNING: "
            Case MsgType.INFO
                s = "INFO: "
            Case Else
                s = String.Empty
        End Select
        Debug.WriteLine(s & source & ": " & msg)
    End Sub

    Public Sub Info(ByRef source As String, ByRef msg As String)
        If outputLevel = MsgType.INFO Then Debug.WriteLine("INFO: " & source & ": " & msg)
    End Sub

    Public Sub Warn(ByRef source As String, ByRef msg As String)
        If outputLevel <= MsgType.WARNING Then Debug.WriteLine("WARNING: " & source & ": " & msg)
    End Sub

    Public Sub Err(ByRef source As String, ByRef msg As String)
        Debug.WriteLine("ERROR: " & source & ": " & msg)
        MsgBox("ERROR: " & source & ": " & msg, MsgBoxStyle.Critical)
        If exitOnError Then Application.Exit()
    End Sub

End Module
