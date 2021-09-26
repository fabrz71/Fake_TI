<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmSettings
    Inherits System.Windows.Forms.Form

    'Form esegue l'override del metodo Dispose per pulire l'elenco dei componenti.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Richiesto da Progettazione Windows Form
    Private components As System.ComponentModel.IContainer

    'NOTA: la procedura che segue è richiesta da Progettazione Windows Form
    'Può essere modificata in Progettazione Windows Form.  
    'Non modificarla mediante l'editor del codice.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.txtWorkDir = New System.Windows.Forms.TextBox()
        Me.bWorkDirSelect = New System.Windows.Forms.Button()
        Me.dlgFolderBrowser = New System.Windows.Forms.FolderBrowserDialog()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(9, 15)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(109, 15)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "disk and files folder"
        '
        'txtWorkDir
        '
        Me.txtWorkDir.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtWorkDir.Location = New System.Drawing.Point(124, 12)
        Me.txtWorkDir.Name = "txtWorkDir"
        Me.txtWorkDir.Size = New System.Drawing.Size(286, 23)
        Me.txtWorkDir.TabIndex = 1
        '
        'bWorkDirSelect
        '
        Me.bWorkDirSelect.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.bWorkDirSelect.Location = New System.Drawing.Point(416, 12)
        Me.bWorkDirSelect.Name = "bWorkDirSelect"
        Me.bWorkDirSelect.Size = New System.Drawing.Size(58, 23)
        Me.bWorkDirSelect.TabIndex = 2
        Me.bWorkDirSelect.Text = "Choose"
        Me.bWorkDirSelect.UseVisualStyleBackColor = True
        '
        'frmSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(486, 187)
        Me.Controls.Add(Me.bWorkDirSelect)
        Me.Controls.Add(Me.txtWorkDir)
        Me.Controls.Add(Me.Label1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.Name = "frmSettings"
        Me.Text = "frmSettings"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents txtWorkDir As TextBox
    Friend WithEvents bWorkDirSelect As Button
    Friend WithEvents dlgFolderBrowser As FolderBrowserDialog
End Class
