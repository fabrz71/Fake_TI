Public Class Code
    Protected lines As New List(Of CodeLine)
    Protected firstLine As CodeLine

    Public Sub Clear()
        lines.Clear()
        firstLine = Nothing
    End Sub

    Public Function IsEmpty() As Boolean
        Return (lines.Count() = 0)
    End Function

    Public Function LinesCount() As Integer
        Return lines.Count()
    End Function

    Public Function GetFirstLine() As CodeLine
        Return firstLine
    End Function

    Public Function GetLine(ByRef num As Integer) As CodeLine
        For Each l As CodeLine In lines
            If l.number = num Then Return l
        Next
        Return Nothing
    End Function

    Public Function GetLower(ByRef num As Integer) As CodeLine
        If lines.Count() = 0 Then Return Nothing
        If num <= firstLine.number Then Return Nothing
        Dim cl, nextcl As CodeLine
        nextcl = firstLine
        Do
            cl = nextcl
            If cl.nextLine IsNot Nothing Then
                nextcl = cl.nextLine
            Else
                Exit Do
            End If
        Loop While nextcl.number < num
        Return cl
    End Function

    Public Function GetUpper(ByRef num As Integer) As CodeLine
        If firstLine Is Nothing Then Return Nothing
        Dim cl As CodeLine = firstLine
        Do
            If cl.number > num Then Return cl
            cl = cl.nextLine
        Loop Until cl Is Nothing
        Return Nothing
    End Function

    Public Function AddLine(ByRef num As Integer, ByRef code As String) As Boolean
        Dim l As CodeLine = GetLine(num)
        If l Is Nothing Then ' la linea non esiste: e' da inserire
            Dim prevLine As CodeLine = GetLower(num)
            If prevLine Is Nothing Then ' nessuna linea precedente: viene inserita come prima linea
                l = New CodeLine(num, code, firstLine)
                lines.Add(l)
                firstLine = l
            Else ' inserimento tra due linee o come ultima
                l = New CodeLine(num, code, prevLine.nextLine)
                lines.Add(l)
                prevLine.nextLine = l
            End If
        Else ' la linea esiste gia': aggiorna solo contenuto
            l.content = code
            Return False
        End If
        Return True
    End Function

    Public Function RemoveLine(ByRef num As Integer) As Boolean
        Dim cl, prevCl, nextCl As CodeLine
        cl = GetLine(num)
        If cl Is Nothing Then Return False
        If lines.Count() = 1 Then
            Clear()
            Return True
        End If
        prevCl = GetLower(num)
        nextCl = cl.nextLine
        lines.Remove(cl)
        If prevCl Is Nothing Then
            firstLine = nextCl ' rimozione prima riga
        Else
            If nextCl Is Nothing Then  ' rimozione ultima riga
                prevCl.nextLine = Nothing
            Else
                prevCl.nextLine = nextCl
            End If
        End If
        Return True
    End Function
End Class
