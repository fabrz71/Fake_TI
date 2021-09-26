' Text Console basic I/O functions
Public Class TiConsole
    Implements IDisposable

    Protected Const CURSOR_INTRV As Integer = 255
    Protected Const CURSOR_CHAR As Byte = 30
    Protected WithEvents cursorTimer As Timer
    Protected video As TiVideo
    Protected picBox As PictureBox
    Protected curR, curC As Integer
    Protected lMargin, rMargin As Integer
    Protected cursorVisible As Boolean
    Protected inputMode As Boolean
    Protected inputStartPosition As Point
    Protected presetInputText As String
    Protected presetInputCursorColumn As Integer
    Protected keyPressed As Byte
    Protected charUnderCursor As Byte
    Private disposedValue As Boolean

    Public Event TextInputCompleted(ByRef sender As Object, ByRef inputString As String)

    Sub New(videoGraphics As TiVideo, container As PictureBox)
        video = videoGraphics
        picBox = container
        cursorTimer = New Timer()
        cursorTimer.Interval = CURSOR_INTRV
        cursorTimer.Enabled = True
        cursorVisible = False
        Init()
    End Sub

    Public Overridable Sub Init()
        presetInputText = String.Empty
        presetInputCursorColumn = 0
        inputMode = False
        keyPressed = 0
        'keyWaitState = False
        presetInputText = String.Empty
        SetTextMargins(0, TiVideo.COLS - 1)
        'Clear()
        video.Init()
        SetCursorPosition(lMargin, TiVideo.ROWS - 1)
    End Sub


    Protected Sub CursorTimer_Tick() Handles cursorTimer.Tick
        If inputMode Then BlinkCursor()
    End Sub

    Public Sub BlinkCursor()
        'cursorVisible = Not cursorVisible
        'video.ShowCursor(curR, curC, cursorVisible)
        If cursorVisible Then HideCursor() Else ShowCursor()
    End Sub

    Public Sub ShowCursor()
        If cursorVisible Then Return
        charUnderCursor = video.GetChar(curR, curC)
        video.PutChar(curR, curC, CURSOR_CHAR)
        cursorVisible = True
        video.Invalidate()
    End Sub

    Protected Sub HideCursor()
        'video.ShowCursor(curR, curC, False)
        video.PutChar(curR, curC, charUnderCursor)
        cursorVisible = False
        video.Invalidate()
    End Sub

    Public Sub Clear()
        video.ClearScreen()
    End Sub

    Public Sub Print(ByRef s As String)
        If String.IsNullOrEmpty(s) Then Return
        HideCursor()
        Dim l As Integer = s.Length
        Dim i As Integer = 0
        Dim c As Char
        Dim ch As Byte
        Do
            c = s.Chars(i)
            Try
                ch = Convert.ToByte(c)
            Catch ex As Exception
                ch = Convert.ToByte("?"c)
            End Try
            If isStandardChar(ch) Then
                video.PutChar(curR, curC, ch)
                MoveForward()
            ElseIf ch < 32 Then
                Select Case ch
                    Case 8 ' left
                        MoveBack(curR, curC)
                    Case 9 ' right
                        If MoveForward(curR, curC) Then ScrollUp()
                    'Case 10 ' down
                    '    MoveDown(curR)
                    'Case 11 ' up
                    '    MoveUp(curR)
                    Case 13 ' return
                        NewLine()
                    Case 15 ' back
                        MoveBack(curR, curC)
                    Case Else
                        ch = 32
                End Select
            Else
                ch = 32
            End If
            i += 1
        Loop Until i = l
        picBox.Invalidate()
        picBox.Update()
    End Sub

    Public Sub PrintLn(ByRef s As String)
        Print(s)
        NewLine()
    End Sub

    ' return true when on bottom of screen and scroll-up is needed
    Public Function MoveForward(ByRef r As Integer, ByRef c As Integer) As Boolean
        c += 1
        If c > rMargin Then Return NewLine(r, c)
        Return False
    End Function

    Public Sub MoveForward()
        If MoveForward(curR, curC) Then ScrollUp()
    End Sub

    Public Sub MoveBack(ByRef r As Integer, ByRef c As Integer)
        c -= 1
        If c < lMargin Then
            c = 0
            MoveUp(r)
        End If
    End Sub

    Public Sub MoveUp(ByRef r As Integer)
        r -= 1
        If r < 0 Then r = 0
    End Sub

    Public Sub MoveDown(ByRef r As Integer)
        r += 1
        If r >= TiVideo.ROWS Then r = TiVideo.ROWS - 1
    End Sub

    ' return true when on bottom of screen and scroll-up is needed
    Public Function Tab(tabPos As Integer, ByRef r As Integer, ByRef c As Integer) As Boolean
        tabPos = tabPos Mod (rMargin - lMargin + 1)
        If c > tabPos Then r += 1
        c = tabPos
        If r >= TiVideo.ROWS Then
            r = TiVideo.ROWS - 1
            Return True
        End If
        Return False
    End Function

    ' return true when on bottom of screen and scroll-up is needed
    Public Function Tab(ByRef tabPos As Integer)
        Return Tab(tabPos, curR, curC)
    End Function

    ' return true when on bottom of screen and scroll-up is needed
    Public Function HalfRowTab(ByRef r As Integer, ByRef c As Integer) As Boolean
        If c < (rMargin - lMargin) / 2 Then
            c = (rMargin - lMargin / 2) + 1
        Else
            'c = lMargin
            Return NewLine(r, c)
        End If
        Return False
    End Function

    Public Function HalfRowTab()
        Return HalfRowTab(curR, curC)
    End Function

    Public Function SetTextMargins(left As Integer, right As Integer) As Boolean
        If left < 0 Or left > TiVideo.COLS - 1 Then Return False
        If right < 0 Or right > TiVideo.COLS - 1 Then Return False
        If left >= right Then Return False
        lMargin = left
        rMargin = right
        ' hide cursor and correct its position
        HideCursor()
        If curC < lMargin Then curC = lMargin Else If curC > rMargin Then curC = rMargin
        Return True
    End Function

    Public Function InputInProgress() As Boolean
        Return inputMode
    End Function

    Public Sub PresetInputTextLine(ByRef defaultStr As String)
        presetInputText = defaultStr
    End Sub

    Public Sub PresetInputCursorPosition(ByRef col As Integer)
        If col >= 0 And col < rMargin - lMargin Then presetInputCursorColumn = lMargin + col
    End Sub

    Public Overridable Sub StartNewTextLineInput()
        NewLine()
        StartTextInput()
        If Not String.IsNullOrEmpty(presetInputText) Then Print(presetInputText)
        If presetInputCursorColumn > 0 Then SetCursorPosition(curR, presetInputCursorColumn)
    End Sub

    Public Sub StartTextInput(Optional ByRef promptString As String = "")
        If Not String.IsNullOrEmpty(promptString) Then Print(promptString)
        cursorTimer.Start()
        inputStartPosition = New Point(curC, curR)
        inputMode = True
    End Sub

    ' legge il testo dalla posizione iniziale di input fino alla fine della riga
    ' dove è posizionato il cursore
    Protected Function FinishInput() As String
        inputMode = False
        cursorTimer.Stop()
        HideCursor()
        Dim retStr As String
        Dim ln As Integer = TiVideo.COLS - inputStartPosition.X + (curR - inputStartPosition.Y) * TiVideo.COLS
        retStr = GetVideoString(inputStartPosition.Y, inputStartPosition.X, ln)
        'If Not String.IsNullOrEmpty(retStr) Then NewLine()
        RaiseEvent TextInputCompleted(Me, retStr)
        presetInputText = String.Empty
        presetInputCursorColumn = 0
        Return retStr
    End Function

    ' return true when on bottom of screen and scroll-up is needed
    Public Function NewLine(ByRef r As Integer, ByRef c As Integer) As Boolean
        c = lMargin
        r += 1
        If r >= TiVideo.ROWS Then
            r = TiVideo.ROWS - 1
            Return True
        End If
        Return False
    End Function

    Public Sub NewLine()
        If NewLine(curR, curC) Then ScrollUp()
    End Sub

    Public Sub Home()
        curC = lMargin
    End Sub

    Public Sub SetCursorPosition(row As Integer, col As Integer, Optional respectMargins As Boolean = True)
        If row < 0 Or row >= TiVideo.ROWS Then Return
        If col < 0 Or col > TiVideo.COLS Then Return
        If respectMargins Then
            If col < lMargin Then col = lMargin Else If col > rMargin Then col = rMargin
        End If
        HideCursor()
        curR = row
        curC = col
    End Sub

    Public Function GetVideoString(fromRow As Integer, fromCol As Integer, length As Integer) As String
        Dim retStr As String = String.Empty
        If TiVideo.OutOfScreenBounds(fromRow, fromCol) Then Return retStr
        Dim c As Integer = fromCol
        Dim r As Integer = fromRow
        Dim i As Integer = 0
        Do
            retStr &= Chr(video.GetChar(r, c))
            If MoveForward(r, c) Then Exit Do
        Loop While i < length
        Return retStr.Trim()
    End Function

    Public Function isStandardChar(ByRef c As Byte) As Boolean
        Return (c >= 32 And c < 128)
    End Function

    Public Function isFunctionChar(ByRef c As Byte) As Boolean
        Return (c >= 1 And c < 16)
    End Function

    Public Sub ScrollUp()
        video.ScrollUp()
        If inputMode Then inputStartPosition.Y -= 1
    End Sub

    Public Sub KeyPress(key As Byte)
        keyPressed = key
        If inputMode Then
            If key >= 97 And key <= 122 Then
                key -= 32
            ElseIf key = 13 Then
                FinishInput()
                Return
            End If
            Try
                Print(Chr(key))
            Catch ex As Exception

            End Try
        End If
    End Sub

    Public Sub KeyRelease(key As Byte)
        keyPressed = 0
    End Sub

    Public Function WaitKeyPress() As Byte
        Do While keyPressed <> 0
            Threading.Thread.Sleep(50)
            Application.DoEvents()
        Loop
        Do While keyPressed = 0
            Threading.Thread.Sleep(50)
            Application.DoEvents()
        Loop
        Return keyPressed
    End Function

    Public Function GetKeyPressed() As Byte
        Return keyPressed
    End Function

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: eliminare lo stato gestito (oggetti gestiti)
            End If

            ' TODO: liberare risorse non gestite (oggetti non gestiti) ed eseguire l'override del finalizzatore
            ' TODO: impostare campi di grandi dimensioni su Null
            cursorTimer.Dispose()
            disposedValue = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Non modificare questo codice. Inserire il codice di pulizia nel metodo 'Dispose(disposing As Boolean)'
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub

End Class
