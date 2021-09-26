Public Class Funct

    Public keyword As String
    Public token As Integer
    Public mandatoryArguments As Boolean
    Public isCommand As Boolean
    Public isIstruction As Boolean ' runtime program istruction
    Public canDivert As Boolean ' solo per istruzioni di programma
    'Public unencodedArguments As Boolean
    'Public expectedArgumentsTypeId As Integer
    'Public minArgsCount As Integer
    'Public executor As Func(Of String, String) ' INPUT: parameters  OUTPUT: final message
    Public Delegate Function Fnct(params As String) As String
    Public executor As Fnct

    Sub New(ByRef id As String,
            ByVal token As Integer,
            ByVal withArguments As Boolean,
            ByVal isCommand As Boolean,
            ByVal isIstruction As Boolean,
            ByVal canDivert As Boolean,
            ByRef executor As Fnct)
        'ByRef executor As Func(Of String, String))
        Me.keyword = id
        Me.token = token
        Me.mandatoryArguments = withArguments
        Me.isCommand = isCommand
        Me.isIstruction = isIstruction
        Me.canDivert = canDivert
        Me.executor = executor
    End Sub

End Class

