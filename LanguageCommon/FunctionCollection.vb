Public Class FunctionCollection
    Protected Const chdr As String = "FunctionCollection"
    Protected funcSet As New List(Of Funct)
    Protected jumpTable As Funct.Fnct() ' method delegate
    Protected maxToken As Integer
    Protected tokenEntries As Integer

    ' <topToken> deve essere > 0
    Public Sub New(topToken As Integer)
        If topToken <= 0 Then Throw New Exception("bad token limit in FunctionCollection instantiation")
        maxToken = topToken
        ReDim jumpTable(maxToken)
        For i As Integer = 0 To maxToken
            jumpTable(i) = Nothing
        Next
        tokenEntries = 0
    End Sub

    Public Function Add(ByRef keyword As String,
                                token As Integer,
                                withArguments As Boolean,
                                isCommand As Boolean,
                                isIstruction As Boolean,
                                canDivert As Boolean,
                                executor As Funct.Fnct) As Boolean 'executor As Func(Of String, String)) As Boolean
        Return Add(New Funct(keyword, token, withArguments, isCommand, isIstruction, canDivert, executor))
    End Function

    Public Function AddCommand(ByRef keyword As String,
                                token As Integer,
                           withArguments As Boolean,
                                isRunnable As Boolean,
                                executor As Funct.Fnct) As Boolean 'executor As Func(Of String, String)) As Boolean
        Return Add(New Funct(keyword, token, withArguments, True, isRunnable, False, executor))
    End Function

    Public Function AddIstruction(ByRef keyword As String,
                                token As Integer,
                            withArguments As Boolean,
                                isFlexible As Boolean,
                                canDivert As Boolean,
                                executor As Funct.Fnct) As Boolean 'executor As Func(Of String, String)) As Boolean
        Return Add(New Funct(keyword, token, withArguments, isFlexible, True, canDivert, executor))
    End Function

    Public Function AddFunction(ByRef keyword As String,
                                token As Integer,
                                withArguments As Boolean,
                                executor As Funct.Fnct) As Boolean 'executor As Func(Of String, String)) As Boolean
        Return Add(New Funct(keyword, token, withArguments, False, False, False, executor))
    End Function

    Public Function Add(ByRef f As Funct) As Boolean
        Dim hdr = chdr & ".Add"
        If f Is Nothing Then
            Outp(MsgType.WARNING, hdr, "void function")
            Return False
        End If
        With f
            If .token > maxToken Then
                Outp(MsgType.WARNING, hdr, "keyword '" & .keyword & "' with invalid token (value too big) - not added")
                Return False
            End If
            If ContainsToken(.token) Then
                Outp(MsgType.WARNING, hdr, "adding keyword '" & .keyword & "' with duplicated token (for '" &
                     GetKeywordByToken(.token) & "')")
                'Return False
            End If
            funcSet.Add(f)
            If jumpTable(.token) Is Nothing Then
                jumpTable(.token) = f.executor
                tokenEntries += 1
            Else
                Outp(MsgType.INFO, hdr, "function for keyword '" & .keyword & "' already defined - won't be modified")
            End If
        End With
        Return True
    End Function

    Public Function GetFunctionByToken(token As Integer) As Funct
        If token < 0 Or token > maxToken Then Return Nothing
        If jumpTable(token) Is Nothing Then Return Nothing
        For Each f As Funct In funcSet
            If f.token = token Then Return f
        Next
        Return Nothing
    End Function

    Public Function GetTokensCount() As Integer
        Return tokenEntries
    End Function

    Public Function GetTokenByKeyword(ByRef keyword As String) As Integer
        For Each f As Funct In funcSet
            If f.keyword = keyword Then Return f.token
        Next
        Return -1
    End Function

    ''' <summary>
    ''' Ricerca una qualiasi keyword nella stringa expr, nella posizione startIdx.
    ''' Se startIdx=0 la ricerca avverra' all'inizio della stringa.
    ''' </summary>
    ''' <param name="expr">stringa in cui eseguire la ricerca</param>
    ''' <param name="startIdx">posizione di ricerca della keyword all'interno della stringa</param>
    ''' <returns>La funzione corrispondente alla keyword individuata, Nothing altrimenti</returns>
    Public Function SearchKeywordAndGetFunction(ByRef expr As String,
                                                Optional ByRef startIdx As Integer = 0) As Funct
        Dim subExpr As String
        If startIdx > 0 Then subExpr = expr.Substring(startIdx) Else subExpr = expr.Clone()
        For Each f As Funct In funcSet
            If f.keyword.Length <= subExpr.Length Then
                If subExpr.IndexOf(f.keyword) = 0 Then Return f
            End If
        Next
        Return Nothing
    End Function

    Public Function GetKeywordByToken(token As Integer, Optional ByRef spacePadding As Boolean = False) As String
        For Each f As Funct In funcSet
            If f.token = token Then
                If spacePadding Then
                    If f.isIstruction Or f.isCommand Then
                        Return " " & f.keyword & " " ' command keyword or statement keyword
                    Else
                        Return f.keyword ' function keyword
                    End If
                Else
                    Return f.keyword
                End If
            End If
        Next
        Return String.Empty
    End Function

    Public Function Exec(token As Integer, ByRef args As String) As String
        If token < 0 Or token > maxToken Then
            Outp(MsgType.WARNING, chdr & ".Exec", "invalid token " & token.ToString)
            Return String.Empty
        End If
        Dim fn As Funct.Fnct = jumpTable(token)
        If fn Is Nothing Then
            Outp(MsgType.WARNING, chdr & ".Exec", "null executor for token " & token.ToString)
            Return String.Empty
        End If
        Return fn(args)
    End Function

    Public Function Exec(keyword As String, ByRef args As String) As String
        Dim token As Integer = GetTokenByKeyword(keyword)
        If token = -1 Then
            Outp(MsgType.WARNING, chdr & ".Exec", "keyword '" & keyword & "' not found")
            Return String.Empty
        End If
        Return Exec(token, args)
    End Function

    Public Function ContainsToken(token As Integer) As Boolean
        'Return jumpTable(token) IsNot Nothing
        If jumpTable(token) Is Nothing Then
            For Each fs As Funct In funcSet
                If fs.token = token Then Return True
            Next
            Return False
        End If
        Return True
    End Function

    Public Function isFunction(token As Integer) As Boolean
        Dim f As Funct = GetFunctionByToken(token)
        If f Is Nothing Then Return False Else Return f.IsFunction()
    End Function
End Class
