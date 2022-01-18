' Text Console basic I/O functions
Public Class TiConsole
    Implements IDisposable

    Public Const chdr As String = "TiConsole"
    Protected Const CURSOR_INTRV As Integer = 255
    Protected Const CURSOR_CHAR As Byte = 30
    Protected Const MAX_INPUT_LWRAPS As Byte = 3
    Protected Const SHOW_END_OF_INPUT_LINE As Boolean = True
    Protected WithEvents CursorTimer As Timer
    Protected ti As TI99
    Protected video As TiVideo
    Protected picBox As PictureBox
    Protected curR, curC As Integer
    Protected lMargin, rMargin As Integer ' first and last character column on every input row
    Protected inputTextRowWidth As Byte
    Protected cursorVisible As Boolean
    Protected insertMode As Boolean
    Protected inputMode As Boolean
    Protected inputStartPosition As Point
    Protected inputCursorPosition As Integer ' relative input line cursor position
    Protected inputTextLength As Integer
    Protected inputTextMaxLength As Integer
    Protected inputLineWraps As Byte
    Protected endOfLineMarkPos As Point
    Protected presetInputText As String
    Protected presetInputCursorColumn As Integer
    Protected lastKeyPressed As Byte
    Protected charUnderCursor As Byte
    Private disposedValue As Boolean
    Public showEndOfInputLine As Boolean

    Public Event TextInputCompleted(ByRef sender As Object, ByRef inputString As String)

    Sub New(machine As TI99, container As PictureBox)
        ti = machine
        video = ti.video
        picBox = container
        CursorTimer = New Timer With {
            .Interval = CURSOR_INTRV,
            .Enabled = True
        }
        cursorVisible = False
        Init()
    End Sub

    Public Overridable Sub Init()
        showEndOfInputLine = SHOW_END_OF_INPUT_LINE
        presetInputText = String.Empty
        presetInputCursorColumn = 0
        insertMode = False
        inputMode = False
        lastKeyPressed = 0
        'keyWaitState = False
        presetInputText = String.Empty
        SetTextMargins(0, TiVideo.COLS - 1)
        'Clear()
        video.Init()
        SetCursorPosition(lMargin, TiVideo.ROWS - 1)
    End Sub


    Protected Sub CursorTimer_Tick() Handles CursorTimer.Tick
        If inputMode Then BlinkCursor()
    End Sub

    Public Sub BlinkCursor()
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
        If cursorVisible Then
            video.PutChar(curR, curC, charUnderCursor)
            cursorVisible = False
            video.Invalidate()
        End If
    End Sub

    Public Function GetCursorPosition() As Integer
        Return curC - lMargin
    End Function

    Public Function GetCursorColumn() As Integer
        Return curC
    End Function

    Public Function GetCursorRow() As Integer
        Return curR
    End Function

    Public Sub Clear()
        video.ClearScreen()
    End Sub

    Public Sub Print(ByRef s As String)
        Dim hdr As String = chdr & ".Print"
        If String.IsNullOrEmpty(s) Then Return
        HideCursor()
        Dim l As Integer = s.Length
        Dim i As Integer = 0
        Dim c As Char
        Dim ch As Byte
        Do
            c = s.Chars(i)
            Try
                'ch = Convert.ToByte(c)
                ch = Convert.ToByte(Convert.ToUInt16(c) And 255)
            Catch ex As Exception
                Warn(hdr, ex.Message)
                ch = Convert.ToByte("?"c)
            End Try
            If Not IsStandardChar(ch) Then ch = 32
            video.PutChar(curR, curC, ch)
            MoveForward()
            i += 1
        Loop Until i = l
        picBox.Invalidate()
        'picBox.Update()
    End Sub

    Public Sub PrintLn(ByRef s As String)
        Print(s)
        NewLine()
    End Sub

    ' advance (r, c) position to the next right char, updating (r, c)
    ' return true when on bottom of screen and scroll-up is needed
    Public Function MoveForward(ByRef r As Integer, ByRef c As Integer) As Boolean
        If inputMode Then HideCursor()
        c += 1
        If c > rMargin Then Return NewLine(r, c)
        Return False
    End Function

    Public Sub MoveForward()
        If inputMode And inputTextLength >= inputTextMaxLength Then ' max length reached
            ti.sound.ErrorBeep()
            Return
        End If

        'Dim r As Integer = curR
        If MoveForward(curR, curC) Then ScrollUp()

        If inputMode Then
            inputCursorPosition += 1
            If inputCursorPosition > inputTextLength Then
                inputTextLength += 1 ' inputTextLength update
                If inputTextLength > 0 And curC = lMargin Then inputLineWraps += 1
                If showEndOfInputLine Then PutEOLMark()
            End If
        End If
    End Sub

    Public Sub MoveBack(ByRef r As Integer, ByRef c As Integer)
        If inputMode Then
            HideCursor()
            If inputCursorPosition > 0 Then inputCursorPosition -= 1
        End If
        c -= 1
        If c < lMargin Then
            c = 0
            MoveUp(r)
        End If
    End Sub

    Public Sub MoveBack()
        MoveBack(curR, curC)
    End Sub

    Public Sub MoveUp(ByRef r As Integer)
        If inputMode Then HideCursor()
        r -= 1
        If r < 0 Then r = 0
    End Sub

    Public Sub MoveDown(ByRef r As Integer)
        If inputMode Then HideCursor()
        r += 1
        If r >= TiVideo.ROWS Then r = TiVideo.ROWS - 1
    End Sub

    ' return true when on bottom of screen and scroll-up is needed
    Public Function Tab(tabPos As Integer, ByRef r As Integer, ByRef c As Integer) As Boolean
        If inputMode Then HideCursor()
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
        If inputMode Then HideCursor()
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
        'If left < 1 Then left = 1
        'If right >= TiVideo.ROWS Then right = TiVideo.ROWS - 2
        lMargin = left
        rMargin = right
        inputTextRowWidth = rMargin - lMargin + 1
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
        'StartTextInput()
        'If Not String.IsNullOrEmpty(presetInputText) Then Print(presetInputText)
        'If presetInputCursorColumn > 0 Then SetCursorPosition(curR, presetInputCursorColumn)
        StartTextInput(presetInputText, presetInputCursorColumn)
    End Sub

    Public Sub StartTextInput(Optional ByRef promptString As String = "",
                              Optional cursorStartPosition As Integer = 0)
        inputMode = True
        inputLineWraps = 0
        inputStartPosition = New Point(curC, curR)
        If Not String.IsNullOrEmpty(promptString) Then Print(promptString)
        inputTextLength = promptString.Length
        inputCursorPosition = cursorStartPosition
        If cursorStartPosition > 0 Then
            curR = inputStartPosition.Y
            curC += cursorStartPosition
        End If
        inputTextMaxLength = (rMargin - inputStartPosition.X) + inputTextRowWidth * MAX_INPUT_LWRAPS
        If showEndOfInputLine Then PutEOLMark()
        CursorTimer.Start()
    End Sub

    ' legge il testo dalla posizione iniziale di input fino alla fine della riga
    ' dove è posizionato il cursore
    ' finally puts the cursor at end of the input text
    Protected Function FinishInput() As String
        inputMode = False
        CursorTimer.Stop()
        HideCursor()
        If showEndOfInputLine Then RemoveEOLMark()
        Dim retStr As String = GetInputString()
        'If Not String.IsNullOrEmpty(retStr) Then NewLine()
        For i As Byte = 1 To inputLineWraps
            NewLine()
        Next
        'Info(chdr & ".FinishInput", "returned string: " & retStr)
        RaiseEvent TextInputCompleted(Me, retStr)
        presetInputText = String.Empty
        presetInputCursorColumn = 0
        Return retStr
    End Function

    Protected Function GetInputTextEndPosition() As Point
        Return New Point(lMargin + ((inputStartPosition.X - lMargin) + inputTextLength) Mod inputTextRowWidth,
                            inputStartPosition.Y + inputLineWraps)
    End Function

    ' return true when on bottom of screen and scroll-up is needed
    Public Function NewLine(ByRef r As Integer, ByRef c As Integer) As Boolean
        'If inputMode Then HideCursor()
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
        If inputMode Then HideCursor()
        curC = lMargin
    End Sub

    Public Sub Delete()
        Dim hdr As String = chdr & ".Delete"
        If Not inputMode Then
            Warn(hdr, "not in input mode - abort")
            Return
        End If
        If inputTextLength = 0 Then
            Warn(hdr, "empty line - abort")
            Return
        End If
        If inputCursorPosition = inputTextLength Then
            Warn(hdr, "no char to delete - abort")
            Return
        End If
        RemoveEOLMark()
        HideCursor()
        Dim tailCh As Byte = 32
        Dim headCh As Byte
        Dim startCol As Integer = 0
        Dim lastRow As Integer = inputStartPosition.Y + inputLineWraps
        For r As Integer = lastRow To curR Step -1
            If r = curR Then startCol = curC
            headCh = InputLineScrollLeft(r, startCol)
            video.PutChar(r, rMargin, tailCh)
            tailCh = headCh
        Next
        inputTextLength -= 1
        If showEndOfInputLine Then PutEOLMark()
        video.Invalidate()
    End Sub

    ' delete a char at left, scrolling left all the following chars (backspace + Delete)
    Public Sub BackCancel()
        Dim hdr As String = chdr & ".Cancel"
        If Not inputMode Then
            Warn(hdr, "not in input mode - abort")
            Return
        End If
        If inputCursorPosition = 0 Then
            Warn(hdr, "cursor at start position - abort")
            Return
        End If
        MoveBack()
        Delete()
    End Sub

    ' returns the leftmost char of input text (within margins)
    Protected Function InputLineScrollLeft(row As Byte, Optional col As Byte = 0) As Byte
        If TiVideo.OutOfScreenBounds(row, col) Then Return 0
        Dim loc As Integer = row * TiVideo.COLS + col
        Dim maxLoc As Integer = row * TiVideo.COLS + rMargin
        Dim rch As Byte = video.Peek(loc)
        Dim ch As Byte
        While loc < maxLoc
            ch = video.Peek(loc + 1)
            video.Poke(loc, ch)
            loc += 1
        End While
        video.Poke(maxLoc, 32)
        Return rch
    End Function

    ' inserts character ch in current position, scrolling right all the following input-line chars
    Public Sub Insert(ch As Byte)
        Dim hdr As String = chdr & ".Insert"
        If Not inputMode Then
            Warn(hdr, "not in input mode - abort")
            Return
        End If
        If inputTextLength >= inputTextMaxLength Then Warn(hdr, "line already fullfilled")
        HideCursor()
        Dim tailCh As Byte
        Dim headCh As Byte = ch
        Dim startCol As Integer = curC
        Dim lastRow As Integer = inputStartPosition.Y + inputLineWraps
        For r As Integer = curR To lastRow
            tailCh = InputLineScrollRight(r, startCol)
            video.PutChar(r, startCol, headCh)
            headCh = tailCh
            startCol = lMargin
        Next
        inputTextLength += 1
        Dim maxInputLength As Integer = (rMargin - curC + 1) + inputTextRowWidth * MAX_INPUT_LWRAPS
        If inputTextLength > maxInputLength Then
            Info(chdr & ".Insert", "max input line length reached: tail char discarded")
            inputTextLength = maxInputLength
        Else
            If showEndOfInputLine Then PutEOLMark()
        End If
        video.Invalidate()
    End Sub

    ' returns the rightmost char of input text (within margins)
    Protected Function InputLineScrollRight(row As Byte, Optional col As Byte = 0) As Byte
        If TiVideo.OutOfScreenBounds(row, col) Then Return 0
        Dim loc As UInt16 = row * TiVideo.COLS + col
        Dim maxLoc As UInt16 = row * TiVideo.COLS + rMargin
        Dim rch As Byte = video.Peek(maxLoc)
        Dim ch As Byte
        While loc < maxLoc
            maxLoc -= 1
            ch = video.Peek(maxLoc)
            video.Poke(maxLoc + 1, ch)
        End While
        video.Poke(loc, 32)
        Return rch
    End Function

    Public Sub SetCursorPosition(row As Integer, col As Integer, Optional respectMargins As Boolean = True)
        If row < 0 Or row >= TiVideo.ROWS Then Return
        If col < 0 Or col > TiVideo.COLS Then Return
        If respectMargins Then
            If col < lMargin Then col = lMargin Else If col > rMargin Then col = rMargin
        End If
        If inputMode Then HideCursor()
        curR = row
        curC = col
    End Sub

    Public Sub SetCursorPosition(pos As Point, Optional respectMargins As Boolean = True)
        SetCursorPosition(pos.Y, pos.X, respectMargins)
    End Sub

    Public Function GetInputString() As String
        Dim hdr As String = chdr & ".GetInputString"
        Info(hdr, "start pos: " & inputStartPosition.ToString() & " length: " & inputTextLength.ToString())
        If inputTextLength = 0 Then
            Info(hdr, "empty string")
            Return String.Empty
        End If
        Dim chrs(inputTextLength) As Char
        Dim p As New Point(inputStartPosition)
        For i As Integer = 0 To inputTextLength - 1
            chrs(i) = ChrW(video.GetChar(p))
            MoveForward(p.Y, p.X)
        Next
        Dim str As String = (New String(chrs, 0, inputTextLength)).Trim()
        Dbug(hdr, "input string: '" & str & "' (length:" & str.Length.ToString() & ")")
        Return str
    End Function

    Public Shared Function IsStandardChar(ByRef c As Byte) As Boolean
        'Return (c >= 32 And c < 127)
        Return c >= 32
    End Function

    Public Shared Function IsFunctionChar(ByRef c As Byte) As Boolean
        Return (c >= 1 And c < 16)
    End Function

    Public Sub ScrollUp()
        video.ScrollUp()
        If inputMode Then
            inputStartPosition.Y -= 1
            If showEndOfInputLine Then endOfLineMarkPos.Y -= 1
        End If
    End Sub

    Protected Sub PutEOLMark()
        endOfLineMarkPos = GetInputTextEndPosition()
        video.PutChar(endOfLineMarkPos.Y, endOfLineMarkPos.X, 60)
    End Sub

    Protected Sub RemoveEOLMark()
        video.PutChar(endOfLineMarkPos.Y, endOfLineMarkPos.X, 32)
        endOfLineMarkPos.Y = TiVideo.ROWS - 1
        endOfLineMarkPos.X = TiVideo.COLS - 1
    End Sub

    Public Sub KeyPress(key As Byte)
        Dim hdr As String = chdr & ".KeyPress"
        'Info(hdr, key.ToString())
        lastKeyPressed = key
        If inputMode Then

            ' normal keys
            If key >= 32 And key <> 127 Then
                If key >= 97 And key <= 122 Then key -= 32
                Try
                    If insertMode Then
                        Insert(key)
                        MoveForward()
                    Else
                        Print(Chr(key))
                    End If
                Catch ex As Exception
                    Warn(hdr, ex.ToString())
                End Try
                Return
            End If

            ' special keys
            Select Case key
                Case 3 ' cancel
                    Delete()
                Case 4 ' insert
                    insertMode = Not insertMode
                    Info(hdr, "insert mode: " & insertMode.ToString())
                Case 8 ' left
                    MoveBack()
                Case 9 ' right
                    MoveForward()
                Case 10, 11, 13 ' down, up, return
                    FinishInput()
                    'NewLine()
                Case 15 ' back
                    BackCancel()
                Case 16 ' shift
                    Return
                Case 17 ' ctrl
                    Return
                Case 18 ' alt
                    Return
                Case 127 ' cancel
                    Delete()
                Case Else
                    Print(" ")
                    Warn(chdr & ".KeyPress", "unhandled key #" & key.ToString())
            End Select
        End If
    End Sub

    Public Sub KeyRelease()
        lastKeyPressed = 0
    End Sub

    Public Function WaitKeyPress() As Byte
        Dim key As Byte
        Do While lastKeyPressed <> 0
            Threading.Thread.Sleep(50)
            Application.DoEvents()
        Loop
        key = lastKeyPressed
        Do While lastKeyPressed = 0
            Threading.Thread.Sleep(50)
            Application.DoEvents()
        Loop
        Return key
    End Function

    Public Function GetKeyPressed() As Byte
        Return lastKeyPressed
    End Function

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: eliminare lo stato gestito (oggetti gestiti)
            End If

            ' TODO: liberare risorse non gestite (oggetti non gestiti) ed eseguire l'override del finalizzatore
            ' TODO: impostare campi di grandi dimensioni su Null
            CursorTimer.Dispose()
            disposedValue = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Non modificare questo codice. Inserire il codice di pulizia nel metodo 'Dispose(disposing As Boolean)'
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub

End Class
