Public Class Operatr
    Protected ReadOnly chdr As String = "Operatr"

    'Public Shared err As TiBasic.ErrorId
    Public idStr As String
    Public isUnary As Boolean ' necessita sempre di un solo operando
    Public isBinary As Boolean ' necessita sempre di due operandi
    Public isNumeric As Boolean ' operatore numerico
    Public isString As Boolean ' operatore di stringhe (non numerico)
    Public priority As Integer
    Delegate Function Fnct(a1 As Variable, a2 As Variable) As Variable
    Public executor As Fnct
    Public Shared typeMistmach_ErrCode, missingOperand_ErrCode As Integer
    Public Shared redundantOperand_ErrCode As Integer

    Sub New(ByRef id As String, ByRef precedence As Integer, ByRef executor As Fnct)
        Me.idStr = id
        Me.isUnary = False
        Me.isBinary = True
        Me.isNumeric = True
        Me.isString = True
        Me.priority = precedence
        Me.executor = executor
    End Sub

    Sub New(ByRef id As String,
            ByRef precedence As Integer,
            ByRef isUnary As Boolean,
            ByRef isBinary As Boolean,
            ByRef isNumeric As Boolean,
            ByRef isString As Boolean,
            ByRef executor As Fnct)
        Me.idStr = id
        Me.isUnary = isUnary
        Me.isBinary = isBinary
        Me.isNumeric = isNumeric
        Me.isString = isString
        Me.priority = precedence
        Me.executor = executor
    End Sub

    Public Function execute(v1 As Variable, v2 As Variable) As Variable
        Dim op1avail, op2avail As Boolean
        op1avail = (v1 IsNot Nothing)
        op2avail = (v2 IsNot Nothing)

        If op1avail Then
            If Not (isNumeric And v1.isNumeric()) Then Return New Variable(VarType.ERR, typeMistmach_ErrCode)
        End If
        If op2avail Then
            If Not (isNumeric And v2.isNumeric()) Then Return New Variable(VarType.ERR, typeMistmach_ErrCode)
        End If

        If isBinary Then
            If Not (op1avail And op2avail) Then Return New Variable(VarType.ERR, missingOperand_ErrCode)
        ElseIf isUnary Then
            If Not (op1avail Or op2avail) Then Return New Variable(VarType.ERR, missingOperand_ErrCode)
            If op1avail And op2avail Then Return New Variable(VarType.ERR, redundantOperand_ErrCode)
        End If

        Return executor(v1, v2)
    End Function
End Class
