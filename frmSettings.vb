Imports System.ComponentModel

Public Class frmSettings
    Private Sub frmSettings_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        txtWorkDir.Text = My.Settings.workDir
    End Sub

    Private Sub bWorkDirSelect_Click(sender As Object, e As EventArgs) Handles bWorkDirSelect.Click
        Dim res As DialogResult = dlgFolderBrowser.ShowDialog()
        If res = DialogResult.Cancel Then Return
        txtWorkDir.Text = dlgFolderBrowser.SelectedPath
    End Sub

    Private Sub bApply_Click(sender As Object, e As EventArgs) Handles bApply.Click
        My.Settings.workDir = dlgFolderBrowser.SelectedPath
        My.Settings.Save()
        Me.Close()
    End Sub

    Private Sub bCancel_Click(sender As Object, e As EventArgs) Handles bCancel.Click
        Me.Close()
    End Sub
End Class