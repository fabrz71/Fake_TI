Public Class TiBasicConsole
    Inherits TiConsole

    Public Sub New(videoGraphics As TiVideo, container As PictureBox)
        MyBase.New(videoGraphics, container)
        SetTextMargins(2, 29)
    End Sub

    Public Overrides Sub Init()
        inputMode = False
        presetInputText = String.Empty
        SetTextMargins(2, TiVideo.COLS - 3)
        Clear()
        SetCursorPosition(TiVideo.ROWS - 1, lMargin)
    End Sub

    Public Overrides Sub StartNewTextLineInput()
        NewLine()
        If lMargin > 0 Then video.PutChar(curR, lMargin - 1, ">"c)
        'curC = lMargin
        StartTextInput()
        If Not String.IsNullOrEmpty(presetInputText) Then Print(presetInputText)
        If presetInputCursorColumn > 0 Then SetCursorPosition(curR, presetInputCursorColumn)
    End Sub

End Class
