Public Enum VarType
    UNDEF
    FLOAT
    STRNG
    ERR
End Enum

Public Class Variable
    Public Const MAX_DIMENSIONS As Integer = 3
    Protected Const chdr As String = "Variable"
    Protected Const noNameStr As String = "(noname)"
    Public Const floatFormat As String = " #.0######### ;-#.0######### "
    Public Const expFormat As String = " 0.0#####E+00 ;-0.0#####E-## "
    Public Const integerFormat As String = " # ;-# "

    Public name As String
    Public type As VarType
    Public value As Object

    ' array attributes
    Protected dimensions As Byte
    'Protected address As Integer()
    Protected dimTop As Integer()
    Protected baseIdx As Byte ' 0 or 1
    Protected arrayValue As Object()
    'Protected realIdx As Integer ' puntatore a elemento corrente dell'array

    Public Shared ReadOnly Zero As New Variable(VarType.FLOAT, 0.0)
    Public Shared ReadOnly EmptyString As New Variable(VarType.STRNG, String.Empty)

    Sub New()
        CreateSimple(noNameStr, VarType.UNDEF, Nothing)
    End Sub

    Sub New(type As VarType)
        CreateSimple(noNameStr, type, Nothing)
    End Sub

    Sub New(value As String)
        CreateSimple(noNameStr, VarType.STRNG, value)
    End Sub

    Sub New(value As Double)
        CreateSimple(noNameStr, VarType.FLOAT, value)
    End Sub

    Sub New(name As String, type As VarType)
        CreateSimple(name, type, Nothing)
    End Sub

    Sub New(name As String, type As VarType, value As Object)
        CreateSimple(name, type, value)
    End Sub

    Sub New(type As VarType, value As Object)
        CreateSimple(noNameStr, type, value)
    End Sub

    Sub New(name As String, str As String)
        CreateSimple(name, VarType.STRNG, str)
    End Sub

    Sub New(name As String, value As Single)
        CreateSimple(name, VarType.FLOAT, value)
    End Sub

    ' crea array
    Sub New(arrayName As String, arrayType As VarType, ByRef arrayDimTops As Integer(), ByRef optBase0 As Boolean)
        CreateArray(arrayName, arrayType, arrayDimTops, optBase0)
    End Sub

    ' crea array di default di dimensioni specificate e ampiezza specificata per ogni dimensione
    Sub New(ByRef arrayName As String,
            ByRef arrayType As VarType,
            ByRef arrayDimensions As Byte,
            ByRef dimensionWidth As Integer,
            ByRef optBase0 As Boolean)
        If arrayDimensions = 0 Then ' simple variable case
            Warn(chdr & "New", "degenere case of array of dimension 0: creating simple variable " & arrayName)
            CreateSimple(arrayName, arrayType, Nothing)
            Return
        End If
        If arrayDimensions > 3 Then arrayDimensions = 3
        Dim topIdxs(arrayDimensions - 1) As Integer
        For i As Integer = 0 To arrayDimensions - 1
            topIdxs(i) = dimensionWidth
        Next
        CreateArray(arrayName, arrayType, topIdxs, optBase0)
    End Sub

    Public Sub CopyContentOf(ByRef from As Variable)
        'If dimensions > 0 Then Return
        type = from.type
        value = from.value
    End Sub

    ' crea una variabile semplice con medesimo contenuto (tipo e valore)
    Public Function CloneSimpleVarContent() As Variable
        Return New Variable(String.Empty, type, value)
    End Function

    Protected Sub CreateSimple(ByRef name As String, ByRef type As VarType, ByRef value As Object)
        Me.name = name
        Me.type = type
        If value Is Nothing Then
            Select Case type
                Case VarType.FLOAT
                    Me.value = 0.0
                Case VarType.STRNG
                    Me.value = String.Empty
            End Select
        Else
            Me.value = value
        End If
        dimensions = 0
    End Sub

    Protected Sub CreateArray(arrayName As String,
                         arrayType As VarType,
                         ByRef arrayDimTops As Integer(),
                         ByRef optBase0 As Boolean)

        ' crea variabile semplice di supporto
        CreateSimple(arrayName, arrayType, Nothing)

        ' crea variabile array
        dimensions = arrayDimTops.Length
        If optBase0 Then baseIdx = 0 Else baseIdx = 1
        If dimensions > 3 Then dimensions = 3
        ReDim dimTop(dimensions - 1)
        Array.Copy(arrayDimTops, dimTop, dimensions)
        Dim ln As Integer = GetArrayLength()
        ReDim arrayValue(ln - 1)
        If arrayType = VarType.FLOAT Then
            For i As Integer = 0 To ln - 1
                arrayValue(i) = 0.0
            Next
        Else
            For i As Integer = 0 To ln - 1
                arrayValue(i) = String.Empty
            Next
        End If
        'realIdx = 0
        value = arrayValue(0)
    End Sub

    ' for arrays only
    Public Function GetArrayLength() As Integer
        If dimensions = 0 Then Return 0
        Dim d As Integer = 1
        For i As Integer = 0 To dimensions - 1
            If baseIdx = 0 Then d *= (dimTop(i) + 1) Else d *= dimTop(i)
        Next
        Return d
    End Function

    ' for arrays only
    Protected Function GetAbsoluteIndex(ByRef idx As Integer()) As Integer
        If dimensions = 0 Or idx.Length < dimensions Then Return -1
        For i As Integer = 0 To dimensions - 1
            If idx(i) < baseIdx Or idx(i) > dimTop(i) Then Return -1
        Next
        Select Case dimensions
            Case 1
                Return idx(0) - baseIdx
            Case 2
                Return (idx(0) - baseIdx) * dimTop(0) + (idx(1) - baseIdx)
            Case 3
                Return (idx(0) - baseIdx) * dimTop(0) * dimTop(1) + (idx(1) - baseIdx) * dimTop(1) + (idx(2) - baseIdx)
            Case Else
                Return -1
        End Select
    End Function

    Public Function GetDimensions() As Integer
        Return dimensions
    End Function

    Public Function GetDimensionSizes() As Integer()
        Return dimTop
    End Function

    Public Function GetVarType() As VarType
        Return type
    End Function

    Public Function IsBase0() As Boolean
        Return (baseIdx = 0)
    End Function

    ' for arrays only
    Public Function ValidArrayIndexes(ByRef idx As Integer()) As Boolean
        If dimensions = 0 Or idx.Length < dimensions Then Return False
        For i As Integer = 0 To dimensions - 1
            If idx(i) < baseIdx Or idx(i) > dimTop(i) Then Return False
        Next
        Return True
    End Function

    Public Function GetArrayVar(ByRef idx As Integer()) As Variable
        If dimensions = 0 Then Return Nothing
        If idx.Length < dimensions Then Return Nothing
        Dim i As Integer = GetAbsoluteIndex(idx)
        If i = -1 Then Return Nothing
        Return New Variable(type, arrayValue(i))
    End Function

    ' for arrays only
    Public Function SetArrayVar(ByRef idx As Integer(), ByRef var As Variable) As Boolean
        If dimensions = 0 Then Return False
        If var.type <> type Then Return False
        If idx.Length < dimensions Then Return False
        Dim i As Integer = GetAbsoluteIndex(idx)
        If i = -1 Then Return False
        arrayValue(i) = var.value
        Return True
    End Function

    Public Function GetString() As String
        Return ValueToString()
    End Function

    Public Function GetDouble() As Double
        If type = VarType.FLOAT Then Return value
        Dim hdr As String = chdr & "GetDouble"
        Info(hdr, "call over a not-STRING type variable returns 0")
        Return 0.0
    End Function

    Public Function GetError() As Integer
        If type = VarType.ERR Then Return value
        Dim hdr As String = chdr & "GetError"
        Info(hdr, "call over a not-ERROR type variable returns 0")
        Return 0
    End Function

    Public Function GetInteger() As Integer
        Dim hdr As String = chdr & "GetInteger"
        If type = VarType.FLOAT Then
            Try
                Dim i As Integer = Convert.ToInt32(Math.Round(CType(value, Double)))
                Return i
            Catch ex As Exception
                Warn(hdr, "problem converting value " & value.ToString & " to integer")
            End Try
        End If
        Info(hdr, "call over a not-STRING type variable returns 0")
        Return 0
    End Function

    'imposta tipo e valore variabile semplice - non ha effetto su variabili array
    Public Sub SetSimpleVarContent(ByRef vt As VarType, ByRef val As Object)
        If dimensions > 0 Then Return ' array
        type = vt
        value = val
    End Sub

    Public Function ValueToString() As String
        Select Case type
            Case VarType.FLOAT
                Return getFloatVarString(CType(value, Double))
            Case VarType.STRNG
                Return CType(value, String)
            Case VarType.ERR
                Return CType(value, Integer)
            Case Else
                Return String.Empty
        End Select
    End Function

    Public Function isNumeric() As Boolean
        Return (type = VarType.FLOAT)
    End Function

    Public Function isString() As Boolean
        Return (type = VarType.STRNG)
    End Function

    Public Function isError() As Boolean
        Return (type = VarType.ERR)
    End Function

    Public Function isArray() As Boolean
        Return (dimensions > 0)
    End Function

    'Public Function FilterType(ByRef mandatoryType As Integer) as Variable
    '    If mandatoryType <> VarType.UNDEF And mandatoryType <> type Then Return Nothing
    '    Return Me
    'End Function

    Protected Function getFloatVarString(ByRef v As Double)
        If v = 0.0 Then Return " 0 "
        'Dim exp As Integer = Convert.ToInt32(Math.Floor(Math.Log10(v)))
        Dim expf As Double = Math.Log10(Math.Abs(v))
        expf = Math.Floor(expf)
        Dim exp As Integer = Convert.ToInt32(expf)
        If exp >= -10 And exp < 10 Then ' normal notation
            If v = Int(v) Then ' integer
                Return Format(v, integerFormat)
            Else ' not integer
                Return Format(v, floatFormat).Replace(",", ".")
            End If
        End If
        ' scientific notation
        Dim str As String = Format(v, expFormat).Replace(",", ".")
        Dim i As Integer = str.IndexOf("E"c)
        If exp < -99 Then
            str = str.Substring(0, i + 1) & "-**"
        ElseIf exp > 99 Then
            str = str.Substring(0, i + 1) & "+**"
        End If
        Return str
    End Function

    Public Overrides Function ToString() As String
        ' simple variabile
        If dimensions = 0 Then
            If type = VarType.STRNG Then
                Return name & " = " & Chr(34) & ValueToString() & Chr(34)
            Else
                Return name & " =" & ValueToString()
            End If
        End If

        ' array
        Dim str As String = name & "("
        For i As Integer = 1 To dimensions
            str &= dimTop(i - 1).ToString()
            If i < dimensions Then str &= ","
        Next
        str &= ") base " & baseIdx.ToString
        Return str
    End Function

End Class
