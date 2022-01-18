' TI99 video graphics implementation

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
    Public Const ROWS As UInt16 = 24
    Public Const COLS As UInt16 = 32
    Public Const HEIGHT_PX As Integer = ROWS * 8
    Public Const WIDTH_PX As Integer = COLS * 8
    Public Const TXT_VIDEO_MEM_SIZE As Integer = COLS * ROWS
    Public Const BORDER_PX As Integer = 8
    Public Const DEF_BACKCOLOR As Integer = TiColor.Cyan
    Public Const DEF_FORECOLOR As Integer = TiColor.Black
    Public Const chdr As String = "TiVideo"
    'Protected WithEvents picBox As PictureBox
    Protected PicBox As PictureBox
    'Protected imageChanged As Boolean
    Protected textMemory As Byte()
    Protected gr As Graphics ' screen
    Protected bgr As Graphics ' cabkup screen
    Protected cgr As Graphics ' screen
    Protected dcgr As Graphics ' default charset bibmap
    Protected mcgr As Graphics ' modified charset bitmap
    Protected ccgr As Graphics ' colored modified charset bitmap
    Protected chgr As Graphics ' single char bitmap
    Protected romCharBmp As Bitmap ' reduced system character bitmap (from ROM)
    Protected defCharBmp As Bitmap ' original full character shape set (black on transparent)
    Protected modCharBmp As Bitmap ' modified char shape set (black on transparent)
    Protected colCharBmp As Bitmap ' "runtime" full character colored shape set (with fore/back color)
    Protected activeCharBmp As Bitmap
    Protected altCharSet As Boolean
    Protected emptyCharBmp, charBmp8x8 As Bitmap ' single char mini-bitmap (8x8 pixels)
    Protected scrnBmp, scrBkpBmp As Bitmap ' screen bitmap and its backup
    Protected backColorCode(16) As Integer ' background charsets' colors
    Protected foreColorCode(16) As Integer ' foreground charsets' colors
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
        ReDim textMemory(ROWS * COLS)
        romCharBmp = New Bitmap(My.Resources.ti99_charset)
        scrnBmp = New Bitmap(WIDTH_PX + BORDER_PX * 2, HEIGHT_PX + BORDER_PX * 2, romCharBmp.PixelFormat)
        gr = Graphics.FromImage(scrnBmp)
        scrBkpBmp = New Bitmap(scrnBmp)
        bgr = Graphics.FromImage(scrBkpBmp)
        CreateDefaultCharsBitmap(romCharBmp)
        charBmp8x8 = New Bitmap(8, 8, romCharBmp.PixelFormat)
        chgr = Graphics.FromImage(charBmp8x8)
        PicBox = renderBox

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
        PicBox.Invalidate()
    End Sub

    Public ReadOnly Property ScreenBitmap As Bitmap
        Get
            Return scrnBmp
        End Get
    End Property

    Public ReadOnly Property ScreenBitmapSize As Size
        Get
            Return scrnBmp.Size
        End Get
    End Property

    Public ReadOnly Property EmptyCharBmpData As Byte()
        Get
            Return emptyCharBmpStripe
        End Get
    End Property

    ' fills screen with spaces
    Public Sub ClearScreen()
        gr.Clear(colr(backColorCode(1)))
        For r As Integer = 0 To ROWS - 1
            For c As Integer = 0 To COLS - 1
                PutChar(r, c, " "c, False)
            Next
        Next
        PicBox.Invalidate()
    End Sub

    Public Sub RedrawAllCharsOnScreen(ByRef clearBefore As Boolean)
        Dim r, c As Integer
        If clearBefore Then gr.Clear(Color.Transparent)
        For r = 0 To ROWS - 1
            For c = 0 To COLS - 1
                DrawChar(r, c)
            Next
        Next
        PicBox.Invalidate()
    End Sub

    Public Sub SetScreenColor(colorId As Integer)
        sBrush(0) = New SolidBrush(colr(colorId))
        PicBox.BackColor = colr(colorId)
        PicBox.Invalidate()
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
        Dim chSet As Integer = GetCharSetNumber(ch)
        If chSet >= 1 And chSet <= 16 Then Return backColorCode(chSet) Else Return 0
    End Function

    Public Function GetCharForeColor(ByRef ch As Integer) As Integer
        Dim chSet As Integer = GetCharSetNumber(ch)
        If chSet >= 1 And chSet <= 16 Then Return foreColorCode(chSet) Else Return 0
    End Function

    Public Sub SetGroupColor(charSet As Integer, foreColor_Code As Integer, backColor_Code As Integer)
        If charSet < 1 Or charSet > 16 Then Return
        If foreColor_Code > 0 And foreColor_Code <= 16 Then foreColorCode(charSet) = foreColor_Code
        If backColor_Code > 0 And backColor_Code <= 16 Then backColorCode(charSet) = backColor_Code
        UpdCharSetBmpColor(charSet, foreColor_Code, backColor_Code)
        'RedrawAllCharsOnScreen(False)
        RefreshCharGroup(charSet)
    End Sub

    Protected Sub UpdCharBmpColor(charNumber As Byte,
                                  foreColor_Code As Integer,
                                  Optional backColor_Code As Integer = TiColor.Transparent)
        Dim foreColr As Color = colr(foreColor_Code)
        Dim backColr As Color = colr(backColor_Code)
        Dim foreColrTransparency As Boolean = (foreColr = Color.Transparent)
        Dim backColrTransparency As Boolean = (backColr = Color.Transparent)
        Dim csY As UInt16 = charNumber >> 5
        Dim csX As UInt16 = charNumber Mod 32
        Dim opRect As New Rectangle(csX << 3, csY << 3, 8, 8)
        Dim srcBmpData, dstBmpData As Imaging.BitmapData
        Dim i, j As Integer
        Dim data(32) As Byte

        srcBmpData = modCharBmp.LockBits(opRect, Imaging.ImageLockMode.ReadOnly, Imaging.PixelFormat.Format32bppArgb)
        dstBmpData = colCharBmp.LockBits(opRect, Imaging.ImageLockMode.WriteOnly, Imaging.PixelFormat.Format32bppArgb)
        For i = 0 To 7 ' 8 pixel scanlines
            Marshal.Copy(srcBmpData.Scan0 + i * dstBmpData.Stride, data, 0, 32)
            For j = 0 To 31 Step 4
                If data(j + 3) > 0 Then ' not transparent pixel = set as foreground color
                    data(j) = foreColr.B
                    data(j + 1) = foreColr.G
                    data(j + 2) = foreColr.R
                    data(j + 3) = foreColr.A
                Else ' transparent background
                    data(j) = backColr.B
                    data(j + 1) = backColr.G
                    data(j + 2) = backColr.R
                    data(j + 3) = backColr.A
                End If
            Next j
            Marshal.Copy(data, 0, dstBmpData.Scan0 + i * dstBmpData.Stride, 32)
        Next i

        modCharBmp.UnlockBits(srcBmpData)
        colCharBmp.UnlockBits(dstBmpData)
    End Sub

    '' colora i caratteri del gruppo specificato nella bitmap "runtime" 
    'Protected Sub UpdCharSetBmpColor(charSet As Integer,
    '                              foreColor_Code As Integer,
    '                              Optional backColor_Code As Integer = TiColor.Transparent)
    '    Dim foreColr As Color = colr(foreColor_Code)
    '    Dim backColr As Color = colr(backColor_Code)
    '    Dim csY, csX As Integer
    '    csY = 1 + ((charSet - 1) >> 2)
    '    csX = ((charSet - 1) Mod 4) * 8
    '    Dim opRect As New Rectangle(csX * 8, csY * 8, 64, 8)
    '    Dim srcBmpData, dstBmpData As Imaging.BitmapData
    '    Dim i, j As Integer
    '    Dim data(256) As Byte
    '    srcBmpData = modCharBmp.LockBits(opRect, Imaging.ImageLockMode.ReadOnly, Imaging.PixelFormat.Format32bppArgb)
    '    dstBmpData = colCharBmp.LockBits(opRect, Imaging.ImageLockMode.WriteOnly, Imaging.PixelFormat.Format32bppArgb)

    '    For i = 0 To 7 ' 8 pixel scanlines
    '        Marshal.Copy(srcBmpData.Scan0 + i * srcBmpData.Stride, data, 0, 256)
    '        If backColor_Code = TiColor.Transparent Then
    '            For j = 0 To 255 Step 4
    '                If data(j + 3) > 0 Then ' not transparent = foreground
    '                    data(j) = foreColr.B
    '                    data(j + 1) = foreColr.G
    '                    data(j + 2) = foreColr.R
    '                    data(j + 3) = 255
    '                Else
    '                    data(j + 3) = 0
    '                End If
    '            Next j
    '        Else
    '            For j = 0 To 255 Step 4
    '                If data(j + 3) > 0 Then ' not transparent = foreground
    '                    data(j) = foreColr.B
    '                    data(j + 1) = foreColr.G
    '                    data(j + 2) = foreColr.R
    '                    data(j + 3) = 255
    '                Else ' transparent = background
    '                    data(j) = backColr.B
    '                    data(j + 1) = backColr.G
    '                    data(j + 2) = backColr.R
    '                    data(j + 3) = 255
    '                End If
    '            Next j
    '        End If
    '        Marshal.Copy(data, 0, dstBmpData.Scan0 + i * dstBmpData.Stride, 256)
    '    Next i

    '    modCharBmp.UnlockBits(srcBmpData)
    '    colCharBmp.UnlockBits(dstBmpData)
    'End Sub

    ' colora i caratteri del gruppo specificato nella bitmap "runtime" 
    Protected Sub UpdCharSetBmpColor(charSet As Integer,
                                  foreColor_Code As Integer,
                                  Optional backColor_Code As Integer = TiColor.Transparent)
        Dim firstChar As Byte = Convert.ToByte(32 + (charSet - 1) * 8)
        For i As Byte = 0 To 7
            UpdCharBmpColor(firstChar + i, foreColor_Code, backColor_Code)
        Next
    End Sub

    Public Sub ShowActiveFontBitmap(ByRef firstRow As Integer)
        If firstRow < 0 Then firstRow = 0
        If firstRow > ROWS - 9 Then firstRow = ROWS - 9
        ClearScreen()
        gr.FillRectangle(sBrush(0), BORDER_PX, BORDER_PX, COLS * 8, 64)
        gr.DrawImage(activeCharBmp, BORDER_PX, BORDER_PX + 8 * firstRow)
        PicBox.Invalidate()
    End Sub

    ' restore default screen color, chars colors & shapes
    Public Sub Restore()
        'RestoreDefaultColors()
        SetScreenColor(DEF_BACKCOLOR)
        RestoreAltCharShapesAndColors(True)
    End Sub

    Protected Sub CreateDefaultCharsBitmap(ByRef romBmp As Bitmap)
        defCharBmp = New Bitmap(romBmp.Width, 64, romBmp.PixelFormat)
        dcgr = Graphics.FromImage(defCharBmp)
        dcgr.DrawImage(romBmp, 0, 8)
        dcgr.FillRectangle(New SolidBrush(Color.Black), 30 * 8 + 1, 0, 6, 7) ' cursor shape (char #30)
    End Sub

    ' restores runtime character bitmap with default shapes and colors (black on transparent)
    Public Sub RestoreAltCharShapesAndColors(Optional ByRef redrawChars As Boolean = True,
                                             Optional ByRef deepRestore As Boolean = False)
        If modCharBmp Is Nothing Then
            modCharBmp = New Bitmap(defCharBmp)
            mcgr = Graphics.FromImage(modCharBmp)
            colCharBmp = New Bitmap(defCharBmp)
            ccgr = Graphics.FromImage(colCharBmp)
        Else
            If deepRestore Then CreateDefaultCharsBitmap(romCharBmp)
            mcgr.Clear(Color.Transparent)
            mcgr.DrawImage(defCharBmp, 0, 0)
            ccgr.Clear(Color.Transparent)
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
        'If chr < 32 Then Return
        Dim c, r, l As Integer

        ClearBmpBox8x8(charBmp8x8, 0, 0)
        l = data.Length - 1
        If l > 7 Then l = 7
        For r = 0 To l
            For c = 7 To 0 Step -1
                If (data(r) And (1 << c)) > 0 Then charBmp8x8.SetPixel(7 - c, r, Color.Black)
            Next
        Next

        r = chr >> 5
        c = chr Mod 32
        If chr > 127 Then ' presistent shape
            ClearBmpBox8x8(defCharBmp, r, c)
            dcgr.DrawImage(charBmp8x8, c * 8, r * 8)
        End If
        ClearBmpBox8x8(modCharBmp, r, c)
        mcgr.DrawImage(charBmp8x8, c * 8, r * 8)
        UpdCharBmpColor(chr, GetCharForeColor(chr), GetCharBackColor(chr))
        If updatePicBox Then UpdateCharOnScreen(chr)
    End Sub

    ' clears a single-char pixel area on the bitmap
    Protected Sub ClearBmpBox8x8(ByRef bitmap As Bitmap, ByRef row As Byte, ByRef col As Byte,
                                 Optional ByRef offset As Integer = 0)
        Dim opRect As New Rectangle(offset + col * 8, offset + row * 8, 8, 8)
        Dim bmpData As Imaging.BitmapData
        bmpData = bitmap.LockBits(opRect, Imaging.ImageLockMode.WriteOnly, Imaging.PixelFormat.Format32bppArgb)
        For i As Integer = 0 To 7
            Marshal.Copy(emptyCharBmpStripe, 0, bmpData.Scan0 + i * bmpData.Stride, 32)
        Next i
        bitmap.UnlockBits(bmpData)
    End Sub

    Public Shared Function GetCharSetNumber(ByRef ch As Byte) As Integer
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
        PicBox.Invalidate()
    End Sub

    ' redraw all chars of a group on the screen
    Protected Sub RefreshCharGroup(chSet As Byte)
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
        PicBox.Invalidate()
    End Sub

    ' put a char both on screen image and in video text memory
    Public Sub PutChar(row As Integer, col As Integer, ch As Char, Optional updatePicBox As Boolean = False)
        'If ch < " "c Then Return
        PutChar(row, col, Convert.ToByte(ch), updatePicBox)
    End Sub

    Public Sub PutChar(row As Integer, col As Integer, chr As Byte, Optional updatePicBox As Boolean = False)
        If OutOfScreenBounds(row, col) Then
            Warn(chdr & ".PutChar", "out of bounds position at row:" & row.ToString() & ", col:" & col.ToString())
            Return
        End If
        textMemory(row * COLS + col) = chr
        DrawChar(row, col)
        If updatePicBox Then PicBox.Invalidate()
    End Sub

    Public Sub Poke(ByRef txtMemLoc As UInt16, ByRef chr As Byte, Optional updatePicBox As Boolean = False)
        If txtMemLoc >= TXT_VIDEO_MEM_SIZE Then
            Warn(chdr & ".Poke", "illegal memory location " & txtMemLoc.ToString())
            Return
        End If
        textMemory(txtMemLoc) = chr
        'Dim row As Integer = txtMemLoc / COLS
        Dim row As Integer = Int(CSng(txtMemLoc) / CSng(COLS))
        Dim col As Integer = txtMemLoc Mod COLS
        DrawChar(row, col)
        If updatePicBox Then PicBox.Invalidate()
    End Sub

    Public Function Peek(ByRef txtMemLoc As UInt16) As Byte
        If txtMemLoc < TXT_VIDEO_MEM_SIZE Then
            Return textMemory(txtMemLoc)
        Else
            Warn(chdr & ".Peek", "illegal memory location " & txtMemLoc.ToString())
            Return 0
        End If
    End Function

    ' put a char on screen image only
    Protected Sub DrawChar(row As Integer, col As Integer)
        Dim ch As Byte = textMemory(row * COLS + col)
        Dim srcR As New Rectangle((ch Mod 32) * 8, (ch >> 5) * 8, 8, 8)
        Dim x As Integer = BORDER_PX + col * 8
        Dim y As Integer = BORDER_PX + row * 8
        'ClrCharBox(row, col)
        ClearBmpBox8x8(scrnBmp, row, col, BORDER_PX)
        gr.DrawImage(activeCharBmp, x, y, srcR, GraphicsUnit.Pixel)
    End Sub

    Public Sub FillWithBitmap(ByRef bmp As Bitmap)
        Dim destR As New Rectangle(BORDER_PX, BORDER_PX, COLS * 8, ROWS * 8)
        gr.DrawImage(bmp, destR)
        PicBox.Invalidate()
        'gr.DrawImage(bmp, BORDER_PX + x, BORDER_PX + y)
    End Sub

    '' clears a single-char pixel area on screen
    'Protected Sub ClrCharBox(row As Integer, col As Integer)
    '    Dim opRect As New Rectangle(BORDER_PX + col * 8, BORDER_PX + row * 8, 8, 8)
    '    Dim bmpData As Imaging.BitmapData
    '    bmpData = scrnBmp.LockBits(opRect, Imaging.ImageLockMode.WriteOnly, Imaging.PixelFormat.Format32bppArgb)
    '    For i As Integer = 0 To 7
    '        Marshal.Copy(emptyCharBmpStripe, 0, bmpData.Scan0 + i * scrnBmp.Width * 4, 32)
    '    Next i
    '    scrnBmp.UnlockBits(bmpData)
    'End Sub

    Public Function GetChar(row As Integer, col As Integer) As Byte
        If OutOfScreenBounds(row, col) Then Return 0
        Return textMemory(row * COLS + col)
    End Function

    Public Function GetChar(p As Point) As Byte
        Return GetChar(p.Y, p.X)
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
        PicBox.Invalidate()
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

    'Private Sub picBox_Paint(sender As Object, e As PaintEventArgs) Handles picBox.Paint

    'End Sub
End Class
