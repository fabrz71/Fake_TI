Public Structure OperationParseResult
    Dim op As Operatr
    Dim idx As Integer
    Dim lArg As String
    Dim rArg As String
End Structure

Public Class OperatorSet
    Protected opSet As New List(Of Operatr)

    Public Function Contains(idStr As String) As Boolean
        For Each op As Operatr In opSet
            If op Is Nothing Then Exit For
            If op.idStr.Equals(idStr) Then Return True
        Next
        Return False
    End Function

    Public Function AddOperator(ByRef idStr As String,
                        priority As Integer,
                        unary As Boolean,
                        binary As Boolean,
                        numericOp As Boolean,
                        stringOp As Boolean,
                        executor As Operatr.Fnct) As Boolean

        If Contains(idStr) Then Return False
        Dim op As New Operatr(idStr, priority, unary, binary, numericOp, stringOp, executor)
        opSet.Add(op)
        Return True
    End Function

    Public Function AddBinaryNumericOperator(ByRef idStr As String, priority As Integer, executor As Operatr.Fnct) As Boolean
        Return AddOperator(idStr, priority, False, True, True, False, executor)
    End Function

    Public Function AddBinaryStringOperator(ByRef idStr As String, priority As Integer, executor As Operatr.Fnct) As Boolean
        Return AddOperator(idStr, priority, False, True, False, True, executor)
    End Function

    Public Function AddBinaryOperator(ByRef idStr As String, priority As Integer, executor As Operatr.Fnct) As Boolean
        Return AddOperator(idStr, priority, False, True, True, True, executor)
    End Function

    Public Function AddUnaryOperator(ByRef idStr As String, priority As Integer, executor As Operatr.Fnct) As Boolean
        Return AddOperator(idStr, priority, True, False, True, True, executor)
    End Function

    '' preleva simbolo dell'operatore individuato nella stringa
    '' preleva gli argomenti di funzione (dx e sx)
    '' restituisce le parti prelevate in un array di stringhe di dimensione 3
    '' String(0) = simbolo operatore
    '' String(1) = argomento sx
    '' String(2) = argomento dx
    '' restituisce nothing in caso di errori sintattici o stringa vuota
    'Public Function ExtractOperatorAndArguments(ByRef exprStr As String) As OperationParseResult
    '    Dim op As Operatr = Nothing
    '    Dim found As Boolean = False
    '    Dim i As Integer
    '    Dim result As OperationParseResult

    '    result.op = Nothing
    '    For Each op In opSet
    '        i = exprStr.IndexOf(op.idStr) ' ricerca eventuale posizione dell'operatore nell'espressione
    '        If i >= 0 Then
    '            found = True
    '            Exit For
    '        End If
    '    Next

    '    If Not found Then Return Nothing
    '    result.op = op
    '    result.lArg = exprStr.Substring(0, i).Trim() ' potrebbe essere stringa vuota
    '    If i < exprStr.Length Then result.rArg = exprStr.Substring(i + 1).Trim() Else result.rArg = String.Empty
    '    Return result
    'End Function

    ' preleva simbolo dell'operatore individuato nella stringa
    ' preleva gli argomenti di funzione (dx e sx)
    ' restituisce le parti prelevate in un array di stringhe di dimensione 3
    ' String(0) = simbolo operatore
    ' String(1) = argomento sx
    ' String(2) = argomento dx
    ' restituisce nothing in caso di errori sintattici o stringa vuota
    Public Function ExtractOperatorAndArguments(ByRef exprStr As String) As OperationParseResult
        Dim op As Operatr = Nothing
        Dim opr As OperationParseResult
        Dim found As Boolean = False
        Dim i As Integer = 0
        Dim result As New OperationParseResult With
            {.op = Nothing, .idx = -1, .lArg = String.Empty, .rArg = String.Empty}
        Dim priority As Integer = 100

        Do
            opr = FindNextOperator(exprStr, i, priority)
            With opr
                If .idx < 0 Then Exit Do ' non trovato
                If .op.priority < priority Then
                    result.op = .op
                    result.idx = .idx
                    priority = .op.priority
                End If
                i = .idx + 1
            End With
        Loop Until i >= exprStr.Length

        With result
            If .op IsNot Nothing Then
                If .idx > 0 Then
                    .lArg = exprStr.Substring(0, .idx).Trim()
                Else
                    .lArg = String.Empty
                End If
                Dim opLn As Integer = .op.idStr.Length()
                If .idx + opLn < exprStr.Length Then
                    .rArg = exprStr.Substring(.idx + opLn).Trim()
                Else
                    .rArg = String.Empty
                End If
            End If
        End With

        Return result
    End Function

    ' ricerca primo operatore da elaborare da sx a dx nell'espressione data
    ' che abbia priorita' minore di <prevOperatorPriority>
    Public Function FindNextOperator(ByRef exprStr As String,
                                 Optional ByRef startIdx As Integer = 0,
                                 Optional ByRef prevOperatorPriority As Integer = -1) As OperationParseResult
        Dim op As Operatr
        Dim i As Integer = startIdx
        Dim j As Integer
        Dim ch As Char
        Dim res As New OperationParseResult With
            {.op = Nothing, .idx = -1, .lArg = String.Empty, .rArg = String.Empty}

        Do
            ch = exprStr.Chars(i)
            ' ricerca apici
            If ch = """"c Then
                Try
                    j = exprStr.IndexOf(Chr(34), i + 1)
                Catch ex As Exception
                    Exit Do
                End Try
                If j = -1 Then Exit Do Else i = j
            ElseIf ch = "("c Then
                j = Parser.SearchClosingBracket(exprStr, i)
                If j = -1 Then Exit Do Else i = j
            Else
                op = ExtractOperatorAt(exprStr, i)
                If op IsNot Nothing Then
                    If op.priority < prevOperatorPriority Then
                        res.op = op
                        res.idx = i
                        Exit Do
                    End If
                    i += op.idStr.Length - 1
                End If
            End If
            i += 1
        Loop Until i >= exprStr.Length
        Return res
    End Function

    ' restituisce l'operatore presente nella stringa <str> nella posizione indicata da <pos>
    ' restituisce Nothing se l'operatore non è stato trovato
    Public Function ExtractOperatorAt(ByRef str As String, ByRef pos As Integer) As Operatr
        Dim resultOp As Operatr = Nothing
        Dim i As Integer
        For Each op In opSet
            With op.idStr
                For i = 0 To .Length - 1
                    If str.Chars(pos + i) <> .Chars(i) Then Exit For
                Next
                If i = .Length Then
                    resultOp = op
                    Exit For
                End If
            End With
        Next
        Return resultOp
    End Function
End Class
