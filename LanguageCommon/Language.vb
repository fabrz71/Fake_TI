Public MustInherit Class Language
    Inherits SSS

    Public code As Code
    Public variables As VarTable
    Public running As Boolean
    Public breakState As Boolean
    Protected Const chdr As String = "Language"
    Protected maxToken As Integer
    Public typeSet As New List(Of Integer)
    Public cmdSet As FunctionCollection
    Public operSet As New OperatorSet()
    Public spaceSeparators As Boolean
    'Protected machine As TI99
    Public errMsgList As New Dictionary(Of Integer, String)
    Public warnMsgList As New Dictionary(Of Integer, String)
    Public errorCode As Integer
    Public warningCode As Integer
    'Public errorMsg As String

    Sub New(ByRef m As TI99)
        MyBase.New(m)
        'machine = m
        running = False
        breakState = False
        code = New Code()
        variables = New VarTable()
    End Sub

    'Public MustOverride Sub Init()
    'Public MustOverride Sub EndActivity()

    ' restituisce stringa di risposta
    Public MustOverride Function ReadInputLine(inputText As String) As String
    Public MustOverride Function ExecCommand(func As String) As String
    Public MustOverride Function ExecCode(func As String) As String
    Public MustOverride Function EvalExpression(ByRef expr As String,
                                                Optional expectedReturnType As VarType = 0) As Variable
    Public MustOverride Function GetErrorMessage(Optional ByRef optionalFinalArg As String = Nothing) As String
    Public MustOverride Function GetWarningMessage(Optional ByRef optionalFinalArg As String = Nothing) As String
    'Public MustOverride Function GetResultTypeFromOperation(v1 As Variable, v2 As Variable) As Integer
End Class
