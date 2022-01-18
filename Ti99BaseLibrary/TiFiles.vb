Imports System.IO

Module TiFiles
    Private Const mhdr As String = "TiFiles"
    Public Const MIN_DISKNUM As Integer = 0
    Public Const MAX_DISKNUM As Integer = 9
    Public Const BASE_MEMADDR As Integer = &H37D7
    Private Const badFileFmtStr As String = "Bad file format"
    Public IOError As Integer

    Public Function LoadProgram(ByRef diskNumber As Integer,
                                ByRef fname As String) As Byte()
        Dim hdr As String = mhdr & ".LoadProgram"
        Dim i As Integer
        IOError = 0

        Dim fullFN As String = GetFullFileName(diskNumber, fname)

        Dim f As FileStream
        Try
            f = File.Open(fullFN, FileMode.Open)
        Catch ex As Exception
            Warn(hdr, ex.Message)
            IOError = 56
            Return Nothing
        End Try

        Dim sectors As UInt16
        Dim flags, eof_offset As Byte
        Dim fileName As String
        Dim sr As New StreamReader(f)

        If f.ReadByte() = 7 Then ' TIFILES format

            If Not FileStringMatch(f, "TIFILES") Then
                Warn(hdr, badFileFmtStr)
                IOError = 56
                f.Close()
                Return Nothing
            End If

            ' position 0x08
            sectors = readWord(f)
            flags = f.ReadByte()
            f.ReadByte() ' #rec/sect
            eof_offset = f.ReadByte()
            f.ReadByte() ' rec-length
            readWord(f) ' #level3-records

            ' position 0x10
            fileName = FileReadString(f, 10).Trim()

            f.Seek(&H1A, SeekOrigin.Begin)
            f.ReadByte() ' MXT
            f.ReadByte() ' reserved
            Dim extHeader As UInt16 = readWord(f)
            readWord(f) ' creation time
            readWord(f) ' update time
            f.ReadByte() ' unused

        Else ' v9t9? format
            f.Seek(0, SeekOrigin.Begin)
            fileName = FileReadString(f, 10).Trim()
            f.Seek(&HA, SeekOrigin.Begin)
            readWord(f) ' ?
            flags = f.ReadByte() ' @0xc
            f.ReadByte()
            sectors = readWord(f) ' @0xe
            eof_offset = f.ReadByte() ' @0x10
        End If

        If String.IsNullOrEmpty(fileName) Then
            Warn(hdr, "null internal file name")
        ElseIf fileName.Chars(0) = Chr(0) Then
            Warn(hdr, "empty internal file name")
        Else
            If fname <> fileName Then Warn(hdr, "internal file name '" & fileName & "' does not match")
        End If

        If (flags And 1) = 0 Then
            Warn(hdr, "this is not a program")
            IOError = 56
            f.Close()
            Return Nothing
        End If

        ' position 0x80: content
        f.Seek(&H80, SeekOrigin.Begin)
        Dim clength = (sectors - 1) * 256 + eof_offset + 1
        Dim content(clength) As Byte
        Try
            For i = 0 To clength - 1
                content(i) = f.ReadByte()
            Next
        Catch ex As Exception
            i += 1
            Warn(hdr, ex.Message & " at position " & i.ToString() & "/" & clength.ToString())
        End Try
        f.Close()
        Return content
    End Function

    Public Function FileStringMatch(ByRef f As FileStream, ByRef refStr As String) As Boolean
        Try
            For i As Integer = 0 To refStr.Length - 1
                If Convert.ToByte(refStr.Chars(i)) <> f.ReadByte() Then Return False
            Next
        Catch ex As Exception
            Warn(mhdr & ".IsSameString", ex.Message)
            Return False
        End Try
        Return True
    End Function

    Public Function FileReadString(ByRef f As FileStream, ByRef len As Integer) As String
        Static strBuff As String
        strBuff = ""
        Try
            For i As Integer = 0 To len - 1
                strBuff &= ChrW(f.ReadByte())
            Next
        Catch ex As Exception
            Warn(mhdr & ".IsSameString", ex.Message)
            Return Nothing
        End Try
        Return strBuff
    End Function

    Public Function SaveProgram(ByRef diskNumber As Integer,
                                ByRef fname As String,
                                ByRef content As Byte()) As Boolean
        Dim hdr As String = mhdr & ".SaveProgram"
        Dim i As Integer
        IOError = 0

        Dim fullFN As String = GetFullFileName(diskNumber, fname)
        Dim f As FileStream
        Try
            f = File.Open(fullFN, FileMode.Create)
        Catch ex As Exception
            Warn(hdr, ex.Message)
            IOError = 56
            Return False
        End Try

        ' position 0x00
        f.WriteByte(7)
        writeChars(f, "TIFILES")

        ' position 0x08
        Dim clength As UInt32 = Convert.ToUInt32(content.Length)
        If clength = 0 Then
            Err(hdr, "empty file content")
            IOError = 56
            f.Close()
            Return False
        End If
        Dim sectors As UInt16 = clength >> 8
        Dim eof_offset As Byte = Convert.ToByte(clength And &HFF)
        If eof_offset > 0 Then sectors += 1
        writeWord(f, sectors) ' sectors
        f.WriteByte(1) ' flag
        f.WriteByte(0) ' #rec/sect
        f.WriteByte(eof_offset) ' EOF offset
        f.WriteByte(0) ' rec-length
        writeWord(f, 0) ' #level3-records

        ' position 0x10
        Dim str As String = (fname & "          ").Substring(0, 10)
        writeChars(f, str.Substring(0, 8)) ' first 8 chars of file nam
        writeChars(f, str.Substring(8, 2)) ' last 2 chars of file name
        f.WriteByte(0) ' MXT
        f.WriteByte(0) ' reserved
        writeWord(f, 0) ' extended header
        writeWord(f, GetTimeWord(Now)) ' creation time
        writeWord(f, GetDateWord(Now)) ' creation time
        writeWord(f, 0) ' update time
        writeWord(f, 0) ' update time

        ' position 0x28: empty space
        Do
            f.WriteByte(0)
        Loop While f.Position < &H80

        ' position 0x80: content
        Try
            'For i = 0 To clength - 1
            '    f.WriteByte(content(i))
            'Next
            f.Write(content, 0, clength)
        Catch ex As Exception
            'i += 1
            'Warn(hdr, ex.Message & " at position " & i.ToString() & "/" & clength.ToString())
            Warn(hdr, ex.Message)
        End Try

        'fills 256-bytes sectors with zeros
        If (clength Mod 256) > 0 Then
            i = 256 - (clength Mod 256) ' remainig bytes to end-of-sector
            While i > 0
                f.WriteByte(0)
                i -= 1
            End While
        End If

        f.Close()
        Return True
    End Function

    ' fornisce il nome file completo nel formato DSK<n>.<nomefile>
    Private Function GetFullFileName(ByRef diskNumber As Integer, ByRef fName As String) As String
        If diskNumber < MIN_DISKNUM Or diskNumber > MAX_DISKNUM Then
            Return My.Settings.workDir & "\" & fName
        Else
            Return My.Settings.workDir & "\DSK" & diskNumber.ToString() & "\" & fName
        End If
    End Function

    Public Sub writeChars(ByRef f As FileStream, ByRef str As String)
        For i As Integer = 0 To str.Length - 1
            f.WriteByte(Asc(str.Chars(i)))
        Next
    End Sub

    Public Function readWord(ByRef f As FileStream) As UInt16
        Dim b0, b1 As UInt16
        b0 = f.ReadByte()
        b1 = f.ReadByte()
        Return b0 * 256 + b1
    End Function

    Public Sub writeWord(ByRef f As FileStream, ByRef w As UInt16)
        f.WriteByte((w >> 8) And &HFF)
        f.WriteByte(w And &HFF)
    End Sub

    Public Function GetTimeWord(ByRef t As DateTime) As UInt16
        Dim w As UInt16
        w = Convert.ToUInt16(t.Hour)
        w <<= 6
        w = w Or (Convert.ToUInt16(t.Minute))
        w >>= 5
        w = w Or (Convert.ToUInt16(t.Minute) >> 1)
        Return w
    End Function

    Public Function GetDateWord(ByRef t As DateTime) As UInt16
        Dim w As UInt16
        Dim year As UInt16 = Convert.ToUInt16(t.Year)
        If year < 1970 Then year = 1970 Else If year > 2069 Then year = 2069
        If year < 2000 Then w = year - 1900 Else w = year - 2000
        w <<= 4
        w = w Or (Convert.ToUInt16(t.Month))
        w <<= 5
        w = w Or (Convert.ToUInt16(t.Day))
        Return w
    End Function
End Module