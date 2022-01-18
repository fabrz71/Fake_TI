<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FrmTiScreen
    Inherits System.Windows.Forms.Form

    'Form esegue l'override del metodo Dispose per pulire l'elenco dei componenti.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.PicBox = New System.Windows.Forms.PictureBox()
        Me.folderBrowser = New System.Windows.Forms.FolderBrowserDialog()
        Me.MenuStrip = New System.Windows.Forms.MenuStrip()
        Me.FileToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SettingsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ExitToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.EditToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.X1ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.X2ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.X3ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.FilterToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.HelpToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AboutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.StatusStrip = New System.Windows.Forms.StatusStrip()
        Me.lSizeRatio = New System.Windows.Forms.ToolStripStatusLabel()
        Me.NoneToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.LowQualityToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.BilinearToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.BicubicToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        CType(Me.PicBox, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.MenuStrip.SuspendLayout()
        Me.StatusStrip.SuspendLayout()
        Me.SuspendLayout()
        '
        'PicBox
        '
        Me.PicBox.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.PicBox.BackColor = System.Drawing.SystemColors.ControlDark
        Me.PicBox.InitialImage = Nothing
        Me.PicBox.Location = New System.Drawing.Point(0, 24)
        Me.PicBox.Margin = New System.Windows.Forms.Padding(0)
        Me.PicBox.Name = "PicBox"
        Me.PicBox.Size = New System.Drawing.Size(562, 403)
        Me.PicBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.PicBox.TabIndex = 0
        Me.PicBox.TabStop = False
        '
        'MenuStrip
        '
        Me.MenuStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FileToolStripMenuItem, Me.EditToolStripMenuItem, Me.HelpToolStripMenuItem})
        Me.MenuStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow
        Me.MenuStrip.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip.Name = "MenuStrip"
        Me.MenuStrip.Size = New System.Drawing.Size(562, 24)
        Me.MenuStrip.TabIndex = 1
        Me.MenuStrip.Text = "MenuStrip1"
        '
        'FileToolStripMenuItem
        '
        Me.FileToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.SettingsToolStripMenuItem, Me.ExitToolStripMenuItem})
        Me.FileToolStripMenuItem.Name = "FileToolStripMenuItem"
        Me.FileToolStripMenuItem.Size = New System.Drawing.Size(37, 20)
        Me.FileToolStripMenuItem.Text = "&File"
        '
        'SettingsToolStripMenuItem
        '
        Me.SettingsToolStripMenuItem.Name = "SettingsToolStripMenuItem"
        Me.SettingsToolStripMenuItem.Size = New System.Drawing.Size(116, 22)
        Me.SettingsToolStripMenuItem.Text = "Settings"
        '
        'ExitToolStripMenuItem
        '
        Me.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem"
        Me.ExitToolStripMenuItem.Size = New System.Drawing.Size(116, 22)
        Me.ExitToolStripMenuItem.Text = "E&xit"
        '
        'EditToolStripMenuItem
        '
        Me.EditToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.CutToolStripMenuItem, Me.FilterToolStripMenuItem})
        Me.EditToolStripMenuItem.Name = "EditToolStripMenuItem"
        Me.EditToolStripMenuItem.Size = New System.Drawing.Size(44, 20)
        Me.EditToolStripMenuItem.Text = "&View"
        '
        'CutToolStripMenuItem
        '
        Me.CutToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.X1ToolStripMenuItem, Me.X2ToolStripMenuItem, Me.X3ToolStripMenuItem})
        Me.CutToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.CutToolStripMenuItem.Name = "CutToolStripMenuItem"
        Me.CutToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.CutToolStripMenuItem.Text = "&Size"
        '
        'X1ToolStripMenuItem
        '
        Me.X1ToolStripMenuItem.Name = "X1ToolStripMenuItem"
        Me.X1ToolStripMenuItem.Size = New System.Drawing.Size(86, 22)
        Me.X1ToolStripMenuItem.Text = "x1"
        '
        'X2ToolStripMenuItem
        '
        Me.X2ToolStripMenuItem.Name = "X2ToolStripMenuItem"
        Me.X2ToolStripMenuItem.Size = New System.Drawing.Size(86, 22)
        Me.X2ToolStripMenuItem.Text = "x2"
        '
        'X3ToolStripMenuItem
        '
        Me.X3ToolStripMenuItem.Name = "X3ToolStripMenuItem"
        Me.X3ToolStripMenuItem.Size = New System.Drawing.Size(86, 22)
        Me.X3ToolStripMenuItem.Text = "x3"
        '
        'FilterToolStripMenuItem
        '
        Me.FilterToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.NoneToolStripMenuItem, Me.LowQualityToolStripMenuItem, Me.BilinearToolStripMenuItem, Me.BicubicToolStripMenuItem})
        Me.FilterToolStripMenuItem.Name = "FilterToolStripMenuItem"
        Me.FilterToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.FilterToolStripMenuItem.Text = "&Filter"
        '
        'HelpToolStripMenuItem
        '
        Me.HelpToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AboutToolStripMenuItem})
        Me.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem"
        Me.HelpToolStripMenuItem.Size = New System.Drawing.Size(44, 20)
        Me.HelpToolStripMenuItem.Text = "&Help"
        '
        'AboutToolStripMenuItem
        '
        Me.AboutToolStripMenuItem.Name = "AboutToolStripMenuItem"
        Me.AboutToolStripMenuItem.Size = New System.Drawing.Size(116, 22)
        Me.AboutToolStripMenuItem.Text = "&About..."
        '
        'StatusStrip
        '
        Me.StatusStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.lSizeRatio})
        Me.StatusStrip.Location = New System.Drawing.Point(0, 427)
        Me.StatusStrip.Name = "StatusStrip"
        Me.StatusStrip.Size = New System.Drawing.Size(562, 22)
        Me.StatusStrip.TabIndex = 2
        Me.StatusStrip.Text = "..."
        '
        'lSizeRatio
        '
        Me.lSizeRatio.BackColor = System.Drawing.SystemColors.Control
        Me.lSizeRatio.BorderStyle = System.Windows.Forms.Border3DStyle.Etched
        Me.lSizeRatio.Name = "lSizeRatio"
        Me.lSizeRatio.Size = New System.Drawing.Size(16, 17)
        Me.lSizeRatio.Text = "..."
        '
        'NoneToolStripMenuItem
        '
        Me.NoneToolStripMenuItem.Name = "NoneToolStripMenuItem"
        Me.NoneToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.NoneToolStripMenuItem.Text = "&None"
        '
        'LowQualityToolStripMenuItem
        '
        Me.LowQualityToolStripMenuItem.Name = "LowQualityToolStripMenuItem"
        Me.LowQualityToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.LowQualityToolStripMenuItem.Text = "Low Quality"
        '
        'BilinearToolStripMenuItem
        '
        Me.BilinearToolStripMenuItem.Name = "BilinearToolStripMenuItem"
        Me.BilinearToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.BilinearToolStripMenuItem.Text = "Bilinear"
        '
        'BicubicToolStripMenuItem
        '
        Me.BicubicToolStripMenuItem.Name = "BicubicToolStripMenuItem"
        Me.BicubicToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.BicubicToolStripMenuItem.Text = "Bicubic"
        '
        'FrmTiScreen
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.ControlDarkDark
        Me.ClientSize = New System.Drawing.Size(562, 449)
        Me.Controls.Add(Me.StatusStrip)
        Me.Controls.Add(Me.PicBox)
        Me.Controls.Add(Me.MenuStrip)
        Me.Name = "FrmTiScreen"
        Me.Text = "FakeTI99"
        CType(Me.PicBox, System.ComponentModel.ISupportInitialize).EndInit()
        Me.MenuStrip.ResumeLayout(False)
        Me.MenuStrip.PerformLayout()
        Me.StatusStrip.ResumeLayout(False)
        Me.StatusStrip.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents PicBox As PictureBox
    Friend WithEvents folderBrowser As FolderBrowserDialog
    Friend WithEvents MenuStrip As MenuStrip
    Friend WithEvents FileToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ExitToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents EditToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents CutToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents HelpToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents AboutToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents StatusStrip As StatusStrip
    Friend WithEvents lSizeRatio As ToolStripStatusLabel
    Friend WithEvents X1ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents X2ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents X3ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents FilterToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SettingsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents NoneToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LowQualityToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents BilinearToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents BicubicToolStripMenuItem As ToolStripMenuItem
End Class
