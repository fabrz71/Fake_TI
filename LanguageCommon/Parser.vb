Public Structure Interval
    Dim startp As Integer
    Dim endp As Integer

    Sub New(p1 As Integer, p2 As Integer)
        startp = p1
        endp = p2
    End Sub
End Structure

Public Structure HeaderAndArguments
    Dim header As String
    Dim joinedArguments As String
    Dim argumentsInBrackets As Boolean
    Dim onlyNumericArguments As Boolean
    Dim argn As Integer
    Dim args As String()
End Structure

Module Parser
    ''' <summary>
    ''' Restituisce estremi di intervallo descritto da stringa "A-B"
    ''' dove A e B sono interi positivi ordinati.
    ''' Sia A che B possono essere omessi ma non contemporaneamente.
    ''' Puo' comparire solo A senza "-", per un intervallo degenere (A=B).
    ''' (funzione utile per comando LIST)
    ''' </summary>
    ''' <param name="s"></param>
    ''' <returns>Intervallo descritto dalla stringa.
    ''' 0 quando il valore non è definito. -1 se A non è convertibile in numero.</returns>
    Function GetInterval(ByRef s As String) As Interval
        Dim intrv As New Interval(0, 0)
        If Not String.IsNullOrEmpty(s) Then
            Dim i As Integer = s.IndexOf("-")
            If i = -1 Then ' trattino non riporato
                Try
                    intrv.startp = Convert.ToInt32(s)
                    intrv.endp = intrv.startp
                Catch ex As Exception
                    intrv.startp = -1
                End Try
            Else ' trattino presente
                If i > 0 Then ' A e' definito
                    Try
                        intrv.startp = Convert.ToInt32(s.Substring(0, i))
                    Catch ex As Exception
                        intrv.startp = -1
                    End Try
                End If
                If i < s.Length - 1 Then ' B e' definito
                    Try
                        intrv.endp = Convert.ToInt32(s.Substring(i + 1))
                    Catch ex As Exception
                        intrv.endp = intrv.startp
                    End Try
                End If
            End If
        End If
        Return intrv
    End Function

    ' estrae argomenti numerici interi da una stringa in formato "n1<sep>n2<sep>n3..."
    ' dove <sep> e' il separatore - li restituisce come array di interi
    Public Function GetNumericParams(ByRef paramStr As String, ByRef separator As String) As Integer()
        Dim params As String() = paramStr.Split(separator)
        Dim n As Integer = params.Length
        Dim args(n - 1) As Integer
        For i As Integer = 0 To n - 1
            Try
                args(i) = Convert.ToInt32(params(i))
            Catch ex As Exception
                Return Nothing
            End Try
        Next
        Return args
    End Function

    ' estrae argomenti numerici interi da una struttura HeaderAndArguments
    Public Function GetNumericParams(ByRef parts As HeaderAndArguments) As Integer()
        If parts.argn <= 0 Then Return Nothing
        Dim values(parts.argn) As Integer
        For i As Integer = 0 To parts.argn - 1
            Try
                values(i) = Convert.ToInt32(parts.args(i))
            Catch ex As Exception
                values(i) = 0
            End Try
        Next
        Return values
    End Function


    ' restituisce posizione nella stringa della prima parentesi chiusa piu' esterna,
    ' a partire da sx verso dx
    ' restituisce -1 in caso di difetto
    Public Function SearchClosingBracket(ByRef str As String, Optional ByRef startFromIdx As Integer = 0) As Integer
        Dim dpth As Integer = 0
        Dim ch As Char
        For i As Integer = startFromIdx To str.Length - 1
            ch = str.Chars(i)
            If ch = "("c Then
                dpth += 1
            ElseIf ch = ")"c Then
                dpth -= 1
                If dpth = 0 Then Return i
            End If
        Next
        Return -1
    End Function

    ' preleva da una stringa nome header ed eventuali argomenti separati da <separator>
    ' gli argomenti possono essere chiusi tra tonde. Se non definito, il separatore di default e' ",".
    ' Il risultato viene restituito da un oggetto HeaderAndArguments.
    Public Function GetHeaderAndArguments(ByVal str As String,
                                          Optional ByRef separator As String = ",") As HeaderAndArguments
        Dim parts As HeaderAndArguments
        If String.IsNullOrEmpty(str) Then Return parts
        str = str.Trim()
        Dim i As Integer = str.IndexOf("(")
        With parts
            .argumentsInBrackets = (i >= 0)
            If .argumentsInBrackets Then
                If i = 0 Then .header = String.Empty
            Else ' senza parentesi
                i = str.IndexOf(" ")
                If i = -1 Then
                    .header = New String(str)
                    .args = Nothing
                    .argn = 0
                    Return parts
                End If
            End If

            .header = str.Substring(0, i).Trim()
            .joinedArguments = str.Substring(i + 1).Trim()
            If .argumentsInBrackets Then
                i = .joinedArguments.LastIndexOf(")")
                If i > 0 Then .joinedArguments = .joinedArguments.Substring(0, i)
            End If
            .args = .joinedArguments.Split(separator, StringSplitOptions.TrimEntries)
            .argn = .args.Length

            .onlyNumericArguments = True
            For i = 0 To .argn - 1
                If Not IsNumeric(.args(i)) Then
                    .onlyNumericArguments = False
                    Exit For
                End If
            Next
        End With
        Return parts
    End Function


    ' estrae argomenti da stringa di espressione nel formato "[A1] OP [A2]" (anche senza spazi) dove
    ' A1 e A2 sono argomenti (espressioni) e OP e' l'operatore unario o binario
    ' restituisce gli argomenti
    ' String(0) = A1 (puo' essere vuoto se non lo e' A2)
    ' String(1) = A2 (puo' essere vuoto se non lo e' A1)
    ' restituisce nothing in caso di stringhe nulle o argomenti mancanti
    Public Function GetOperatorArguments(ByRef exprStr As String, ByRef opStr As String) As String()
        If String.IsNullOrEmpty(exprStr) Or String.IsNullOrEmpty(opStr) Then Return Nothing
        If exprStr.Length = opStr.Length Then Return Nothing
        Dim ln As Integer = exprStr.Length - 1
        Dim i As Integer = exprStr.IndexOf(opStr)
        If i = -1 Then Return Nothing
        Dim result(2)
        result(0) = exprStr.Substring(0, i).Trim()
        result(1) = exprStr.Substring(i + ln).Trim()
        Return result
    End Function

    Public Function ExtractStringConstant(ByRef expr As String) As Variable
        If expr Is Nothing Then Return Nothing
        Dim i1, i2, ln As Integer
        ln = expr.Length
        i1 = expr.IndexOf(Chr(34))
        i2 = expr.LastIndexOf(Chr(34))
        Dim vres As Variable
        If i1 = 0 And i2 = ln - 1 Then ' apici proprio all'inizio e alla fine
            If ln = 2 Then
                vres = New Variable(VarType.STRNG)
            Else
                vres = New Variable(VarType.STRNG, expr.Substring(1, ln - 2))
            End If
            Return vres
        End If
        Return Nothing
    End Function

    Public Function ExtractNumericConstant(ByRef expr As String) As Variable
        If expr Is Nothing Then Return Nothing
        Dim mExpr As String = expr.Replace(".", ",")
        If IsNumeric(mExpr) Then
            Dim vres As New Variable(VarType.FLOAT)
            Try
                vres.value = Convert.ToDouble(mExpr)
            Catch ex As Exception
                Return Nothing
            End Try
            Return vres
        End If
        Return Nothing
    End Function

    ' increments idx
    Public Function ExtractUIntegerFromString(ByRef str As String, ByRef idx As Integer) As UInt32
        If String.IsNullOrEmpty(str) Then Return 0
        Dim ch As Char
        Dim l As Integer = str.Length
        Dim nStr As String = ""
        Dim res As UInt32
        If idx >= str.Length Then Return 0

        While str.Chars(idx) = " "c
            idx += 1
        End While

        ch = str.Chars(idx)
        While ch >= "0"c And ch <= "9"c
            nStr &= ch
            idx += 1
            If idx = l Then
                ch = " "c
                Exit While
            End If
            ch = str.Chars(idx)
        End While
        If ch <> " " Then Return 0 ' number right-delimitation not correct

        Try
            res = Convert.ToUInt32(nStr)
        Catch ex As Exception
            Warn(mhdr & ".ExtractUIntegerFromString", ex.Message)
            res = 0
        End Try

        Return res
    End Function

    Public Function GetNumericStringLength(ByRef str As String, ByVal idx As Integer) As Integer
        If String.IsNullOrEmpty(str) Then Return 0
        Dim ch As Char
        Dim startp As Integer = idx
        Dim dot As Boolean = False
        Dim ex As Boolean = False
        Dim dgt As Boolean = False
        Dim l As Integer = str.Length
        If idx >= str.Length Then Return 0

        ch = str.Chars(idx)
        idx += 1
        While ch = " "c
            ch = str.Chars(idx)
            idx += 1
            If idx = l Then Return 0
        End While
        If ch = "+"c Or ch = "-"c Then
            ch = str.Chars(idx)
            idx += 1
            If idx = l Then Return 0
        End If

        Do
            If ch = "."c Then
                If dot Or ex Then Return 0 Else dot = True
            ElseIf ch = "E"c Then
                If ex Or (Not dgt) Then Return 0 Else ex = True
            ElseIf ch >= "0"c And ch <= "9"c Then
                If Not ex Then dgt = True
            ElseIf ch = " "c Then
                idx -= 1
                Exit Do
            Else
                Return 0
            End If
            If idx = l Then Exit Do
            ch = str.Chars(idx)
            idx += 1
        Loop

        If Not dgt Then Return 0
        Return idx - startp
    End Function

    ' Converte array di variabili in array di valori interi.
    ' Se almeno una variabile dell'array non e' numerica, restituisce nothing.
    Public Function GetIntegerArrayFromVars(ByRef vars As Variable()) As Integer()
        If vars Is Nothing Then Return Nothing
        Dim l As Integer = vars.Length
        If l = 0 Then Return Nothing
        Dim intVal(l - 1) As Integer
        For i As Integer = 0 To l - 1
            If Not vars(i).isNumeric() Then
                'SetError(ErrorId.STR_NUM_MISMATCH)
                Return Nothing
            End If
            intVal(i) = vars(i).GetInteger()
        Next
        Return intVal
    End Function
End Module