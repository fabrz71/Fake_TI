Imports System.Threading
Imports System.Drawing.Drawing2D
Imports System.ComponentModel

Public Class FrmTiScreen
    Public Const chdr As String = "FrmTiScreen"
    Private ti As TI99
    Private tiStarted As Boolean
    'Private tiThread As Thread
    Private interpolation As InterpolationMode

    Private Sub FrmTiScreen_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        tiStarted = False
        interpolation = InterpolationMode.NearestNeighbor
        ti = New TI99(PicBox)
        'tiThread = New Thread(AddressOf ti.Init)
        If String.IsNullOrEmpty(My.Settings.workDir) Then
            MsgBox("E' necessario selezionare una directory di lavoro", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
            folderBrowser.ShowDialog()
            My.Settings.workDir = folderBrowser.SelectedPath
            My.Settings.Save()
        End If
        SetScreenSizeMultiplier(2)
    End Sub

    Private Sub FrmTiScreen_KeyPress(sender As Object, e As KeyPressEventArgs) Handles MyBase.KeyPress
        ti.keyPress(e.KeyChar)
    End Sub

    Private Sub FrmTiScreen_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        ti.KeyDown(e.KeyCode)
    End Sub

    Private Sub FrmTiScreen_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        ti.KeyRelease(e.KeyCode)
    End Sub

    Protected Function SetScreenSizeMultiplier(ByRef multiplier As Integer) As Boolean
        Dim newSize = ti.video.ScreenBitmapSize * multiplier
        Dim borders As Size = Me.Size - PicBox.Size
        newSize += borders
        If newSize.Width > Screen.PrimaryScreen.Bounds.Width Or
            newSize.Height > Screen.PrimaryScreen.Bounds.Height Then Return False
        Me.Size = newSize
        Return True
    End Function

    Protected Function GetSizeRatio() As Single
        If ti Is Nothing Then Return 0
        Return Convert.ToSingle(PicBox.Width) / Convert.ToSingle(ti.video.ScreenBitmapSize.Width)
    End Function

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Me.Close()
        Application.Exit()
    End Sub

    Private Sub FrmTiScreen_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        lSizeRatio.Text = "x" & GetSizeRatio().ToString
        If Not tiStarted Then
            ti.Init()
            'tiThread.Start()
            tiStarted = True
        End If
    End Sub

    Private Sub FrmTiScreen_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        lSizeRatio.Text = "x" & GetSizeRatio().ToString
    End Sub

    Private Sub FrmTiScreen_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        lSizeRatio.Text = "x" & GetSizeRatio().ToString
    End Sub

    Private Sub X1ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles X1ToolStripMenuItem.Click
        SetScreenSizeMultiplier(1)
    End Sub

    Private Sub X2ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles X2ToolStripMenuItem.Click
        SetScreenSizeMultiplier(2)
    End Sub

    Private Sub X3ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles X3ToolStripMenuItem.Click
        SetScreenSizeMultiplier(3)
    End Sub

    Private Sub SettingsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SettingsToolStripMenuItem.Click
        frmSettings.Show(Me)
    End Sub

    Private Sub PicBox_Paint(sender As Object, e As PaintEventArgs) Handles PicBox.Paint
        e.Graphics.InterpolationMode = interpolation
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half
        Dim pixelBitmap As Bitmap = ti.video.ScreenBitmap
        e.Graphics.DrawImage(pixelBitmap, GetScaledImageRect(pixelBitmap, DirectCast(sender, Control)))
    End Sub

    Public Function GetScaledImageRect(image As Image, canvas As Control) As RectangleF
        Return GetScaledImageRect(image, canvas.ClientSize)
    End Function

    Public Function GetScaledImageRect(image As Image, containerSize As SizeF) As RectangleF
        Dim imgRect As RectangleF = RectangleF.Empty

        Dim scaleFactor As Single = CSng(image.Width / image.Height)
        Dim containerRatio As Single = containerSize.Width / containerSize.Height

        If containerRatio >= scaleFactor Then
            imgRect.Size = New SizeF(containerSize.Height * scaleFactor, containerSize.Height)
            imgRect.Location = New PointF((containerSize.Width - imgRect.Width) / 2, 0)
        Else
            imgRect.Size = New SizeF(containerSize.Width, containerSize.Width / scaleFactor)
            imgRect.Location = New PointF(0, (containerSize.Height - imgRect.Height) / 2)
        End If
        Return imgRect
    End Function

    Protected Sub SetInterpolationMode(ByRef im As InterpolationMode)
        interpolation = im
        PicBox.Invalidate()
    End Sub

    Private Sub NoneToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles NoneToolStripMenuItem.Click
        SetInterpolationMode(InterpolationMode.NearestNeighbor)
    End Sub

    Private Sub LowQualityToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LowQualityToolStripMenuItem.Click
        SetInterpolationMode(InterpolationMode.Low)
    End Sub

    Private Sub BilinearToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles BilinearToolStripMenuItem.Click
        SetInterpolationMode(InterpolationMode.Bilinear)
    End Sub

    Private Sub BicubicToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles BicubicToolStripMenuItem.Click
        SetInterpolationMode(InterpolationMode.Bicubic)
    End Sub

    Private Sub FrmTiScreen_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        ti.Quit()
    End Sub
End Class