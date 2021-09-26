Public Class TI99
    Protected rom As SSS
    Public textUI As TiConsole
    Public video As TiVideo
    Public sound As TiSound
    Public renderBox As PictureBox

    Sub New(graphicBox As PictureBox)
        video = New TiVideo(graphicBox)
        sound = New TiSound(graphicBox)
        renderBox = graphicBox
        textUI = New TiConsole(video, graphicBox)
    End Sub

    Public Sub Init()
        ShowStartScreens()
        'Dim ch As Integer = GetChoice()
        Threading.Thread.Sleep(500)
        rom = New TiBasic(Me)
        rom.Init()
    End Sub

    Public Sub Quit()
        rom.EndActivity()
    End Sub

    Protected Sub ShowStartScreens()
        Dim scrBmp As Bitmap
        'scrBmp = New Bitmap(Application.StartupPath & "\start_screen.png")
        'video.FillWithBitmap(scrBmp)
        'sound.Beep()
        'textUI.WaitKeyPress()
        scrBmp = New Bitmap(Application.StartupPath & "\second_screen.png")
        video.FillWithBitmap(scrBmp)
        textUI.SetCursorPosition(7, 4, False)
        textUI.Print("1 FOR TI BASIC")
        sound.Beep()
        Application.DoEvents()
    End Sub

    Protected Function GetChoice() As Integer
        Dim choice As Integer
        Do
            choice = textUI.WaitKeyPress()
            'If choice = 49 Then Exit Do
            'sound.ErrorBeep()
        Loop
        video.ClearScreen()
        Application.DoEvents()
        Return choice - 49
    End Function

    Public Shared Function KeyCodeToTi99Key(ByRef kc As Keys) As Byte
        Dim ch As Byte = Convert.ToByte(kc And 255)
        Select Case kc
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
        End Select
        Return ch
    End Function

End Class
