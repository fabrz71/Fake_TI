Public Class Funct

    Public keyword As String
    Public token As Integer
    Public mandatoryArguments As Boolean
    'Public isCommand As Boolean
    'Public isIstruction As Boolean ' runtime program istruction
    'Public canDivert As Boolean ' only for program instructions
    'Public expectedArgumentsTypeId As Integer
    'Public minArgsCount As Integer
    'Public executor As Func(Of String, String) 
    Public Delegate Function Fnct(params As String) As String ' INPUT: parameters  OUTPUT: final message
    Public executor As Fnct

    <Flags>
    Enum Feature
        COMMAND = 1
        ISTRUCTION = 2
        CANDIVERT = 128
        ALL = 255
    End Enum
    Private _Features As Byte

    Public Property IsCommand As Boolean
        Get
            If (_Features And Feature.COMMAND) > 0 Then Return True
            Return False
        End Get
        Private Set
            If Value Then
                _Features = _Features Or Feature.COMMAND
            Else
                _Features = _Features And (Feature.ALL - Feature.COMMAND)
            End If
        End Set
    End Property

    Public Property IsIstruction As Boolean
        Get
            If (_Features And Feature.ISTRUCTION) > 0 Then Return True
            Return False
        End Get
        Private Set
            If Value Then
                _Features = _Features Or Feature.ISTRUCTION
            Else
                _Features = _Features And (Feature.ALL - Feature.ISTRUCTION)
            End If
        End Set
    End Property

    Public Property CanDivert As Boolean
        Get
            If (_Features And Feature.CANDIVERT) > 0 Then Return True
            Return False
        End Get
        Private Set
            If Value Then
                _Features = _Features Or Feature.CANDIVERT
            Else
                _Features = _Features And (Feature.ALL - Feature.CANDIVERT)
            End If
        End Set
    End Property

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

    Public Function IsFunction() As Boolean
        Return Not (isCommand Or isIstruction)
    End Function

End Class

