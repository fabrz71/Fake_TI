Public Class frmSettings
    Private Sub frmSettings_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        txtWorkDir.Text = My.Settings.workDir
    End Sub

    Private Sub bWorkDirSelect_Click(sender As Object, e As EventArgs) Handles bWorkDirSelect.Click
        dlgFolderBrowser.ShowDialog()
        My.Settings.workDir = dlgFolderBrowser.SelectedPath
        My.Settings.Save()
    End Sub
End Class