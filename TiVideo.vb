' TI99 graphic screen implementation

Imports System.Runtime.InteropServices

Enum TiColor
    Transparent = 1
    Black
    Green
    LightGreen
    DarkBlue
    LightBlue
    DarkRed
    Cyan
    Red
    LightRed
    DarkYellow
    LightYellow
    DarkGreen
    Magenta
    Gray
    White
End Enum

Public Class TiVideo
    Public Const ROWS As Integer = 24
    Public Const COLS As Integer = 32
    Public Const HEIGHT_PX As Integer = ROWS * 8
    Public Const WIDTH_PX As Integer = COLS * 8
    Public Const BORDER_PX As Integer = 8
    Public Const DEF_BACKCOLOR As Integer = TiColor.Cyan
    Public Const DEF_FORECOLOR As Integer = TiColor.Black
    Protected picBox As PictureBox
    Protected textMemory As Byte()
    Protected gr, bgr, cgr, dcgr, mcgr, ccgr As Graphics
    'Protected backBrush, foreBrush As Brush
    'protected pen As Pen
    Protected romCharBmp As Bitmap ' reduced system character bitmap (from ROM)
    Protected defCharBmp As Bitmap ' original full character shape set (black on transparent)
    Protected modCharBmp As Bitmap ' modified char shape set (black on transparent)
    Protected colCharBmp As Bitmap ' "runtime" full character colored shape set (with fore/back color)
    Protected activeCharBmp As Bitmap
    Protected altCharSet As Boolean
    Protected charBmp8x8 As Bitmap ' single char mini-bitmap
    Protected scrnBmp, scrBkpBmp As Bitmap ' screen bitmap and its backup
    Protected backColorCode(16) As Integer ' char sets' colors
    Protected foreColorCode(16) As Integer ' char sets' colors
    Protected sBrush(16) As SolidBrush 'sBrush(0) = screen color brush
    Protected screenColorCode As Integer

    Protected colr() As Color = {
        Color.Transparent,
        Color.Transparent,
        Color.Black,
        Color.Green,
        Color.LightGreen,
        Color.DarkBlue,
        Color.LightBlue,
        Color.DarkRed,
        Color.FromArgb(255, 20, 240, 255), ' cyan corrected
        Color.Red,
        Color.OrangeRed,
        Color.Yellow,
        Color.LightYellow,
        Color.DarkGreen,
        Color.Magenta,
        Color.Gray,
        Color.White
    }

    Protected emptyCharBmpStripe(32) As Byte

    Sub New(renderBox As PictureBox)
        'colr(8) = Color.FromArgb(255, 20, 240, 255) ' Cyan correction
        ReDim textMemory(ROWS * COLS)
        romCharBmp = New Bitmap(Application.StartupPath & "\ti99_charset.png")
        'charBmp = New Bitmap(charRomBmp.Width, 32 * 8, charRomBmp.PixelFormat)
        'RestoreAltCharShapesAndColors(False)
        scrnBmp = New Bitmap(WIDTH_PX + BORDER_PX * 2, HEIGHT_PX + BORDER_PX * 2, romCharBmp.PixelFormat)
        gr = Graphics.FromImage(scrnBmp)
        gr.InterpolationMode = Drawing2D.InterpolationMode.Low
        'gr.SmoothingMode = Drawing2D.SmoothingMode.None
        scrBkpBmp = New Bitmap(scrnBmp)
        bgr = Graphics.FromImage(scrBkpBmp)
        defCharBmp = New Bitmap(romCharBmp.Width, 64, romCharBmp.PixelFormat)
        dcgr = Graphics.FromImage(defCharBmp)
        dcgr.DrawImage(romCharBmp, 0, 8)
        dcgr.FillRectangle(New SolidBrush(Color.Black), 30 * 8 + 1, 0, 6, 7) ' cursor shape (char #30)
        charBmp8x8 = New Bitmap(8, 8, romCharBmp.PixelFormat)
        romCharBmp.Dispose()
        romCharBmp = Nothing

        picBox = renderBox
        picBox.Image = scrnBmp

        Dim i As Integer
        For i = 1 To 16
            sBrush(i) = New SolidBrush(colr(i))
        Next
        For i = 0 To 31
            emptyCharBmpStripe(i) = 0
        Next
        altCharSet = False
        Init()
    End Sub

    Public Sub Init()
        'RestoreDefaultColors()
        SetScreenColor(DEF_BACKCOLOR)
        RestoreAltCharShapesAndColors(False)
        SwitchToDefaultCharSet()
        ClearScreen()
    End Sub

    Public Sub Invalidate()
        picBox.Invalidate()
    End Sub

    ' fills screen with spaces
    Public Sub ClearScreen()
        gr.Clear(colr(backColorCode(1)))
        'For i As Integer = 0 To ROWS * COLS - 1
        '    textMemory(i) = 32 ' spaces
        'Next
        For r As Integer = 0 To ROWS - 1
            For c As Integer = 0 To COLS - 1
                PutChar(r, c, " "c, False)
            Next
        Next
        picBox.Invalidate()
    End Sub

    Public Sub RedrawAllCharsOnScreen(ByRef clearBefore As Boolean)
        Dim r, c As Integer
        If clearBefore Then gr.Clear(Color.Transparent)
        For r = 0 To ROWS - 1
            For c = 0 To COLS - 1
                DrawChar(r, c)
            Next
        Next
        picBox.Invalidate()
    End Sub

    Public Sub SetScreenColor(colorId As Integer)
        sBrush(0) = New SolidBrush(colr(colorId))
        picBox.BackColor = colr(colorId)
        picBox.Invalidate()
    End Sub

    Public Function GetScreenColor() As Integer
        Return screenColorCode
    End Function

    Public Function GetGroupBackColor(ByRef chSet As Integer) As Integer
        If chSet = 0 Then Return screenColorCode
        If chSet >= 1 And chSet <= 16 Then Return backColorCode(chSet) Else Return 0
    End Function

    Public Function GetGroupForeColor(ByRef chSet As Integer) As Integer
        If chSet >= 1 And chSet <= 16 Then Return foreColorCode(chSet) Else Return 0
    End Function

    Public Function GetCharBackColor(ByRef ch As Integer) As Integer
        Dim chSet As Integer = GetCharSet(ch)
        If chSet >= 1 And chSet <= 16 Then Return backColorCode(chSet) Else Return 0
    End Function

    Public Function GetCharForeColor(ByRef ch As Integer) As Integer
        Dim chSet As Integer = GetCharSet(ch)
        If chSet >= 1 And chSet <= 16 Then Return foreColorCode(chSet) Else Return 0
    End Function

    Public Sub SetGroupColor(charSet As Integer, foreColor_Code As Integer, backColor_Code As Integer)
        If charSet < 1 Or charSet > 16 Then Return
        If foreColor_Code > 0 And foreColor_Code <= 16 Then foreColorCode(charSet) = foreColor_Code
        If backColor_Code > 0 And backColor_Code <= 16 Then backColorCode(charSet) = backColor_Code
        UpdCharBmpColor(charSet, foreColor_Code, backColor_Code)
        'RedrawAllCharsOnScreen(False)
        UpdateCharGroupOnScreen(charSet)
    End Sub

    ' colora i caratteri del gruppo specificato nella bitmap del font "runtime" 
    Protected Sub UpdCharBmpColor(charSet As Integer,
                                  foreColor_Code As Integer,
                                  Optional backColor_Code As Integer = TiColor.Transparent)
        Dim foreColr As Color = colr(foreColor_Code)
        Dim backColr As Color = colr(backColor_Code)
        Dim csY, csX As Integer
        csY = 1 + ((charSet - 1) >> 2)
        csX = ((charSet - 1) Mod 4) * 8
        Dim opRect As New Rectangle(csX * 8, csY * 8, 64, 8)
        Dim srcBmpData, dstBmpData As Imaging.BitmapData
        Dim i, j As Integer
        Dim data(256) As Byte
        srcBmpData = modCharBmp.LockBits(opRect, Imaging.ImageLockMode.ReadOnly, Imaging.PixelFormat.Format32bppArgb)
        dstBmpData = colCharBmp.LockBits(opRect, Imaging.ImageLockMode.WriteOnly, Imaging.PixelFormat.Format32bppArgb)

        For i = 0 To 7
            Marshal.Copy(srcBmpData.Scan0 + i * srcBmpData.Stride, data, 0, 256)
            If backColor_Code = TiColor.Transparent Then
                For j = 0 To 255 Step 4
                    If data(j + 3) > 0 Then ' not transparent = foreground
                        data(j) = foreColr.B
                        data(j + 1) = foreColr.G
                        data(j + 2) = foreColr.R
                    End If
                Next j
            Else
                For j = 0 To 255 Step 4
                    If data(j + 3) > 0 Then ' not transparent = foreground
                        data(j) = foreColr.B
                        data(j + 1) = foreColr.G
                        data(j + 2) = foreColr.R
                    Else ' traansparent = background
                        data(j) = backColr.B
                        data(j + 1) = backColr.G
                        data(j + 2) = backColr.R
                        data(j + 3) = 255
                    End If
                Next j
            End If
            Marshal.Copy(data, 0, dstBmpData.Scan0 + i * dstBmpData.Stride, 256)
        Next i

        modCharBmp.UnlockBits(srcBmpData)
        colCharBmp.UnlockBits(dstBmpData)
    End Sub

    Public Sub ShowFont(ByRef firstRow As Integer)
        If firstRow < 0 Then firstRow = 0
        If firstRow > ROWS - 9 Then firstRow = ROWS - 9
        ClearScreen()
        gr.FillRectangle(sBrush(0), BORDER_PX, BORDER_PX, COLS * 8, 64)
        gr.DrawImage(activeCharBmp, BORDER_PX, BORDER_PX + 8 * firstRow)
        picBox.Invalidate()
    End Sub

    ' restore default screen color, chars colors & shapes
    Public Sub Restore()
        'RestoreDefaultColors()
        SetScreenColor(DEF_BACKCOLOR)
        RestoreAltCharShapesAndColors(True)
    End Sub

    ' restores default runtime character bitmap with default shapes and colors (black on transparent)
    Public Sub RestoreAltCharShapesAndColors(Optional ByRef redrawChars As Boolean = True)
        If modCharBmp Is Nothing Then
            modCharBmp = New Bitmap(defCharBmp)
            mcgr = Graphics.FromImage(modCharBmp)
            colCharBmp = New Bitmap(defCharBmp)
            ccgr = Graphics.FromImage(colCharBmp)
        Else
            mcgr.Clear(Color.Transparent)
        ccgr.Clear(Color.Transparent)
        mcgr.DrawImage(defCharBmp, 0, 0)
        ccgr.DrawImage(defCharBmp, 0, 0)
        End If

        For i As Integer = 1 To 16
            backColorCode(i) = TiColor.Transparent
            foreColorCode(i) = DEF_FORECOLOR
        Next

        If redrawChars Then RedrawAllCharsOnScreen(True)
    End Sub

    Public Sub SwitchToDefaultCharSet(Optional ByRef redrawChars As Boolean = True)
        activeCharBmp = defCharBmp
        'mcgr = dcgr
        altCharSet = False
        If redrawChars Then RedrawAllCharsOnScreen(True)
    End Sub

    Public Sub SwitchToAltCharSet(Optional ByRef redrawChars As Boolean = True)
        activeCharBmp = colCharBmp
        'mcgr = ccgr
        altCharSet = True
        If redrawChars Then RedrawAllCharsOnScreen(True)
    End Sub

    Public Function IsAltCharSetActive() As Boolean
        Return altCharSet
    End Function

    Public Sub SetCharShape(ByRef chr As Byte, ByRef data As Byte(), ByRef updatePicBox As Boolean)
        If chr < 32 Then Return
        Dim c, r, l As Integer
        l = data.Length - 1
        If l > 7 Then l = 7
        For r = 0 To l
            For c = 7 To 0 Step -1
                If (data(r) And (1 << c)) > 0 Then
                    charBmp8x8.SetPixel(7 - c, r, Color.Black)
                Else
                    charBmp8x8.SetPixel(7 - c, r, Color.Transparent)
                End If
            Next
        Next
        Do While r < 8
            For c = 0 To 7
                charBmp8x8.SetPixel(c, r, Color.Transparent)
            Next
            r += 1
        Loop
        mcgr.DrawImage(charBmp8x8, ((chr - 32) Mod 32) * 8, 8 + ((chr - 32) >> 5) * 8)
        UpdCharBmpColor(GetCharSet(chr), GetCharForeColor(chr), GetCharBackColor(chr))
        If updatePicBox Then UpdateCharOnScreen(chr)
    End Sub

    Public Shared Function GetCharSet(ByRef ch As Byte) As Integer
        Dim s As Integer = 1 + ((ch - 32) >> 3)
        If s < 1 Then Return 1
        If s > 16 Then Return 16
        Return s
    End Function

    ' redraw all the chars with code <chr> on the screen
    Protected Sub UpdateCharOnScreen(chr As Byte)
        If chr < 32 Then Return
        Dim c, r, base As Integer
        For r = 0 To ROWS - 1
            base = r * COLS
            For c = 0 To COLS - 1
                If textMemory(base + c) = chr Then DrawChar(r, c)
            Next
        Next
        picBox.Invalidate()
    End Sub

    ' redraw all chars of a group on the screen
    Protected Sub UpdateCharGroupOnScreen(chSet As Byte)
        If chSet < 1 Or chSet > 16 Then Return
        Dim startCh As Byte = 32 + (chSet - 1) * 8
        Dim endCh As Byte = startCh + 7
        Dim c, r, base As Integer
        Dim ch As Byte
        For r = 0 To ROWS - 1
            base = r * COLS
            For c = 0 To COLS - 1
                ch = textMemory(base + c)
                If ch >= startCh And ch <= endCh Then DrawChar(r, c)
            Next
        Next
        picBox.Invalidate()
    End Sub

    ' put a char both on screen image and in video text memory
    Public Sub PutChar(row As Integer, col As Integer, ch As Char, Optional updatePicBox As Boolean = False)
        'If ch < " "c Then Return
        PutChar(row, col, Convert.ToByte(ch), updatePicBox)
    End Sub

    Public Sub PutChar(row As Integer, col As Integer, chr As Byte, Optional updatePicBox As Boolean = False)
        If OutOfScreenBounds(row, col) Then Return
        textMemory(row * COLS + col) = chr
        DrawChar(row, col)
        If updatePicBox Then picBox.Invalidate()
    End Sub

    ' put a char on screen image only
    Protected Sub DrawChar(row As Integer, col As Integer)
        Dim ch As Byte = textMemory(row * COLS + col)
        Dim srcR As New Rectangle((ch Mod 32) * 8, (ch >> 5) * 8, 8, 8)
        Dim x As Integer = BORDER_PX + col * 8
        Dim y As Integer = BORDER_PX + row * 8
        ClrCharBox(row, col)
        gr.DrawImage(activeCharBmp, x, y, srcR, GraphicsUnit.Pixel)
    End Sub

    Public Sub FillWithBitmap(ByRef bmp As Bitmap)
        Dim destR As New Rectangle(BORDER_PX, BORDER_PX, COLS * 8, ROWS * 8)
        gr.DrawImage(bmp, destR)
        picBox.Invalidate()
        'gr.DrawImage(bmp, BORDER_PX + x, BORDER_PX + y)
    End Sub

    'Protected Sub DrawCursor(row As Integer, col As Integer)
    '    Dim x As Integer = BORDER_PX + col * 8
    '    Dim y As Integer = BORDER_PX + row * 8

    '    ClrCharBox(row, col)
    '    gr.FillRectangle(sBrush(DEF_FORECOLOR), x + 1, y, 6, 7) ' draw cursor

    '    'PutChar(row, col, Chr(30), True)
    'End Sub

    ' clears a single-char pixel area on screen
    Protected Sub ClrCharBox(row As Integer, col As Integer)
        Dim opRect As New Rectangle(BORDER_PX + col * 8, BORDER_PX + row * 8, 8, 8)
        Dim bmpData As Imaging.BitmapData
        bmpData = scrnBmp.LockBits(opRect, Imaging.ImageLockMode.WriteOnly, Imaging.PixelFormat.Format32bppArgb)
        For i As Integer = 0 To 7
            Marshal.Copy(emptyCharBmpStripe, 0, bmpData.Scan0 + i * scrnBmp.Width * 4, 32)
        Next i
        scrnBmp.UnlockBits(bmpData)
    End Sub

    Public Function GetChar(row As Integer, col As Integer) As Byte
        If OutOfScreenBounds(row, col) Then Return 0
        Return textMemory(row * COLS + col)
    End Function

    Public Sub ScrollUp()
        Dim srcR As New Rectangle(BORDER_PX, BORDER_PX + 8, WIDTH_PX, HEIGHT_PX - 8)
        bgr.Clear(Color.Transparent)
        bgr.DrawImage(scrnBmp, BORDER_PX, BORDER_PX, srcR, GraphicsUnit.Pixel)
        srcR.Y = BORDER_PX
        gr.Clear(Color.Transparent)
        gr.DrawImage(scrBkpBmp, BORDER_PX, BORDER_PX, srcR, GraphicsUnit.Pixel)
        ' empty bottom line
        If backColorCode(1) <> TiColor.Transparent Then
            gr.FillRectangle(sBrush(backColorCode(1)), BORDER_PX, BORDER_PX + HEIGHT_PX - 8, WIDTH_PX, 8)
        End If
        For i As Integer = 0 To (ROWS - 1) * COLS - 1
            textMemory(i) = textMemory(i + COLS)
        Next
        'Dim base As Integer = (ROWS - 1) * COLS
        For i = 0 To COLS - 1
            'textMemory(base + i) = 32 ' spaces
            PutChar(ROWS - 1, i, " "c, False)
        Next
        picBox.Invalidate()
    End Sub

    Public Function GetVideoString(fromRow As Integer, fromCol As Integer, length As Integer) As String
        Dim retStr As String = String.Empty
        If OutOfScreenBounds(fromRow, fromCol) Then Return retStr
        Dim base As Integer = fromRow * COLS + fromCol
        Dim max As Integer = COLS * ROWS
        Dim i As Integer = 0
        Do
            retStr &= Chr(textMemory(base + i))
            i += 1
        Loop While i < length And base + i < max
        Return retStr.Trim()
    End Function

    Public Shared Function OutOfScreenBounds(row As Integer, col As Integer) As Boolean
        Return row < 0 Or col < 0 Or row >= ROWS Or col >= COLS
    End Function

End Class
