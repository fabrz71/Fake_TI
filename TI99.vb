Public Class TI99
    Public Const chdr As String = "TI99"
    Protected rom As ROM
    Public consl As TiConsole
    Public video As TiVideo
    Public sound As TiSound
    Public renderBox As PictureBox

    Sub New(graphicBox As PictureBox)
        video = New TiVideo(graphicBox)
        sound = New TiSound()
        renderBox = graphicBox
        consl = New TiConsole(Me, graphicBox)
    End Sub

    Public Sub Init()
        ShowStartScreens()
        'Dim ch As Integer = GetChoice()
        Threading.Thread.Sleep(500)
        rom = New TiBasic(Me)
        rom.Start()
    End Sub

    Public Sub Quit()
        rom.Quit()
    End Sub

    Protected Sub ShowStartScreens()
        Dim scrBmp As Bitmap
        'scrBmp = New Bitmap(My.Resources.start_screen)
        'video.FillWithBitmap(scrBmp)
        'sound.Beep()
        'textUI.WaitKeyPress()
        'scrBmp = New Bitmap(Application.StartupPath & "\second_screen.png")
        scrBmp = New Bitmap(My.Resources.second_screen)
        video.FillWithBitmap(scrBmp)
        consl.SetCursorPosition(7, 4, False)
        consl.Print("1 FOR TI BASIC")
        sound.Beep()
        Application.DoEvents()
    End Sub

    Protected Function GetChoice() As Integer
        Dim choice As Integer
        Do
            choice = consl.WaitKeyPress()
            'If choice = 49 Then Exit Do
            'sound.ErrorBeep()
        Loop
        video.ClearScreen()
        Application.DoEvents()
        Return choice - 49
    End Function

    Public Sub keyPress(ByRef ch As Char)
        Dim hdr As String = chdr & ".KeyPress"
        Dim bch As Byte
        bch = Asc(ch)
        If bch = 0 Then
            Warn(chdr & ".KeyPress", "unhandled char #" & bch.ToString())
            Return
        End If
        If bch >= 32 Then
            Dbug(hdr, ch & " -> " & bch.ToString())
            consl.KeyPress(bch)
        End If
    End Sub

    Public Sub KeyDown(ByRef key As Keys)
        Dim hdr As String = chdr & ".KeyDown"
        Dim bch As Byte = TI99.KeyCodeToASCII(key)
        If bch = 0 Then
            Warn(hdr, "can't handle keyCode " & key.ToString())
            Return
        End If

        If bch < 32 Or bch = 127 Then
            Dbug(hdr, key.ToString() & " -> " & bch.ToString())
            consl.KeyPress(bch)
        End If
    End Sub

    Public Sub KeyRelease(ByRef key As Keys)
        'Dbug(chdr & ".KeyRelease", "call")
        consl.KeyRelease()
    End Sub

    'converts Keys argument from UI to ASCII code for TI99
    Protected Shared Function KeyCodeToASCII(ByRef key As Keys) As Byte
        Dim ch As Byte
        Select Case key
            Case Keys.Cancel
                ch = 3
            Case Keys.Insert
                ch = 4
            Case Keys.Left
                ch = 8
            Case Keys.Right
                ch = 9
            Case Keys.Down
                ch = 10
            Case Keys.Up
                ch = 11
            Case Keys.Enter
                ch = 13
            Case Keys.Back
                ch = 15
            Case Keys.Delete
                ch = 127
            Case Else
                Try
                    ch = Convert.ToByte(key)
                Catch ex As Exception
                    Warn(chdr & ".KeyCodeToTi99Key", "unhandled key " & key.ToString())
                    ch = 0
                End Try
        End Select
        Return ch
    End Function

End Class
