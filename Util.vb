Enum MsgType
    DEBUG
    INFO
    WARNING
    ERR
End Enum

Module Util
    Public Const mhdr As String = "Util"
    Public exitOnError As Boolean = True
    Public outputLevel As MsgType = MsgType.DEBUG

    Public Sub Outp(t As MsgType, ByRef source As String, ByRef msg As String)
        Dim s As String
        Select Case t
            Case MsgType.ERR
                s = "ERROR: "
            Case MsgType.WARNING
                s = "WARNING: "
            Case MsgType.INFO
                s = "INFO: "
            Case MsgType.DEBUG
                s = ">"
            Case Else
                s = String.Empty
        End Select
        Debug.WriteLine(s & source & ": " & msg)
    End Sub

    Public Sub Dbug(ByRef source As String, ByRef msg As String)
        If outputLevel = MsgType.DEBUG Then Debug.WriteLine("INFO: " & source & ": " & msg)
    End Sub

    Public Sub Info(ByRef source As String, ByRef msg As String)
        If outputLevel <= MsgType.INFO Then Debug.WriteLine("INFO: " & source & ": " & msg)
    End Sub

    Public Sub Warn(ByRef source As String, ByRef msg As String)
        If outputLevel <= MsgType.WARNING Then Debug.WriteLine("WARNING: " & source & ": " & msg)
    End Sub

    Public Sub Err(ByRef source As String, ByRef msg As String)
        Debug.WriteLine("ERROR: " & source & ": " & msg)
        MsgBox("ERROR: " & source & ": " & msg, MsgBoxStyle.Critical)
        If exitOnError Then Application.Exit()
    End Sub

    Public Function GetHighByte(ByRef word As UInt16) As Byte
        Return Convert.ToByte((word >> 8) And &HFF)
    End Function

    Public Function GetLowByte(ByRef word As UInt16) As Byte
        Return Convert.ToByte(word And &HFF)
    End Function

    Public Function GetWordFromBytes(ByRef bytes As Byte(), ByRef idx As Integer) As UInt16
        If idx > bytes.Length - 2 Then
            Warn(mhdr & ".GetWordFromBytes", "index too big")
            Return 0
        End If
        Return Convert.ToUInt16(bytes(idx + 1)) + (Convert.ToUInt16(bytes(idx)) << 8)
    End Function

    Public Sub WriteWordInBytes(ByRef bytes As Byte(), ByRef idx As Integer, ByRef w As UInt16)
        If idx < bytes.Length - 2 Then
            Warn(mhdr & ".WriteWordInBytes", "index too big")
            Return
        End If
        bytes(idx) = Convert.ToByte((w >> 8) & &HFF)
        bytes(idx + 1) = Convert.ToByte(w & &HFF)
    End Sub
End Module
