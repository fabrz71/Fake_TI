Public Class VarTable
    Protected table As New List(Of Variable)

    Public Sub Clear()
        table.Clear()
    End Sub

    Public Function Contains(ByRef name As String) As Boolean
        Return GetVariable(name) IsNot Nothing
    End Function

    Public Function IsArray(ByRef name As String) As Boolean
        Dim v As Variable = GetVariable(name)
        If v Is Nothing Then Return False
        Return v.isArray
    End Function

    Public Function GetVariable(ByRef name As String) As Variable
        For Each v As Variable In table
            If v.name.Equals(name) Then Return v
        Next
        Return Nothing
    End Function

    Public Function Add(ByRef name As String, ByRef type As VarType) As Variable
        If Contains(name) Then Return Nothing
        Dim v As New Variable(name, type)
        table.Add(v)
        Return v
    End Function

    Public Function Add(ByRef name As String, ByRef val As Double) As Variable
        If Contains(name) Then Return Nothing
        Dim v As New Variable(name, VarType.FLOAT, val)
        table.Add(v)
        Return v
    End Function

    Public Function Add(ByRef name As String, ByRef val As Integer) As Variable
        If Contains(name) Then Return Nothing
        Dim v As New Variable(name, VarType.FLOAT, Convert.ToDouble(val))
        table.Add(v)
        Return v
    End Function

    Public Function Add(ByRef name As String, ByRef val As String) As Variable
        If Contains(name) Then Return Nothing
        Dim v As New Variable(name, VarType.STRNG, val)
        table.Add(v)
        Return v
    End Function

    Public Function Add(ByRef var As Variable) As Variable
        If Contains(var.name) Then Return Nothing
        table.Add(var)
        Return var
    End Function

    Public Function AddArray(ByRef arrayName As String,
                             ByRef arrayType As VarType,
                             ByRef arrayDimTops As Integer(),
                             ByRef optBase0 As Boolean) As Variable
        If Contains(arrayName) Then Return Nothing
        Dim arr As New Variable(arrayName, arrayType, arrayDimTops, optBase0)
        table.Add(arr)
        Return arr
    End Function

    Public Function AddArray(ByRef arrayName As String,
                             ByRef arrayType As VarType,
                             ByRef arrayDimensions As Byte,
                             ByRef arrayDimWidth As Integer,
                             ByRef optBase0 As Boolean) As Variable

        If Contains(arrayName) Then Return Nothing
        Dim arr As New Variable(arrayName, arrayType, arrayDimensions, arrayDimWidth, optBase0)
        table.Add(arr)
        Return arr
    End Function

    Public Function SetValue(ByRef name As String, ByRef val As Double, Optional createNew As Boolean = False) As Boolean
        Dim v As Variable = GetVariable(name)
        If String.IsNullOrEmpty(v.name) Then ' variabile non definita
            If createNew Then
                v = New Variable(name, VarType.FLOAT, val)
                Return True
            Else
                Return False
            End If
        End If
        If v.type <> VarType.FLOAT Then Return False
        v.value = val
        Return True
    End Function

    Public Function SetValue(ByRef name As String, ByRef val As Integer, Optional createNew As Boolean = False) As Boolean
        Dim v As Variable = GetVariable(name)
        If v Is Nothing Then ' variabile non definita
            If createNew Then
                v = New Variable(name, val)
                Return True
            Else
                Return False
            End If
        End If
        If v.type <> VarType.FLOAT Then Return False
        v.value = val
        Return True
    End Function

    Public Function SetValue(ByRef name As String, ByRef val As String, Optional createNew As Boolean = False) As Boolean
        Dim v As Variable = GetVariable(name)
        If String.IsNullOrEmpty(v.name) Then ' variabile non definita
            If createNew Then
                v = New Variable(name, val)
                Return True
            Else
                Return False
            End If
        End If
        If v.type <> VarType.STRNG Then Return False
        v.value = val
        Return True
    End Function

    Public Function GetValue(ByRef name As String) As Object
        Dim v As Variable = GetVariable(name)
        If String.IsNullOrEmpty(v.name) Then Return Nothing
        Return v.value
    End Function

    Public Function GetDecimal(ByRef name As String) As Double
        Dim v As Variable = GetVariable(name)
        If String.IsNullOrEmpty(v.name) Then Return Nothing
        If v.type <> VarType.FLOAT Then Return Nothing
        Return Convert.ToDouble(v.value)
    End Function

    'Public Function GetInteger(name As String) As Integer
    '    Dim v As Variable = GetVar(name)
    '    If String.IsNullOrEmpty(v.name) Then Return Nothing
    '    If v.type <> VarType.INTGR Then Return Nothing
    '    Return Convert.ToInt32(v.value)
    'End Function

    Public Function GetString(ByRef name As String) As String
        Dim v As Variable = GetVariable(name)
        If String.IsNullOrEmpty(v.name) Then Return Nothing
        If v.type <> VarType.STRNG Then Return Nothing
        Return Convert.ToString(v.value)
    End Function

    Public Function GetDescrList() As List(Of String)
        Dim vlist As New List(Of String)
        For Each v As Variable In table
            vlist.Add(v.ToString())
        Next
        Return vlist
    End Function

    Public Function Count() As Integer
        Return table.Count()
    End Function

End Class
