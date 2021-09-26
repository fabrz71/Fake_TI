Imports System.Threading

Public Class FrmTiScreen
    Public Const chdr As String = "FrmTiScreen"
    Private ti As TI99
    'Private tiStarted As Boolean
    Private tiThread As Thread

    Private Sub FrmTiScreen_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'tiStarted = False
        ti = New TI99(PicBox)
        If String.IsNullOrEmpty(My.Settings.workDir) Then
            MsgBox("E' necessario selezionare una directory di lavoro", MsgBoxStyle.Information + MsgBoxStyle.OkOnly)
            folderBrowser.ShowDialog()
            My.Settings.workDir = folderBrowser.SelectedPath
            My.Settings.Save()
        End If
    End Sub

    Private Sub FrmTiScreen_KeyPress(sender As Object, e As KeyPressEventArgs) Handles MyBase.KeyPress
        Dim ch As Byte
        Try
            ch = Convert.ToByte(e.KeyChar)
        Catch ex As Exception
            Warn(chdr & ".Keypress event", "error converting KeyChar to byte")
            Return
        End Try
        If ch >= 32 Then ti.textUI.KeyPress(ch)
    End Sub

    Private Sub FrmTiScreen_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode >= 32 Then Return ' for compatibility with KeyPress
        Dim ch As Byte = TI99.KeyCodeToTi99Key(e.KeyCode)
        If ch > 0 Then ti.textUI.KeyPress(ch)
    End Sub

    Private Sub FrmTiScreen_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        Dim ch As Byte = TI99.KeyCodeToTi99Key(e.KeyCode)
        If ch > 0 Then ti.textUI.KeyRelease(ch)
    End Sub

    'Private Sub FrmTiScreen_Activated(sender As Object, e As EventArgs) Handles Me.Activated
    '    If Not tiStarted Then

    '        ' option 1
    '        ti.Init()

    '        ' option2
    '        'tiThread = New Thread(AddressOf ti.Init)
    '        'tiThread.Start()

    '        tiStarted = True
    '    End If
    'End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs)
        ti.Quit()
        Me.Close()
        Application.Exit()
    End Sub

    Private Sub FrmTiScreen_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        ti.Init()
    End Sub
End Class