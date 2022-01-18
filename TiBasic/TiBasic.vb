Imports System.IO

' rivedere tutte le richieste di valori Variable.value

Public Class TiBasic
    Inherits Language

    Protected Shadows Const chdr As String = "TIBasic"
    Public Const ROM_NAME As String = "TI BASIC"
    Public Const MAX_LINE_NUMBER As Integer = 32767
    Public Const MIN_FLOAT As Double = 9.99999E-127
    Public Const MAX_FLOAT As Double = 9.99999E+127
    Public Const DEF_RUN_SCRCOLOR As Integer = TiColor.LightGreen
    Protected ReadOnly TOKEN_PREFIX As Char = "%"c
    Protected Const TOKEN_BASECODE As Byte = 0 '48
    Protected Const VAR_ADMITTED_CHARS As String = "@[]\-$ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
    Protected Const DEF_ARRAY_DIM_SIZE As Integer = 10
    Protected Const PLAIN_TEXT_SUBDIR = "Text"
    Protected numbering As Boolean
    Protected numLine, numStep As Integer
    Protected currDataArgs As String()
    Protected currDataLine As CodeLine
    Protected currDataIndex As Integer
    Protected gosubCalls As New List(Of CodeLine) ' stack numeri di linea chiamate GOSUB
    Protected forLoops As New List(Of ForCycle) ' stack di cicli FOR-NEXT
    Protected currentLine, nextLine As CodeLine
    Protected currentToken As Integer
    Protected inputSetupStr As String
    Protected inputStr As String ' input text string from INPUT instruction
    Protected WithEvents cnsl As TiConsole
    Protected functionResult As Variable
    Protected optionBase0 As Boolean
    Protected trace As Boolean
    Protected rnd As Random
    Protected printIstrEval As Boolean ' vero solo durante valutazione espressione in istruzione PRINT
    Protected lastKey As Byte ' utile per istruzione CALL KEY
    Protected auxTokens As New Dictionary(Of Char, Integer) ' token per simboli di lunghezza 1
    Protected IOError As Integer
    'Protected ReadOnly InputCompletedEvent As New Threading.ManualResetEvent(False)
    Protected runtimeTextInputReady As Boolean

    Protected Structure ProgramFileHeader
        Public lineNumTable_Start As UInt16
        Public lineNumTable_End As UInt16
        Public programLines_End As UInt16
        Public chkWord As UInt16
    End Structure

    Public Enum CmdId
        RUN = &H0
        NEWW = &H1
        CON = &H2
        LIST = &H3
        BYE = &H4
        NUM = &H5
        OLD = &H6
        RES = &H7
        SAVE = &H8
        EDIT = &H9
        BREAK = &H8E
        TRACE = &H90
        UNTRACE = &H91
        TEST = &HFF ' non originale
    End Enum

    Public Enum IstrId
        _ELSE = &H81
        _IF = &H84
        GO = &H85
        GO_TO = &H86
        GO_SUB = &H87
        RETRN = &H88
        DEF = &H89
        _DIM = &H8A
        _END = &H8B
        _FOR = &H8C
        _LET = &H8D
        INPUT = &H92
        DATA = &H93
        RESTORE = &H94
        RANDOM = &H95
        _NEXT = &H96
        READ = &H97
        _STOP = &H98
        REMRK = &H9A
        _ON = &H9B
        PRINT = &H9C
        _CALL = &H9D
        OPTN = &H9E
        _SUB = &HA1
        DISPLAY = &HA2
        _THEN = &HB0
        _TO = &HB1
        _STEP = &HB2
    End Enum

    Public Enum FunId
        SGN = &HD1
        INT = &HCF
        ABS = &HCB
        ATN = &HCC
        CHR = &HD6
        ASC = &HDC
        LEN = &HD5
        COS = &HCD
        SIN = &HD2
        TAN = &HD4
        LOG = &HD0
        EXP = &HCE
        SQR = &HD3
        VAL = &HDA
        STR = &HDB
        POS = &HD9
        SEG = &HD8
        RND = &HD7
        TAB = &HFC
    End Enum

    Public Enum ErrorId
        NONE
        INCORRECT_STATEMENT
        CANT_DO_THAT
        SYNTAX_ERROR
        BAD_NAME
        BAD_VALUE
        BAD_ARGUMENT
        BAD_LINE_NUMBER
        STR_NUM_MISMATCH
        MEMORY_FULL
        DATA_ERROR
        FOR_NEXT
        NAME_CONFLICT
        BAD_SUBSCRIPT
        IO
        SIMULATOR_ERROR ' non originale
        UNIMPLEMENTED ' non originale
    End Enum

    Public Enum WarnId
        NONE
        NUMBER_TOO_BIG
        INPUT_ERROR
        UNIMPLEMENTED ' non originale
    End Enum

    Structure ForCycle
        Dim beginLine As CodeLine
        Dim var As Variable
        Dim endValue, stepValue As Double
    End Structure

    Sub New(ByRef m As TI99)
        MyBase.New(m)
        Text.Encoding.RegisterProvider(Text.CodePagesEncodingProvider.Instance)
        spaceSeparators = True
        cnsl = New TiBasicConsole(m, m.renderBox)
        m.consl.Dispose()
        m.consl = cnsl
        numbering = False
        inputSetupStr = String.Empty
        optionBase0 = True
        trace = False
        functionResult = New Variable(VarType.UNDEF)
        rnd = New Random()
        lastKey = 0

        ' variable types
        typeSet.Add(VarType.UNDEF)
        typeSet.Add(VarType.FLOAT)
        typeSet.Add(VarType.STRNG)

        ' commands
        cmdSet = New FunctionCollection(256)
        cmdSet.AddCommand("LIST", CmdId.LIST, False, False, AddressOf CMD_List)
        cmdSet.AddCommand("NEW", CmdId.NEWW, False, False, AddressOf CMD_New)
        cmdSet.AddCommand("RES", CmdId.RES, False, False, AddressOf CMD_Resequence)
        cmdSet.AddCommand("RESEQUENCE", CmdId.RES, False, False, AddressOf CMD_Resequence)
        cmdSet.AddCommand("CON", CmdId.CON, False, False, AddressOf CMD_Continue)
        cmdSet.AddCommand("CONTINUE", CmdId.CON, False, False, AddressOf CMD_Continue)
        cmdSet.AddCommand("EDIT", CmdId.EDIT, True, False, AddressOf CMD_Edit)
        cmdSet.AddCommand("TEST", CmdId.TEST, True, False, AddressOf CMD_Test)
        cmdSet.AddCommand("BYE", CmdId.BYE, False, False, AddressOf CMD_Bye)
        cmdSet.AddCommand("NUM", CmdId.NUM, False, False, AddressOf CMD_Number)
        cmdSet.AddCommand("NUMBER", CmdId.NUM, False, False, AddressOf CMD_Number)
        cmdSet.AddCommand("TRACE", CmdId.TRACE, False, True, AddressOf CMD_Trace)
        cmdSet.AddCommand("UNTRACE", CmdId.UNTRACE, False, True, AddressOf CMD_Untrace)
        cmdSet.AddCommand("RUN", CmdId.RUN, False, False, AddressOf CMD_Run)
        cmdSet.AddCommand("SAVE", CmdId.SAVE, True, False, AddressOf CMD_Save)
        cmdSet.AddCommand("OLD", CmdId.OLD, True, False, AddressOf CMD_Old)
        cmdSet.AddCommand("BREAK", CmdId.BREAK, False, True, AddressOf CMD_Break)

        ' program instructions
        cmdSet.AddIstruction("PRINT", IstrId.PRINT, False, True, False, AddressOf ISTR_Print)
        cmdSet.AddIstruction("DISPLAY", IstrId.DISPLAY, False, True, False, AddressOf ISTR_Print)
        cmdSet.AddIstruction("GOTO", IstrId.GO_TO, True, False, True, AddressOf ISTR_Goto)
        cmdSet.AddIstruction("GOSUB", IstrId.GO_SUB, True, False, True, AddressOf ISTR_Gosub)
        cmdSet.AddIstruction("GO", IstrId.GO, True, False, True, AddressOf ISTR_Go)
        cmdSet.AddIstruction("TO", IstrId._TO, True, False, False, AddressOf ISTR_NoOp)
        cmdSet.AddIstruction("SUB", IstrId._SUB, True, False, False, AddressOf ISTR_NoOp)
        cmdSet.AddIstruction("RETURN", IstrId.RETRN, False, False, True, AddressOf ISTR_Return)
        cmdSet.AddIstruction("IF", IstrId._IF, True, False, True, AddressOf ISTR_If)
        cmdSet.AddIstruction("THEN", IstrId._THEN, True, False, True, AddressOf ISTR_NoOp)
        cmdSet.AddIstruction("ELSE", IstrId._ELSE, True, False, True, AddressOf ISTR_NoOp)
        cmdSet.AddIstruction("ON", IstrId._ON, True, False, True, AddressOf ISTR_OnGotoGosub)
        cmdSet.AddIstruction("LET", IstrId._LET, True, True, False, AddressOf ISTR_Let)
        cmdSet.AddIstruction("CALL", IstrId._CALL, True, True, False, AddressOf ISTR_Call_)
        cmdSet.AddIstruction("REM", IstrId.REMRK, False, True, False, AddressOf ISTR_NoOp)
        cmdSet.AddIstruction("DATA", IstrId.DATA, True, True, False, AddressOf ISTR_NoOp)
        cmdSet.AddIstruction("READ", IstrId.READ, True, True, False, AddressOf ISTR_Read)
        cmdSet.AddIstruction("RESTORE", IstrId.RESTORE, False, True, False, AddressOf ISTR_Restore)
        cmdSet.AddIstruction("END", IstrId._END, False, True, False, AddressOf ISTR_End)
        cmdSet.AddIstruction("FOR", IstrId._FOR, True, False, False, AddressOf ISTR_For)
        cmdSet.AddIstruction("NEXT", IstrId._NEXT, True, False, True, AddressOf ISTR_Next)
        cmdSet.AddIstruction("STEP", IstrId._STEP, True, False, False, AddressOf ISTR_NoOp)
        cmdSet.AddIstruction("RANDOMIZE", IstrId.RANDOM, False, True, False, AddressOf ISTR_Random)
        cmdSet.AddIstruction("STOP", IstrId._STOP, False, True, False, AddressOf ISTR_End)
        cmdSet.AddIstruction("DIM", IstrId._DIM, True, True, False, AddressOf ISTR_Dim)
        cmdSet.AddIstruction("OPTION", IstrId.OPTN, True, True, False, AddressOf ISTR_Option)
        cmdSet.AddIstruction("DEF", IstrId.DEF, True, False, False, AddressOf ISTR_Def)
        cmdSet.AddIstruction("INPUT", IstrId.INPUT, True, False, False, AddressOf ISTR_Input)

        ' functions
        cmdSet.AddFunction("SGN", FunId.SGN, True, AddressOf FUN_Sgn)
        cmdSet.AddFunction("ABS", FunId.ABS, True, AddressOf FUN_Abs)
        cmdSet.AddFunction("INT", FunId.INT, True, AddressOf FUN_Int)
        cmdSet.AddFunction("ATN", FunId.ATN, True, AddressOf FUN_Atn)
        cmdSet.AddFunction("CHR$", FunId.CHR, True, AddressOf FUN_Chr)
        cmdSet.AddFunction("ASC", FunId.ASC, True, AddressOf FUN_Asc)
        cmdSet.AddFunction("LEN", FunId.LEN, True, AddressOf FUN_Len)
        cmdSet.AddFunction("COS", FunId.COS, True, AddressOf FUN_Cos)
        cmdSet.AddFunction("SIN", FunId.SIN, True, AddressOf FUN_Sin)
        cmdSet.AddFunction("TAN", FunId.TAN, True, AddressOf FUN_Tan)
        cmdSet.AddFunction("LOG", FunId.LOG, True, AddressOf FUN_Log)
        cmdSet.AddFunction("EXP", FunId.EXP, True, AddressOf FUN_Exp)
        cmdSet.AddFunction("SQR", FunId.SQR, True, AddressOf FUN_Sqr)
        cmdSet.AddFunction("VAL", FunId.VAL, True, AddressOf FUN_Val)
        cmdSet.AddFunction("STR$", FunId.STR, True, AddressOf FUN_Str)
        cmdSet.AddFunction("SEG$", FunId.SEG, True, AddressOf FUN_Seg)
        cmdSet.AddFunction("POS", FunId.POS, True, AddressOf FUN_Pos)
        cmdSet.AddFunction("RND", FunId.RND, False, AddressOf FUN_Rnd)
        cmdSet.AddFunction("TAB", FunId.TAB, True, AddressOf FUN_Tab) ' solo per istruzione PRINT

        ' operators
        operSet.AddBinaryNumericOperator("^", 5, AddressOf OPER_Power)
        operSet.AddBinaryNumericOperator("*", 4, AddressOf OPER_Product)
        operSet.AddBinaryNumericOperator("/", 4, AddressOf OPER_Division)
        operSet.AddOperator("+", 3, False, False, True, False, AddressOf OPER_Sum)
        operSet.AddOperator("-", 3, False, False, True, False, AddressOf OPER_Subtraction)
        operSet.AddBinaryOperator("<>", 2, AddressOf OPER_DiffersFrom)
        operSet.AddBinaryOperator("<=", 2, AddressOf OPER_LessOrEqualThan)
        operSet.AddBinaryOperator(">=", 2, AddressOf OPER_MoreOrEqualThan)
        operSet.AddBinaryOperator("=", 2, AddressOf OPER_EqualsTo)
        operSet.AddBinaryOperator("<", 2, AddressOf OPER_LessThan)
        operSet.AddBinaryOperator(">", 2, AddressOf OPER_MoreThan)
        operSet.AddBinaryStringOperator("&", 1, AddressOf OPER_Concat)

        Operatr.missingOperand_ErrCode = ErrorId.INCORRECT_STATEMENT
        Operatr.typeMistmach_ErrCode = ErrorId.STR_NUM_MISMATCH
        Operatr.redundantOperand_ErrCode = ErrorId.INCORRECT_STATEMENT

        auxTokens.Add(","c, &HB3)
        auxTokens.Add(";"c, &HB4)
        auxTokens.Add(":"c, &HB5)
        auxTokens.Add(")"c, &HB6)
        auxTokens.Add("("c, &HB7)
        auxTokens.Add("&"c, &HB8)
        auxTokens.Add("="c, &HBE)
        auxTokens.Add("<"c, &HBF)
        auxTokens.Add(">"c, &HC0)
        auxTokens.Add("+"c, &HC1)
        auxTokens.Add("-"c, &HC2)
        auxTokens.Add("*"c, &HC3)
        auxTokens.Add("/"c, &HC4)
        auxTokens.Add("^"c, &HC5)
        auxTokens.Add("#"c, &HFD)

        errMsgList.Add(ErrorId.NONE, "(NO ERROR)")
        errMsgList.Add(ErrorId.INCORRECT_STATEMENT, "INCORRECT STATEMENT")
        errMsgList.Add(ErrorId.CANT_DO_THAT, "CAN'T DO THAT")
        errMsgList.Add(ErrorId.SYNTAX_ERROR, "SYNTAX ERROR")
        errMsgList.Add(ErrorId.BAD_NAME, "BAD NAME")
        errMsgList.Add(ErrorId.BAD_VALUE, "BAD VALUE")
        errMsgList.Add(ErrorId.BAD_ARGUMENT, "BAD ARGUMENT")
        errMsgList.Add(ErrorId.BAD_LINE_NUMBER, "BAD LINE NUMBER")
        errMsgList.Add(ErrorId.STR_NUM_MISMATCH, "STRING-NUMBER MISMATCH")
        errMsgList.Add(ErrorId.DATA_ERROR, "DATA ERROR")
        errMsgList.Add(ErrorId.MEMORY_FULL, "MEMORY FULL")
        errMsgList.Add(ErrorId.FOR_NEXT, "FOR-NEXT ERROR")
        errMsgList.Add(ErrorId.NAME_CONFLICT, "NAME CONFLICT")
        errMsgList.Add(ErrorId.BAD_SUBSCRIPT, "BAD SUBSCRIPT")
        errMsgList.Add(ErrorId.IO, "I/O ERROR")
        errMsgList.Add(ErrorId.SIMULATOR_ERROR, "SIMULATOR ERROR") ' non originale
        errMsgList.Add(ErrorId.UNIMPLEMENTED, "UNIMPL'D FEATURE") ' non originale

        warnMsgList.Add(WarnId.NONE, "(NO WARNING)")
        warnMsgList.Add(WarnId.NUMBER_TOO_BIG, "NUMBER TOO BIG")
        warnMsgList.Add(WarnId.INPUT_ERROR, "INPUT ERROR") ' non originale
        warnMsgList.Add(WarnId.UNIMPLEMENTED, "UNIMPL'D FEATURE") ' non originale
        ExecCommand("NEW")
    End Sub

    Public Overrides Sub Start()
        code.Clear()
        ClearVariables()
        cnsl.Init()
        cnsl.PrintLn(ROM_NAME & " READY")
        cnsl.StartNewTextLineInput()
    End Sub

    Public Overrides Function GetRomName() As String
        Return ROM_NAME
    End Function

    Public Overrides Sub Quit()
        StopRun(True)
    End Sub

    'Public Sub ClearVariables()
    '    variables.Clear()
    'End Sub

    Public Sub LineInputEvent(ByRef sender As Object, ByRef line As String) Handles cnsl.TextInputCompleted
        If running Then
            inputStr = New String(line)
            'InputCompletedEvent.Set()
            runtimeTextInputReady = True
            Return
        End If
        Dim reply As String = ReadInputLine(line)
        If Not String.IsNullOrEmpty(reply) Then cnsl.PrintLn(reply)
        If line = cmdSet.GetKeywordByToken(CmdId.NEWW) Then Return
        cnsl.StartNewTextLineInput()
    End Sub

    ' legge linea di testo in input:
    ' - la memorizza come istruzione di programma se e' numerata
    ' - la esegue immediatamente altrimenti
    Public Overrides Function ReadInputLine(inputText As String) As String
        'Dim hdr As String = chdr & ".ReadInputLine"
        errorCode = ErrorId.NONE
        If String.IsNullOrEmpty(inputText) Then Return String.Empty
        inputText = inputText.ToUpper().Trim()
        If Char.IsDigit(inputText.Chars(0)) Then ' inseirmento linea numerata (programma)
            ReadProgramLine(inputText)
            If errorCode Then Return GetErrorMessage()
            If numbering Then AdvanceLineNumbering()
            Return String.Empty
        Else ' input di comando immediato
            cnsl.NewLine()
            Return ExecCommand(inputText)
        End If
    End Function

    Protected Function ReadProgramLine(inputText As String) As String
        Dim hdr As String = chdr & ".ReadProgramLine"
        Dim i, n, ln As Integer
        Dim nStr, pStr As String

        ' ricerca primo carattere non numerico diverso da spazio
        i = 0
        ln = inputText.Length()
        While Char.IsNumber(inputText.Chars(i)) And i < ln - 1
            i += 1
        End While

        ' lettura numero di linea
        nStr = inputText.Substring(0, i + 1)
        Try
            n = Convert.ToInt32(nStr)
        Catch ex As Exception
            Return SetError(ErrorId.BAD_LINE_NUMBER)
        End Try
        If n < 1 Or n > MAX_LINE_NUMBER Then Return SetError(ErrorId.BAD_LINE_NUMBER)
        If i = ln - 1 Then ' solo numero: rimuove linea di codice
            If numbering Then
                StopNumbering()
            Else
                Util.Info(hdr, "Removing line " & n.ToString())
                RemoveLine(n)
            End If
            Return String.Empty
        End If

        ' lettura codice linea
        While inputText.Chars(i) = Chr(32) And i < ln - 1
            i += 1
        End While
        If i < ln Then
            pStr = inputText.Substring(i)
        Else
            RemoveLine(n)
            Return String.Empty
        End If

        ' memorizzazione linea
        pStr = EncodeCodeLine(pStr)
        Util.Info(hdr, "Storing line " & n.ToString())
        If StoreCodeLine(n, pStr) < 0 Then Return SetError(ErrorId.BAD_LINE_NUMBER)

        Return String.Empty
    End Function

    ''' <summary>
    ''' Memorizza, sovrascrive o elimina una linea di programmazione. 
    ''' Le parole chiave vengono memorizzate codificate in token.
    ''' </summary>
    ''' <param name="lineNum">numero di linea da memorizzare</param>
    ''' <param name="code">codice della linea</param>
    ''' <returns>numero di linea inserita/rimossa, oppure 
    ''' 0 in caso di nessun azione, -1 in caso di errore</returns>
    Protected Function StoreCodeLine(ByRef lineNum As Integer, ByRef codeStr As String) As Integer
        If String.IsNullOrEmpty(codeStr) Then Return 0
        If Not code.AddLine(lineNum, codeStr) Then Return 0
        Return lineNum
    End Function

    ''' <summary>
    ''' Elimina una linea di programmazione. 
    ''' </summary>
    ''' <param name="lineNum">numero di linea da rimuovere</param>
    ''' <returns>numero di linea inserita/rimossa, oppure 
    ''' 0 in caso di nessun azione</returns>
    Protected Function RemoveLine(ByRef lineNum As Integer) As Integer
        If code.RemoveLine(lineNum) Then
            'consl.PrintLn(DBGMSG_PREFIX & "line " & lineNum & " removed.")
            Return lineNum
        Else
            'consl.PrintLn(DBGMSG_PREFIX & "no line removed.")
            Return 0
        End If
    End Function

    ' executes an *unencoded* code line (with no tokens)
    Public Overrides Function ExecCommand(funcStr As String) As String
        Dim str As String = EncodeCodeLine(funcStr)
        Return ExecCode(str)
    End Function

    ' executes an *encoded* program line
    Public Overrides Function ExecCode(funcStr As String) As String
        errorCode = ErrorId.NONE

        'tmpVarCount = 0
        If String.IsNullOrEmpty(funcStr) Then Return String.Empty
        Static cmdStr, args As String
        'Dim currentToken As Integer
        Static fn As Funct

        ' estrae sottostringhe: token e argomenti
        funcStr = funcStr.Trim()
        Dim i As Integer = funcStr.IndexOf(" ")
        If i = 2 Then
            cmdStr = funcStr.Substring(0, i)
            args = funcStr.Substring(i).Trim()
        Else
            cmdStr = funcStr
            args = String.Empty
        End If

        ' cerca ed esegue token comando/istruzione
        currentToken = ExtractTokenFromStr(cmdStr)
        If currentToken >= 0 Then 'trovata keyword
            fn = cmdSet.GetFunctionByToken(currentToken)
            If fn IsNot Nothing Then
                If fn.mandatoryArguments And args Is Nothing Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                If running Then
                    If fn.IsIstruction Then Return cmdSet.Exec(currentToken, args)
                Else
                    If fn.IsCommand Then Return cmdSet.Exec(currentToken, args)
                End If
                Return SetError(ErrorId.CANT_DO_THAT)
            End If
        Else ' cerca ed esegue assegnazione
            If funcStr.IndexOf("=") > 0 Then
                Return cmdSet.Exec(IstrId._LET, funcStr)
            End If
        End If

        Return SetError(ErrorId.INCORRECT_STATEMENT)
    End Function

    ' trova le keyword nella stringa e le sostituisce con i rispettivi token
    Public Overrides Function EncodeCodeLine(codeLine As String) As String
        If String.IsNullOrEmpty(codeLine) Then Return String.Empty
        Dim tokenStr As String
        Dim f As Funct
        Dim i As Integer = 0
        Dim inString As Boolean = False
        Dim isFirstAlpha As Boolean = False
        Dim ch As Char

        Do
            ch = codeLine.Chars(i)
            isFirstAlpha = Char.IsLetter(ch) And (Not isFirstAlpha)
            If ch <> " "c Then
                If ch.Equals(Chr(34)) Then inString = Not inString
                If (Not inString) And isFirstAlpha Then
                    f = cmdSet.SearchKeywordAndGetFunction(codeLine, i)
                    If f IsNot Nothing Then
                        tokenStr = GetTokenCodeStr(f.token)
                        codeLine = codeLine.Replace(f.keyword, tokenStr)
                        If f.IsCommand And Not f.IsIstruction Then Exit Do
                        If f.token = IstrId.REMRK Or f.token = IstrId.DATA Then Exit Do
                        i += 1
                    End If
                End If
            End If
            i += 1
        Loop While i < codeLine.Length

        Return codeLine
    End Function

    ' restituisce sequenza di codifica "token"
    Public Function GetTokenCodeStr(tk As Byte) As String
        Dim val As UInt16 = CUInt(tk) + TOKEN_BASECODE
        If val > 255 Then Warn(chdr & "GetTokenCodeStr", "illegal value " & val.ToString())
        Return Convert.ToString(TOKEN_PREFIX) & Convert.ToString(Chr(val))
    End Function

    ''' <summary>
    ''' estrae codice token da sequenza di codifica (lunga 2 caratteri) che deve cominciare con TOKEN_PREFIX_CHAR
    ''' </summary>
    ''' <param name="tkSeq">sequenza di codifica (2 caratteri)</param>
    ''' <returns></returns>
    Public Function ExtractTokenFromStr(ByRef tkSeq As String, Optional ByRef startIdx As Integer = 0) As Integer
        If String.IsNullOrEmpty(tkSeq) Then Return -1
        If tkSeq.Chars(startIdx) <> TOKEN_PREFIX Then Return -1
        Return Convert.ToByte((Asc(tkSeq.Chars(startIdx + 1)) - TOKEN_BASECODE) And 255)
    End Function

    ''' <summary>
    ''' Estrae argomenti dalla stringa specificata isolati dai separatori dati, e li valuta 
    ''' restituendo variabili di tipo corrispondente.
    ''' </summary>
    ''' <param name="argList">Stringa contenente gli argomenti separati</param>
    ''' <param name="separator">carattere utilizzato come separatore di argomenti</param>
    ''' <param name="minArgsCount">numero minimo di argomenti attesi</param>
    ''' <param name="types">Stringa di descrizione dei tipi attesi degli argomenti estratti, nel loro
    ''' medesimo ordine. Nella stringa 'N' indica un parametro numerico, 'S' un parametro di stringa, 
    ''' altri caratteri non specificano alcun tipo richiesto e sono ininfluenti.</param>
    ''' <returns>Array di variabili contenenti i valori degli argomenti valutati.
    ''' In caso di mancata corrispondenza tra i tipi richiesti e quelli estratti
    ''' viene restituito Nothing e generato l'errore.
    ''' In caso di numero minimo di parametri non presenti
    ''' viene restituito Nothing e generato l'errore.</returns>
    Public Function ExtractArgsFromString(ByRef argList As String,
                                          ByRef separator As Char,
                                          Optional ByRef minArgsCount As Integer = 0,
                                          Optional ByRef types As String = Nothing) As Variable()
        Dim hdr As String = chdr & "ExtractArgsFromStr"
        If String.IsNullOrEmpty(argList) Then
            Warn(hdr, "null list of arguments")
            Return Nothing
        End If

        ' estrazione argomenti
        Dim i As Integer
        Dim args As String() = argList.Split(separator, StringSplitOptions.TrimEntries)
        If args.Length < minArgsCount Then
            SetError(ErrorId.INCORRECT_STATEMENT)
            Return Nothing
        End If
        Dim values(args.Length - 1) As Variable
        For i = 0 To args.Length - 1
            values(i) = EvalExpression(args(i))
            If errorCode Then Return Nothing
        Next

        ' verifica dei tipi
        If types IsNot Nothing Then
            Dim expectedType As VarType
            Dim ch As Char
            For i = 0 To types.Length - 1
                ch = types.Chars(i)
                If i >= args.Length Then Exit For
                If ch = "N"c Then
                    expectedType = VarType.FLOAT
                ElseIf ch = "S"c Then
                    expectedType = VarType.STRNG
                Else
                    Continue For
                End If
                If values(i).type <> expectedType Then
                    SetError(ErrorId.STR_NUM_MISMATCH)
                    Return Nothing
                End If
            Next
        End If

        Return values
    End Function

    ' trova i token nella stringa e le sostituisce con le rispettive keyword
    Protected Function DecodeCodeLine(lstr As String) As String
        If String.IsNullOrEmpty(lstr) Then Return lstr
        If lstr.Length < 2 Then Return lstr
        Dim i, tk As Integer
        Dim tkStr, keywStr As String
        Dim inString As Boolean = False
        i = 0
        Do
            If lstr.Chars(i) = Chr(34) Then inString = Not inString
            If Not inString Then
                tk = ExtractTokenFromStr(lstr, i)
                If tk >= 0 Then
                    tkStr = lstr.Substring(i, 2)
                    keywStr = cmdSet.GetKeywordByToken(tk)
                    lstr = lstr.Replace(tkStr, keywStr)
                End If
                If tk = IstrId.REMRK Then Exit Do
            End If
            i += 1
        Loop While i < lstr.Length - 2
        Return lstr
    End Function

    ' evidenzia in modo leggibile i token nella stringa data
    Protected Function GetRawCodeLine(lstr As String) As String
        If String.IsNullOrEmpty(lstr) Then Return lstr
        If lstr.Length < 2 Then Return lstr
        Dim i, tk As Integer
        Dim tkStr, keywStr As String
        Dim inString As Boolean = False
        i = 0
        Do
            If lstr.Chars(i) = Chr(34) Then inString = Not inString
            If Not inString Then
                tk = ExtractTokenFromStr(lstr, i)
                If tk >= 0 Then
                    tkStr = lstr.Substring(i, 2)
                    keywStr = "[" & tk.ToString & "]"
                    lstr = lstr.Replace(tkStr, keywStr)
                End If
                If tk = IstrId.REMRK Then Exit Do
            End If
            i += 1
        Loop While i < lstr.Length - 2
        Return lstr
    End Function

    Protected Shared Function FindCodeLineWithHeader(ByRef header As String, ByRef fromLine As CodeLine) As CodeLine
        If fromLine Is Nothing Then Return Nothing
        Dim cl As CodeLine = fromLine
        Do
            If cl.content.IndexOf(header) = 0 Then Exit Do
            cl = cl.nextLine
        Loop Until cl Is Nothing
        Return cl
    End Function

    Protected Function FindCodeLineWithHeader(ByRef istrId As IstrId, ByRef fromLine As CodeLine) As CodeLine
        If fromLine Is Nothing Then Return Nothing
        Dim cl As CodeLine = fromLine
        Dim header As String = GetTokenCodeStr(istrId)
        Do
            If cl.content.IndexOf(header) = 0 Then Exit Do
            cl = cl.nextLine
        Loop Until cl Is Nothing
        Return cl
    End Function

    Public Sub Run(Optional lineNum As Integer = 0, Optional resetVariables As Boolean = True)
        If resetVariables Then
            ClearVariables()
            'FindAllDataLines()
            'If dataLineSet.Count > 0 Then currDataLine = dataLineSet.First() Else currDataLine = Nothing
            currDataLine = FindCodeLineWithHeader(IstrId.DATA, code.GetFirstLine())
            currDataIndex = 0
        End If
        If lineNum = 0 Then
            currentLine = code.GetFirstLine()
        Else
            currentLine = code.GetLine(lineNum)
        End If
        running = True

        ' colori e bitmap
        If resetVariables Then
            machine.video.Restore()
            machine.video.SetScreenColor(DEF_RUN_SCRCOLOR)
        End If
        machine.video.SwitchToAltCharSet()
        errorCode = ErrorId.NONE
        warningCode = WarnId.NONE

        Dim str As String
        While running
            If currentLine Is Nothing Then
                StopRun(True)
                Exit While
            End If
            nextLine = currentLine.nextLine ' puo' essere alterato dall'istruzione
            If trace Then cnsl.Print("<" & currentLine.number.ToString & ">")
            If currentLine.breakpoint Then
                str = Break()
                cnsl.PrintLn(str)
                Exit While
            End If
            str = ExecCode(currentLine.content) ' può aggiornare currentLine
            If Not String.IsNullOrEmpty(str) Then cnsl.PrintLn(str)
            Application.DoEvents() ' TODO provvisorio
            If errorCode Then
                StopRun(False)
                Exit While
            End If
            currentLine = nextLine
        End While
    End Sub

    Public Sub StopRun(ByRef endProgram As Boolean)
        running = False
        If endProgram Then
            cnsl.NewLine()
            cnsl.PrintLn("** DONE **")
        End If
        machine.video.SetScreenColor(TiVideo.DEF_BACKCOLOR)
        machine.video.SwitchToDefaultCharSet()
    End Sub

    Public Function Break() As String
        If Not running Then Return SetError(ErrorId.CANT_DO_THAT)
        StopRun(False)
        breakState = True
        currentLine.breakpoint = False
        Return "* BREAKPOINT AT " & currentLine.number.ToString()
    End Function

    Public Sub Cont()
        breakState = False
        Run(currentLine.number, False)
    End Sub

    Protected Function CMD_List(params As String) As String
        If code.IsEmpty Then Return SetError(ErrorId.CANT_DO_THAT)
        Dim intrv As Interval = Parser.GetInterval(params)
        If intrv.startp = -1 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        List(intrv)
        Return String.Empty
    End Function

    Protected Sub List(intrv As Interval, Optional ByRef streamWriter As StreamWriter = Nothing)
        Dim lnStr As String
        Dim ln As CodeLine = code.GetLine(intrv.startp)
        If ln Is Nothing Then ln = code.GetUpper(intrv.startp)
        If intrv.endp = 0 Then intrv.endp = MAX_LINE_NUMBER

        While ln.number <= intrv.endp
            lnStr = DecodeCodeLine(ln.content)
            If streamWriter Is Nothing Then
                cnsl.PrintLn(ln.number.ToString & " " & lnStr)
            Else
                streamWriter.WriteLine(ln.number.ToString & " " & lnStr)
            End If
            ln = ln.nextLine
            If ln Is Nothing Then Exit While
        End While
    End Sub

    Protected Function CMD_New(params As String) As String
        Start()
        Return String.Empty
    End Function

    Protected Function CMD_Resequence(params As String) As String
        Dim startLn As Integer = 100
        Dim stp As Integer = 10
        If Not String.IsNullOrEmpty(params) Then
            Dim args As Integer() = Parser.GetNumericParams(params, ",")
            If args(0) = -1 Then Return SetError(ErrorId.BAD_LINE_NUMBER)
            startLn = args(0)
            If args(1) = -1 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
            stp = args(1)
        End If
        Return Renumber(startLn, stp)
    End Function

    ' TODO: aggiustare anche i riferimenti di linea nel programma!
    Public Function Renumber(ByRef startNumber As Integer, ByRef stp As Integer) As String
        If code.LinesCount() = 0 Then Return SetError(ErrorId.CANT_DO_THAT)
        Dim n As Integer = startNumber
        Dim cl As CodeLine = code.GetFirstLine()
        Do
            cl.number = n
            If cl.nextLine Is Nothing Then Exit Do
            n += stp
            'cl = lines.Item(cl.nextIdx)
            cl = cl.nextLine
        Loop
        Return String.Empty
    End Function

    Protected Sub StartNumbering(start As Integer, stp As Integer)
        numLine = start
        numStep = stp
        numbering = True
        inputSetupStr = numLine.ToString & " "
    End Sub

    Protected Sub StopNumbering()
        numbering = False
        inputSetupStr = String.Empty
    End Sub

    Protected Sub AdvanceLineNumbering()
        numLine += numStep
        If numLine > MAX_LINE_NUMBER Then
            StopNumbering()
        Else
            inputSetupStr = numLine.ToString & " "
        End If
    End Sub

    'Protected Sub ClearTempVars()
    '    tmpVar.Clear()
    'End Sub

    ''' <summary>
    ''' Valuta un'espressione generica e restituisce il suo valore in un'intanza di Variable.
    ''' Restituisce Nothing in caso di errori, definendo il tipo di errore.
    ''' Se <paramref name="expectedReturnType"/> e' definito, verifica il tipo di variabile risultante
    ''' e genera errore in caso di mancata corrispondenza.
    ''' </summary>
    ''' <param name="expr">stringa dell'espressione da esaminare</param>
    ''' <param name="printArgs">indica se l'espressione e' argomento di un istruzione PRINT</param>
    ''' <param name="expectedReturnType">tipo di variabile strettamente richiesto</param>
    ''' <returns></returns>
    Public Overrides Function EvalExpression(ByRef expr As String,
                                             Optional expectedReturnType As VarType = VarType.UNDEF) As Variable

        Dim hdr = chdr & ".EvalExpression"
        Dim v1, v2 As Variable
        Dim parts As HeaderAndArguments
        Dim typeChk As Boolean = (expectedReturnType <> VarType.UNDEF)
        Dim vres As Variable

        If String.IsNullOrEmpty(expr) Then
            If expr Is Nothing Then Warn(hdr, "null expression string") Else Info(hdr, "empty string")
            Return Nothing ' non fa nulla
        End If
        'Info(hdr, "evaluating '" & expr & "'...")

        Dim ln As Integer = expr.Length
        Dim firstCh As Char = expr.Chars(0)

        ' ricerca costante numerica
        vres = Parser.ExtractNumericConstant(expr)
        If vres IsNot Nothing Then Return CheckedVar(typeChk, vres, expectedReturnType)

        ' ricerca costante di stringa (racchiusa da apici)
        vres = Parser.ExtractStringConstant(expr)
        If vres IsNot Nothing Then Return CheckedVar(typeChk, vres, expectedReturnType)

        ' ricerca operatore con priorita'
        Dim operParts As OperationParseResult = operSet.ExtractOperatorAndArguments(expr)
        If operParts.op IsNot Nothing Then
            v1 = EvalExpression(operParts.lArg)
            v2 = EvalExpression(operParts.rArg)
            'Dim vres As Variable = operParts.op.executor(v1, v2)
            vres = operParts.op.execute(v1, v2)
            If vres.type = VarType.ERR Then
                SetError(vres.GetError())
                Return Nothing
            End If
            Return CheckedVar(typeChk, vres, expectedReturnType)
        End If

        ' ricerca parentesi aperta iniziale
        If firstCh = "("c Then
            Dim i As Integer = Parser.SearchClosingBracket(expr)
            If i < ln - 1 Then
                If i = 1 Then
                    Info(hdr, "no content between brackets in expression " & expr)
                ElseIf i = -1 Then
                    Info(hdr, "missing closing bracket in expression " & expr)
                Else
                    Warn(hdr, "closing bracket should'n be there" & expr)
                End If
                SetError(ErrorId.INCORRECT_STATEMENT)
                Return Nothing
            End If
            Dim subExprStr As String = expr.Substring(1, i - 1)
            vres = EvalExpression(subExprStr)
            Return CheckedVar(typeChk, vres, expectedReturnType)
        End If

        ' estrazione nome ed eventuali parametri seguenti tra parentesi
        parts = Parser.GetHeaderAndArguments(expr)
        Dim withArgs As Boolean = parts.argumentsInBrackets And parts.argn > 0
        Dim argsv As Variable() = Nothing
        If withArgs Then
            argsv = EvalMultipleExpression(parts.args)
            If errorCode Then Return Nothing ' indici scorretti
        End If

        ' ricerca funzione
        If firstCh = TOKEN_PREFIX And Not String.IsNullOrEmpty(parts.header) Then
            Dim tk As Integer = ExtractTokenFromStr(parts.header)
            If tk >= 0 Then
                Dim f As Funct = cmdSet.GetFunctionByToken(tk)
                If Not f.IsCommand And Not f.IsIstruction Then ' si tratta di una funzione
                    If parts.argn > 0 Then
                        If Not parts.argumentsInBrackets Then
                            SetError(ErrorId.INCORRECT_STATEMENT)
                            Return Nothing
                        End If
                    Else ' no arguments
                        If f.mandatoryArguments Then
                            SetError(ErrorId.INCORRECT_STATEMENT)
                            Return Nothing
                        End If
                    End If
                    cmdSet.Exec(tk, parts.joinedArguments)
                    If errorCode Then Return Nothing
                    Return functionResult
                Else ' trovato comando o istruzione (fuori luogo)
                    SetError(ErrorId.INCORRECT_STATEMENT)
                    Return Nothing
                End If
            End If
        End If

        ' ricerca nome variabile semplice
        If variables.Contains(expr) Then
            vres = variables.GetVariable(expr).CloneSimpleVarContent()
            Return CheckedVar(typeChk, vres, expectedReturnType)
        Else
            Dim vt As VarType = GetVarTypeFromName(expr)
            If vt <> VarType.UNDEF Then
                Info(hdr, "creating undefined variable " & expr & " on access")
                vres = variables.Add(expr, vt)
                Return CheckedVar(typeChk, vres, expectedReturnType)
            End If
        End If

        ' ricerca nome variabile array
        If withArgs Then
            Dim argsi As Integer() = GetIntegerArrayFromVars(argsv)
            If argsi Is Nothing Then Return Nothing ' indici non numerici
            Dim v = CheckArrayAccess(parts.header, argsi)
            If errorCode Or v Is Nothing Then Return Nothing
            vres = v.GetArrayVar(argsi)
            Return CheckedVar(typeChk, vres, expectedReturnType)
        End If

        ' fallimento: elemento non riconosciuto
        SetError(ErrorId.INCORRECT_STATEMENT)
        Return Nothing
    End Function

    ' puo' generare errore (senza output)
    Protected Function CheckedVar(ByRef typeCheckReq As Boolean,
                                 ByRef var As Variable,
                                 ByRef mandatoryType As Integer) As Variable
        If var Is Nothing Then Return Nothing
        If typeCheckReq And mandatoryType <> var.type Then
            SetError(ErrorId.STR_NUM_MISMATCH)
            Return Nothing
        End If
        If var.type = VarType.FLOAT Then
            Dim av As Double = Math.Abs(var.GetDouble())
            If av > MAX_FLOAT Then
                SetWarning(WarnId.NUMBER_TOO_BIG)
                var.value = MAX_FLOAT
            ElseIf av < MIN_FLOAT Then
                var.value = 0.0
            End If
        End If
        Return var
    End Function

    ' verifica la possibilita' di esegurie un accesso ad un elemento della variabile array
    ' secondo i parametri specificati
    ' se l'array non esiste lo crea, anche nel caso che gli indici siano fuori limiti
    ' puo' generare errori (senza output)
    ' restituisce la variabile array (preesistene o nuova), nothing in caso di problemi
    ' nota: verificare presenza errori dopo la chiamata
    Protected Function CheckArrayAccess(ByRef name As String, ByRef idxs As Integer()) As Variable
        Dim hdr = chdr & ".CheckArrayAccess"
        If String.IsNullOrEmpty(name) Then
            Warn(hdr, "null array name!")
            Return Nothing
        End If
        Dim v As Variable = variables.GetVariable(name)
        If v Is Nothing Then ' ARRAY NON DEFINITO
            If idxs.Length > 3 Then ' troppi indici
                SetError(ErrorId.INCORRECT_STATEMENT)
                Return Nothing
            End If
            Dim base As Integer
            If optionBase0 Then base = 0 Else base = 1
            If Not AreValuesLimited(idxs, base, DEF_ARRAY_DIM_SIZE) Then
                SetError(ErrorId.BAD_SUBSCRIPT)
                Return Nothing
            End If
            Dim vt As VarType = GetVarTypeFromName(name)
            v = CreateDefaultArray(name, vt, idxs.Length)
            If v Is Nothing Then
                Err(hdr, "troubles creating array " & name)
                Return Nothing
            End If
            variables.Add(v)
        Else ' ARRAY GIA' DEFINITO
            If v.isArray() And v.GetDimensions() <> idxs.Length Then ' dimensioni non corrispondenti
                SetError(ErrorId.NAME_CONFLICT)
                Return Nothing
            End If
        End If
        If Not v.ValidArrayIndexes(idxs) Then ' indici fuori limiti array
            SetError(ErrorId.BAD_SUBSCRIPT)
            Return Nothing
        End If
        Return v
    End Function

    ' verifica che tutti i valorin teri siano compresi tra minVal e maxVal
    Protected Shared Function AreValuesLimited(ByRef values As Integer(),
                                        ByRef minVal As Integer,
                                        ByRef maxVal As Integer) As Boolean
        If values Is Nothing Then Return False
        For i As Integer = 0 To values.Length - 1
            If values(i) < minVal Or values(i) > maxVal Then Return False
        Next
        Return True
    End Function

    ' crea array di dimensioni standard (quando non sono definite)
    Protected Function CreateDefaultArray(ByRef name As String,
                                          ByRef type As VarType,
                                          ByRef dimensions As Integer) As Variable
        Dim idxTop(dimensions - 1) As Integer
        For i As Integer = 0 To dimensions - 1
            idxTop(i) = DEF_ARRAY_DIM_SIZE
        Next
        Dim v As New Variable(name, type, idxTop, optionBase0)
        Return v
    End Function

    Protected Function EvalMultipleExpression(ByRef exprArray As String()) As Variable()
        If exprArray Is Nothing Then Return Nothing
        Dim l As Integer = exprArray.Length
        If l = 0 Then Return Nothing
        Dim var(l - 1) As Variable
        For i As Integer = 0 To l - 1
            var(i) = EvalExpression(exprArray(i))
        Next
        Return var
    End Function

    ''' <summary>
    ''' Esamina nome di variabile e individua il relativo tipo di dati tra STRNG e FLOAT.
    ''' Restituisce UNDEF in caso di stringa nulla o non valida (con caratteri illegali)
    ''' </summary>
    ''' <param name="vname">nome della variabile</param>
    ''' <returns>tipo della variabile: STRNG o FLOAT</returns>
    Protected Shared Function GetVarTypeFromName(ByRef vname As String) As VarType
        If String.IsNullOrEmpty(vname) Then Return VarType.UNDEF
        Dim i As Integer
        Dim ln As Integer = vname.Length
        Dim ch As Char() = vname.ToCharArray()
        For i = 0 To ln - 1
            If Not VAR_ADMITTED_CHARS.Contains(ch(i)) Then Return VarType.UNDEF
        Next
        i = vname.IndexOf("$")
        If i = ln - 1 Then Return VarType.STRNG
        Return VarType.FLOAT
    End Function

    Public Function SetError(ByRef errId As Integer, Optional ByRef optionalFinalArg As String = Nothing) As String
        errorCode = errId
        machine.sound.ErrorBeep()
        cnsl.NewLine()
        Return GetErrorMessage(optionalFinalArg)
    End Function

    Public Sub SetWarning(ByRef warnId As Integer, Optional ByRef optionalFinalArg As String = Nothing)
        warningCode = warnId
        machine.sound.ErrorBeep()
        cnsl.NewLine()
        cnsl.PrintLn("* WARNING:")
        cnsl.PrintLn(GetWarningMessage(optionalFinalArg))
        'Return GetWarningMessage(optionalFinalArg)
    End Sub

    Public Overrides Function GetErrorMessage(Optional ByRef optionalFinalArg As String = Nothing) As String
        Dim msg As String = "* " & errMsgList.GetValueOrDefault(errorCode)
        If Not String.IsNullOrEmpty(optionalFinalArg) Then msg &= " " & optionalFinalArg
        If running Then msg &= " IN " & currentLine.number.ToString()
        'errorMsg = msg
        Return msg
    End Function

    Public Overrides Function GetWarningMessage(Optional ByRef optionalFinalArg As String = Nothing) As String
        Dim msg As String = "  " & warnMsgList.GetValueOrDefault(warningCode)
        If Not String.IsNullOrEmpty(optionalFinalArg) Then msg &= " " & optionalFinalArg
        If running Then msg &= " IN " & currentLine.number.ToString()
        'warningMsg = msg
        Return msg
    End Function

    Public Sub ShowCharFont(ByRef firstRow As Integer)
        If firstRow < 0 Then firstRow = 0
        If firstRow > TiVideo.ROWS - 9 Then firstRow = TiVideo.ROWS - 9
        For i As UInt16 = 0 To 255
            machine.video.PutChar(firstRow + (i >> 5), i Mod 32, i, False)
            machine.video.Invalidate()
        Next
    End Sub

#Region "Commands"
    Protected Function CMD_Break(params As String) As String
        If String.IsNullOrEmpty(params) Then ' senza argomenti
            If running Then
                Return Break()
            Else
                Return SetError(ErrorId.CANT_DO_THAT)
            End If
        Else ' sono specificate le linee
            Dim args As Integer() = Parser.GetNumericParams(params, ",")
            Dim n As Integer
            For i As Integer = 0 To args.Length - 1
                n = args(i)
                If n > 0 Then
                    code.GetLine(n).breakpoint = True
                Else
                    Return SetError(ErrorId.BAD_LINE_NUMBER)
                End If
            Next
        End If
        Return String.Empty
    End Function

    Protected Function CMD_Continue(params As String) As String
        Cont()
        Return String.Empty
    End Function

    Protected Shared Function CMD_Bye(params As String) As String
        Application.Exit()
        Return String.Empty
    End Function

    Protected Function CMD_Edit(params As String) As String
        If String.IsNullOrEmpty(params) Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        Dim n As Integer
        Try
            n = Convert.ToInt32(params)
        Catch ex As Exception
            Return SetError(ErrorId.INCORRECT_STATEMENT)
        End Try
        Dim ln As CodeLine = code.GetLine(n)
        If ln Is Nothing Then SetError(ErrorId.BAD_LINE_NUMBER)
        Dim lnStr As String = ln.GetComplete()
        lnStr = DecodeCodeLine(lnStr)
        cnsl.PresetInputTextLine(lnStr)
        cnsl.PresetInputCursorPosition(ln.number.ToString().Length + 1)
        Return String.Empty
    End Function

    Protected Function CMD_Number(params As String) As String
        Dim start As Integer = 100
        Dim stp As Integer = 10
        If Not String.IsNullOrEmpty(params) Then
            Dim args As Integer() = Parser.GetNumericParams(params, ",")
            If args(0) = -1 Then Return SetError(ErrorId.BAD_LINE_NUMBER)
            start = args(0)
            If args(1) = -1 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
            stp = args(1)
        End If
        StartNumbering(start, stp)
        Return String.Empty
    End Function

    Protected Function CMD_Trace(params As String) As String
        trace = True
        Return String.Empty
    End Function

    Protected Function CMD_Untrace(params As String) As String
        trace = False
        Return String.Empty
    End Function

    Protected Function CMD_Run(params As String) As String
        If code.LinesCount() = 0 Then Return SetError(ErrorId.CANT_DO_THAT)
        If String.IsNullOrEmpty(params) Then
            Run()
            Return String.Empty
        End If
        Dim num As Integer
        If Information.IsNumeric(params) Then
            Try
                num = Convert.ToInt32(params)
            Catch ex As Exception
                Return SetError(ErrorId.BAD_LINE_NUMBER)
            End Try
            Run(num)
            Return String.Empty
        End If
        Return SetError(ErrorId.BAD_LINE_NUMBER)
    End Function

    Protected Function CMD_Save(params As String) As String
        If String.IsNullOrEmpty(params) Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        If params.Length >= 3 Then
            Dim strH As String = params.Substring(0, 3)
            If strH = "CS1" Or strH = "CS2" Then Return SetError(ErrorId.UNIMPLEMENTED)
            If strH = "DSK" Then
                Dim args As String() = params.Split("."c)
                If args(0).Length > 4 Then Return SetError(ErrorId.IO)
                Dim disknum As Integer
                Try
                    disknum = Convert.ToInt32(args(0).Substring(3))
                Catch ex As Exception
                    disknum = -1
                End Try
                If SaveProgram_Dsk(disknum, args(1)) Then Return Nothing ' successfull
                Return SetError(ErrorId.IO, IOError.ToString())
            End If
        End If
        ' salvataggio file in formato ASCII
        If Not SaveProgram(params) Then Return SetError(ErrorId.IO, IOError.ToString)
        Return String.Empty
    End Function

    Protected Function SaveProgram(ByRef fname As String) As Boolean
        Dim f As FileStream
        Try
            f = File.Open(My.Settings.workDir & "\" & PLAIN_TEXT_SUBDIR & "\" & fname & ".txt", FileMode.Create)
        Catch ex As Exception
            IOError = 66
            Return False
        End Try
        Dim sw As New StreamWriter(f)
        List(New Interval(0, MAX_LINE_NUMBER), sw)
        sw.Flush()
        f.Close()
        IOError = 0
        Return True
    End Function

    Protected Function CMD_Old(params As String) As String
        If String.IsNullOrEmpty(params) Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        If params.Length >= 3 Then
            Dim strH As String = params.Substring(0, 3)
            If strH = "CS1" Or strH = "CS2" Then Return SetError(ErrorId.UNIMPLEMENTED)
            If strH = "DSK" Then ' disk file
                Dim args As String() = params.Split("."c)
                If args(0).Length > 4 Then Return SetError(ErrorId.IO)
                Dim disknum As Integer
                Try
                    disknum = Convert.ToInt32(args(0).Substring(3))
                Catch ex As Exception
                    disknum = -1
                End Try
                If LoadProgram_Dsk(disknum, args(1)) Then Return Nothing ' successfull
                Return SetError(ErrorId.IO, IOError.ToString())
            End If
        End If
        If Not LoadProgram(params.ToUpper()) Then Return SetError(ErrorId.IO, IOError.ToString())
        Return Nothing
    End Function

#Region "Files"
    Protected Function LoadProgram(ByRef fname As String) As Boolean
        Dim hdr As String = chdr & ".LoadProgram"
        Dim f As FileStream
        Try
            f = File.Open(My.Settings.workDir & "\" & PLAIN_TEXT_SUBDIR & "\" & fname & ".txt", FileMode.Open)
        Catch ex As Exception
            Warn(hdr, ex.Message)
            IOError = 56
            Return False
        End Try

        code.Clear()
        ClearVariables()

        Dim sw As New StreamReader(f)
        Dim ln As String
        While Not sw.EndOfStream
            Try
                ln = sw.ReadLine()
            Catch ex As Exception
                IOError = 56
                Return False
            End Try
            ReadProgramLine(ln)
        End While
        f.Close()

        Return True
    End Function

    Protected Function LoadProgram_Dsk(ByRef diskNumber As Integer, ByRef fname As String) As Boolean
        Dim hdr As String = chdr & ".LoadProgram_Dsk"
        Dim prg As Byte() = TiFiles.LoadProgram(diskNumber, fname)
        If prg Is Nothing Then Return False

        Dim h As ProgramFileHeader

        ' lettura puntatori
        h.chkWord = GetWordFromBytes(prg, 0)
        h.lineNumTable_End = GetWordFromBytes(prg, 2)
        h.lineNumTable_Start = GetWordFromBytes(prg, 4)
        h.programLines_End = GetWordFromBytes(prg, 6)

        Dim lineNumberTableSize As UInt16 = h.lineNumTable_End - h.lineNumTable_Start + 1
        Dim statementListSize As UInt16 = h.programLines_End - h.lineNumTable_End
        Dim linesCount As UInt16 = lineNumberTableSize >> 2
        Dim lineNum As Integer ' line number, code line length
        Dim ptr As UInt16
        'Dim codeBytes As Byte()
        Dim lineOfCode As String

        ' cancellazione memoria
        code.Clear()
        ClearVariables()

        'lettura linee di codice
        Dim i As Integer
        For n = 0 To linesCount - 1
            ' lettura numero di linea e puntatore a codice
            i = 8 + n * 4
            lineNum = GetWordFromBytes(prg, i)
            ptr = GetWordFromBytes(prg, i + 2)
            lineOfCode = CreateCodeLineFromFileBytes(prg, ptr - h.lineNumTable_Start + 8)
            If lineOfCode Is Nothing Then
                Warn(hdr, "can't read code of line " & lineNum.ToString())
            ElseIf lineOfCode.Chars(0) = "*" Then
                Warn(hdr, "illegal token in line " & lineNum.ToString())
                lineOfCode = lineOfCode.Substring(1)
            End If
            If Not code.AddLine(lineNum, lineOfCode) Then
                Warn(hdr, "can't store line " & lineNum.ToString() & " " & lineOfCode)
            End If
        Next

        Return True
    End Function

    Protected Function SaveProgram_Dsk(ByRef diskNumber As Integer, ByRef fname As String,
                                       Optional ByRef memoryTop As UInt16 = &H37D7,
                                       Optional ByRef protection As Boolean = False) As Boolean
        Dim hdr As String = chdr & ".SaveProgram_Dsk"

        ' prepare the program content
        Dim prgBody As New MemoryStream()
        Dim linePosition(code.LinesCount()) As Integer
        Dim cl As CodeLine = code.GetFirstLine()
        Dim i As UInt32 = 0
        Do
            linePosition(i) = prgBody.Position
            WriteFileBytesFromCodeLine(prgBody, cl.content)
            cl = cl.nextLine
            i += 1
        Loop Until cl Is Nothing

        ' get sizes
        Dim prgBodySize As Integer = prgBody.Position - 1
        Dim lineNumberTableSize As Integer = code.LinesCount() * 4
        Dim prgSize As Integer = 8 + lineNumberTableSize + prgBodySize
        If memoryTop < prgSize Then ' program too large!
            'TODO
            Err(hdr, "program too large")
            errorCode = ErrorId.MEMORY_FULL
            Return False
        End If

        ' prepare the line number table 
        Dim prgBodyBegin As UInt32 = memoryTop - prgBodySize
        Dim lineNumberTable(lineNumberTableSize) As Byte
        Dim lnum, lptr As UInt16
        i = 0
        cl = code.GetFirstLine()
        Do
            lnum = cl.number
            lptr = prgBodyBegin + linePosition(i >> 2)
            lineNumberTable(i) = GetHighByte(lnum)
            lineNumberTable(i + 1) = GetLowByte(lnum)
            lineNumberTable(i + 2) = GetHighByte(lptr + 1)
            lineNumberTable(i + 3) = GetLowByte(lptr + 1)
            cl = cl.nextLine
            i += 4
        Loop Until cl Is Nothing

        ' define header values
        Dim lineNumberTableBegin As UInt32 = prgBodyBegin - lineNumberTableSize
        Dim lineNumberTableEnd As UInt32 = prgBodyBegin - 1
        Dim magicValue As UInt16 = Convert.ToUInt16(lineNumberTableBegin Xor lineNumberTableEnd)
        If protection Then magicValue = Not magicValue

        ' put parts together
        Dim prg(prgSize) As Byte
        prg(0) = GetHighByte(magicValue)
        prg(1) = GetLowByte(magicValue)
        prg(2) = GetHighByte(lineNumberTableEnd)
        prg(3) = GetLowByte(lineNumberTableEnd)
        prg(4) = GetHighByte(lineNumberTableBegin)
        prg(5) = GetLowByte(lineNumberTableBegin)
        prg(6) = GetHighByte(memoryTop)
        prg(7) = GetLowByte(memoryTop)
        Array.Copy(lineNumberTable, 0, prg, 8, lineNumberTableSize)
        prgBody.Position = 0
        For i = 8 + lineNumberTableSize To prgSize - 1
            prg(i) = prgBody.ReadByte()
        Next

        prgBody.Dispose()
        Return TiFiles.SaveProgram(diskNumber, fname, prg)
    End Function

    ' if byte sequence contains an illegal token, the return string will have prefix "*"
    Protected Function CreateCodeLineFromFileBytes(ByRef bytes As Byte(), Optional ByRef ptr As Integer = 0) As String
        Dim hdr = chdr & ".GetLineCodeFromFileBytes"
        Dim st As String
        Dim res As String = ""
        Dim b As Byte
        Dim ln As Integer = bytes.Length
        Dim i, l As Integer
        Dim illegalToken As Boolean = False

        If ln = 0 Then
            Warn(hdr, "empty bytes buffer passed")
            Return Nothing
        End If

        Do
            b = bytes(ptr)
            If b = 0 Then
                ptr = 0
                Exit Do ' end of line
            End If
            If b >= &H80 Then ' token
                Select Case b
                    Case &HC7 ' quoted string
                        l = bytes(ptr + 1)
                        st = ""
                        For i = 1 To l
                            st &= ChrW(bytes(ptr + i + 1))
                        Next
                        ptr += l + 2
                        res &= Chr(34) & st & Chr(34)
                    Case &HC8 ' unquoted string
                        l = bytes(ptr + 1)
                        For i = 1 To l
                            res &= ChrW(bytes(ptr + i + 1))
                        Next
                        ptr += l + 2
                    Case &HC9 ' line number
                        l = bytes(ptr + 1) * 256 + bytes(ptr + 2)
                        ptr += 3
                        res &= l.ToString()
                    Case Else ' token byte
                        If b >= &HB3 And b <= &HC5 Then ' 1 char keyword
                            If auxTokens.ContainsValue(b) Then
                                'For Each c As Char In auxTokens.Keys
                                '    If auxTokens.GetValueOrDefault(c) = b Then
                                '        res &= c
                                '        Exit For
                                '    End If
                                'Next
                                For Each vp As KeyValuePair(Of Char, Integer) In auxTokens
                                    If vp.Value = b Then
                                        res &= vp.Key
                                        Exit For
                                    End If
                                Next
                            Else
                                Warn(hdr, "can't convert token h" & Hex(b))
                                res &= Chr(b)
                            End If
                            ptr += 1
                        Else ' normal keyword
                            Dim f As Funct = cmdSet.GetFunctionByToken(b)
                            If f Is Nothing Then '  illegal token
                                Warn(hdr, "illegal token " & b.ToString() & " at position " & ptr.ToString())
                                illegalToken = True
                                res &= Chr(b)
                            Else
                                If f.IsFunction() Then
                                    st = GetTokenCodeStr(b)
                                Else
                                    st = " " & GetTokenCodeStr(b) & " " ' space padding
                                End If
                                res &= st
                            End If
                            ptr += 1
                        End If
                End Select
            Else ' simple character
                res &= Chr(b)
                ptr += 1
            End If
        Loop While ptr < ln

        If illegalToken Then res = "*" & res
        If ptr > 0 Then Warn(hdr, "unexpected end of file")
        If res.Length = 0 Then Warn(hdr, "empty string result")
        Return res.Trim()
    End Function

    Protected Function WriteFileBytesFromCodeLine(ByRef f As MemoryStream, ByRef codeLine As String) As Boolean
        Dim hdr As String = chdr & ".WriteFileBytesFromCodeLine"
        Dim startPos As Long = f.Position
        Dim ch As Char
        Dim b As Byte
        Dim prevToken As Integer = -1
        Dim err As Boolean = False
        Dim i As Integer = 0
        Dim j As Integer

        If String.IsNullOrEmpty(codeLine) Then
            Util.Err(hdr, "empty code line")
            Return False
        End If

        Info(hdr, "parsing line: " & codeLine)
        f.WriteByte(0) ' line size: will be defined later
        Do
            ch = codeLine.Chars(i)
            b = Convert.ToByte(ch)
            i += 1

            If ch = """"c Then ' QUOTED STRING
                If i = codeLine.Length - 1 Then ' quotes as last char
                    f.WriteByte(34)
                    Warn(hdr, "opening quotes at end of encoded line " & codeLine)
                    err = True
                    Exit Do
                Else ' normal case
                    f.WriteByte(&HC7) ' quoted string token
                    j = codeLine.IndexOf(""""c, i)
                    If j = -1 Then
                        err = True
                        j = codeLine.Length
                        Warn(hdr, "missing closing quotes in line " & codeLine)
                    End If
                    f.WriteByte(j - i) ' quoted string length
                    Info(hdr, "- quoted string: " & codeLine.Substring(i, j - i))
                    While i < j
                        f.WriteByte(Convert.ToByte(codeLine.Chars(i)))
                        i += 1
                    End While
                    i += 1
                    prevToken = -1
                End If
                Continue Do
            End If

            If b = 32 Then
                Info(hdr, "- space (ignored)")
                Continue Do ' space
            End If

            If auxTokens.ContainsKey(ch) Then
                b = auxTokens.GetValueOrDefault(ch)
                f.WriteByte(b)
                Info(hdr, "- 1-char token " & b.ToString() & " for '" & ch & "'")
                prevToken = -1
                Continue Do
            ElseIf ch = TOKEN_PREFIX Then ' TOKEN for >1 chars keyword
                If i >= codeLine.Length Then ' token prefix as last char = not converted
                    Info(hdr, "- token prefix '" & TOKEN_PREFIX & "' as last char (not converted)")
                    f.WriteByte(Convert.ToByte(TOKEN_PREFIX))
                    Exit Do
                Else ' normal condition
                    ch = codeLine.Chars(i)
                    i += 1
                    'b = Convert.ToByte(ch)
                    b = Asc(ch)
                    b -= TOKEN_BASECODE ' token
                    f.WriteByte(b)
                    If cmdSet.ContainsToken(b) Then
                        Info(hdr, "- token " & b.ToString() & " ('" & cmdSet.GetKeywordByToken(b) & "')")
                        Dim withLineNumber As Boolean = False
                        If b = IstrId.GO_TO Or b = IstrId.GO_SUB Or b = IstrId._THEN Or b = IstrId._ELSE Then
                            withLineNumber = True
                        ElseIf b = IstrId._TO Or b = IstrId._SUB Then
                            If prevToken = IstrId.GO Then withLineNumber = True
                        ElseIf b = IstrId.RESTORE Then
                            If i < codeLine.Length Then withLineNumber = True
                        End If
                        If withLineNumber Then
                            f.WriteByte(&HC9)
                            Dim n As UInt32 = Parser.ExtractUIntegerFromString(codeLine, i) ' increments i
                            Info(hdr, "- line number " & n.ToString())
                            If n < 1 Or n > MAX_LINE_NUMBER Then
                                Warn(hdr, "illegal reference line number in line " & codeLine)
                                f.WriteByte(0)
                                f.WriteByte(0)
                                err = True
                            End If
                            f.WriteByte(Convert.ToByte((n And &HFF00) >> 8))
                            f.WriteByte(Convert.ToByte(n And &HFF))
                        End If
                    Else ' illegal token!
                        Warn(hdr, "illegal token " & b.ToString & " @" & (i - 1).ToString & " in line " & codeLine)
                        err = True
                    End If
                    prevToken = b
                End If
                Continue Do
            End If

            ' UNQUOTED STRING
            j = Parser.GetNumericStringLength(codeLine, i - 1)
            If j > 0 Then ' numeric constant
                Info(hdr, "- unquoted string: numeric: " & codeLine.Substring(i - 1, j) & " (" & j.ToString() & ")")
                f.WriteByte(&HC8) ' unquoted string token
                f.WriteByte(j) ' string length
                f.WriteByte(b)
                j += i - 1
                While i < j
                    f.WriteByte(Convert.ToByte(codeLine.Chars(i)))
                    i += 1
                End While
                prevToken = -1
                Continue Do
            ElseIf prevToken = IstrId._CALL Then ' call-routine name
                f.WriteByte(&HC8) ' unquoted string token
                i -= 1
                j = i ' first char position
                While j < codeLine.Length And b <> &HB7 And b <> 32
                    b = Convert.ToByte(codeLine.Chars(j))
                    j += 1
                End While
                Dim l As Integer = j - i
                Info(hdr, "- unquoted string: funciton name: " & codeLine.Substring(i, l) & " (" & l.ToString() & ")")
                f.WriteByte(l) ' string length
                While i < j
                    f.WriteByte(Convert.ToByte(codeLine.Chars(i)))
                    i += 1
                End While
                prevToken = -1
                Continue Do
            End If

            ' VARIABLE NAMES (not encoded)
            'f.WriteByte(b)
            j = 0 ' var name length
            While VAR_ADMITTED_CHARS.Contains(ch)
                f.WriteByte(Convert.ToByte(ch))
                j += 1
                If i = codeLine.Length Then
                    i += 1
                    Exit While
                End If
                ch = codeLine.Chars(i)
                i += 1
            End While
            If j > 0 Then
                i -= 1
                Info(hdr, "- var name (not encoded): " & codeLine.Substring(i - j, j) & " (" & j.ToString() & ")")
            Else
                Warn(hdr, "unrecognized char '" & ch & "' ignored")
                err = True
            End If
            prevToken = -1

        Loop Until i >= codeLine.Length

        ' update line size byte
        i = f.Position
        f.Seek(startPos, SeekOrigin.Begin)
        f.WriteByte(Convert.ToByte(i - startPos))
        f.Seek(i, SeekOrigin.Begin)
        f.WriteByte(0) ' EOL

        Return Not err
    End Function

#End Region

    Protected Function CMD_Test(params As String) As String
        Dim i As Integer
        Dim cmd As String
        Dim args As String() = Nothing
        Dim argn As Integer = 0

        If String.IsNullOrEmpty(params) Then
            cmd = "HELP"
        Else
            args = params.Split(" ")
            cmd = args(0)
            argn = args.Length - 1
        End If

        cnsl.NewLine()
        Select Case cmd
            Case "LIST" ' esegue list con tokens
                If Not code.IsEmpty() Then
                    Dim ln As CodeLine = code.GetFirstLine()
                    Do
                        'consl.PrintLn(ln.GetComplete())
                        cnsl.PrintLn(ln.number.ToString & " " & GetRawCodeLine(ln.content))
                        ln = ln.nextLine
                    Loop Until ln Is Nothing
                End If
                cnsl.PrintLn("-- " & code.LinesCount() & " LINES")
            Case "VARS" ' elenco delle variabili definite e relativi valori
                If variables.Count > 0 Then
                    Dim vlist As List(Of String) = variables.GetDescrList()
                    For Each v As String In vlist
                        cnsl.PrintLn(v)
                    Next
                End If
                cnsl.PrintLn("TOTAL: " & variables.Count.ToString & " VARIABLES")
                'consl.PrintLn("       " & arrays.Count.ToString & " ARRAYS")
            Case "COLR"
                cnsl.PrintLn("SCREEN COLOR: " & machine.video.GetScreenColor().ToString)
                For i = 1 To 16
                    cnsl.PrintLn("CH.SET #" & i.ToString & " COLORS: " &
                                    machine.video.GetGroupForeColor(i).ToString & "," &
                                    machine.video.GetGroupBackColor(i).ToString)
                Next
            Case "VER"
                cnsl.PrintLn("FAKETI by Fabrizio Volpi")
                cnsl.PrintLn("fabvolpi@gmail.com")
                cnsl.PrintLn("Version: 0.0")
            Case "FONT"
                'machine.video.ShowActiveFontBitmap(10)
                ShowCharFont(10)
            Case "BEEP"
                If argn > 0 Then
                    Try
                        Dim f As Integer = Convert.ToInt32(args(1))
                        machine.sound.Tone(f, 200)
                    Catch ex As Exception
                        Return SetError(ErrorId.BAD_ARGUMENT)
                    End Try
                Else
                    machine.sound.Beep()
                End If
            Case "CHRS"
                Dim rnd As New Random()
                For i = 1 To 100
                    machine.video.PutChar(rnd.Next(TiVideo.ROWS), rnd.Next(TiVideo.COLS), Convert.ToByte(32 + rnd.Next(96)))
                Next
            Case "SWCS"
                cnsl.Print("Switched to ")
                If machine.video.IsAltCharSetActive() Then
                    machine.video.SetScreenColor(TiVideo.DEF_BACKCOLOR)
                    machine.video.SwitchToDefaultCharSet()
                    cnsl.Print("default ")
                Else
                    machine.video.SetScreenColor(DEF_RUN_SCRCOLOR)
                    machine.video.SwitchToAltCharSet()
                    cnsl.Print("runtime ")
                End If
                cnsl.PrintLn("char bitmap")
            Case "BRK"
                cnsl.PrintLn("Active breakpoints:")
                If code.IsEmpty() Then
                    cnsl.PrintLn("NONE (empty program!)")
                    Return String.Empty
                End If
                Dim cl As CodeLine = code.GetFirstLine()
                i = 0
                Do
                    If cl.breakpoint Then
                        If i > 0 Then cnsl.Print(",")
                        cnsl.Print(cl.number.ToString)
                        i += 1
                    End If
                    cl = cl.nextLine
                Loop Until cl Is Nothing
                If i = 0 Then
                    cnsl.PrintLn("NO breakpoints.")
                Else
                    cnsl.NewLine()
                    cnsl.PrintLn("Total: " & i.ToString & " breakpoints.")
                End If
            Case Else
                cnsl.PrintLn("ADD AN ARGUMENT:")
                cnsl.PrintLn("---------------")
                cnsl.PrintLn("LIST  show encoded program")
                cnsl.PrintLn("VARS show defined variables")
                cnsl.PrintLn("BRK  show breakpoints")
                cnsl.PrintLn("VER  show software version")
                cnsl.PrintLn("COLR print active colors")
                cnsl.PrintLn("FONT show char font&colors")
                cnsl.PrintLn("CHRS show random chars")
                cnsl.PrintLn("BEEP [freq] generate tone")
                cnsl.PrintLn("SWCS switch char bitmaps")
        End Select
        Return String.Empty
    End Function
#End Region

#Region "Istructions"

    Private Function ISTR_Let(codeStr As String) As String
        Dim hdr = chdr & ".ISTR_Let"

        ' controlli
        If String.IsNullOrEmpty(codeStr) Then
            Warn(hdr, "empty assignment string")
            Return String.Empty ' no operation
        End If
        codeStr = codeStr.Replace(" ", String.Empty)
        Dim l As Integer = codeStr.Length()
        Dim ei As Integer = codeStr.IndexOf("=")
        If ei = 0 Then
            Info(hdr, "missing variable name")
            Return SetError(ErrorId.INCORRECT_STATEMENT)
        End If
        If ei = l - 1 Then
            Info(hdr, "missing expression")
            Return SetError(ErrorId.INCORRECT_STATEMENT)
        End If

        Dim vName As String = codeStr.Substring(0, ei).Trim()
        Dim vExpr As String = codeStr.Substring(ei + 1).Trim()
        Dim err As Integer = SetVariable(vName, vExpr)
        If err Then Return SetError(err)
        Return String.Empty
    End Function

    ' assegna il valore risultato dall'espressione <expr> alla variabile specificata <varName>
    ' Se la variabile non esiste, la crea.
    ' in caso di errore non genera output ma restituisce il relativo codice errore
    Protected Function SetVariable(ByVal varName As String, ByRef expr As String) As Integer
        Dim hdr = chdr & ".SetVariable"

        ' lettura valore espressione
        Dim exprVal As Variable = EvalExpression(expr)
        If errorCode Then Return errorCode

        ' lettura nome variabile
        Dim isMatrix As Boolean
        Dim ha As HeaderAndArguments = Parser.GetHeaderAndArguments(varName)
        If ha.argn = 0 And Not ha.argumentsInBrackets Then ' variabile semplice
            isMatrix = False
        ElseIf ha.argumentsInBrackets And ha.argn > 0 Then ' variabile di matrice
            isMatrix = True
            varName = ha.header
        Else
            Return ErrorId.INCORRECT_STATEMENT
        End If
        Dim varTypeByName As VarType = GetVarTypeFromName(varName)
        If varTypeByName = VarType.UNDEF Then
            Info(hdr, "bad variable name " & varName)
            Return ErrorId.BAD_NAME
        End If
        If exprVal.type <> varTypeByName Then Return ErrorId.STR_NUM_MISMATCH

        ' assegnazione valore a variabile
        Dim destVar As Variable
        If isMatrix Then ' variabile di matrice
            Dim idxv As Variable() = EvalMultipleExpression(ha.args)
            If errorCode Then Return errorCode ' indici scorretti
            Dim idxi As Integer() = GetIntegerArrayFromVars(idxv)
            If idxi Is Nothing Then Return ErrorId.STR_NUM_MISMATCH ' indici non numerici
            destVar = CheckArrayAccess(varName, idxi)
            If errorCode Then Return errorCode
            If Not destVar.SetArrayVar(idxi, exprVal) Then
                Err(hdr, "can't set array variable " & varName & " @ " & idxi.ToString)
                Return ErrorId.SIMULATOR_ERROR
            End If
        Else ' variabile semplice
            destVar = variables.GetVariable(varName)
            If destVar IsNot Nothing Then ' variabile gia' definita: aggiornamento ---
                If destVar.type <> exprVal.type Then ' non corrispondenza di tipo
                    Return ErrorId.STR_NUM_MISMATCH
                End If
            Else ' creazione nuova variabile ---
                destVar = New Variable(varName, exprVal.type)
                variables.Add(destVar)
            End If
            destVar.value = exprVal.value
        End If

        Return ErrorId.NONE
    End Function

    Protected Shared Function ISTR_NoOp(params As String) As String
        Return String.Empty 'do nothing
    End Function

    Protected Function ISTR_Print(params As String) As String
        If Not String.IsNullOrEmpty(params) Then
            If params.Chars(0) = "#"c Then
                If currentToken = IstrId.DISPLAY Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                ' TODO...
            End If
        End If
        Return PrintOnScreen(params)
    End Function

    Protected Function PrintOnScreen(ByRef prList As String) As String
        Dim i As Integer
        Dim sep As Char
        Dim res As Variable
        Dim subStr As String
        Dim finalNewLine As Boolean = True

        If String.IsNullOrEmpty(prList) Then
            cnsl.NewLine()
            Return String.Empty
        End If
        printIstrEval = True

        Do ' esamina uno a uno gli argomenti tra separatori
            i = FindFirstPrintSeparator(prList)
            If i = -1 Then ' nessun separatore
                res = EvalExpression(prList)
                If errorCode Then
                    printIstrEval = False
                    Return GetErrorMessage()
                End If
                If res Is Nothing Then cnsl.Print("?") Else cnsl.Print(res.GetString())
                Exit Do
            End If

            If i > 0 Then
                subStr = prList.Substring(0, i)
                res = EvalExpression(subStr)
                If errorCode Then
                    printIstrEval = False
                    Return GetErrorMessage()
                End If
                If res Is Nothing Then cnsl.Print("?") Else cnsl.Print(res.GetString())
            End If

            sep = prList.Chars(i)
            Select Case sep
                Case ":"c
                    cnsl.NewLine()
                Case ","c
                    cnsl.HalfRowTab()
                Case ";"c
                    If i = prList.Length - 1 Then finalNewLine = False
            End Select

            If i < prList.Length - 1 Then prList = prList.Substring(i + 1) Else Exit Do
        Loop While prList.Length > 0

        If finalNewLine And cnsl.GetCursorPosition() > 0 Then cnsl.NewLine()
        printIstrEval = False
        Return String.Empty
    End Function

    ' individua la prima occorrenza di ":" o ";" o "," all'interno della strina <str> che non sia racchiusa tra apici (")
    Protected Shared Function FindFirstPrintSeparator(ByRef referenceStr As String) As Integer
        If String.IsNullOrEmpty(referenceStr) Then Return -1
        Dim i As Integer
        Dim ch As Char
        Dim quotes As Char = Chr(34)
        Dim textContent As Boolean = False
        For i = 0 To referenceStr.Length - 1
            ch = referenceStr.Chars(i)
            If ch = quotes Then
                textContent = Not textContent
                Continue For
            End If
            If Not textContent Then
                If ch = ";"c Or ch = ":"c Or ch = ","c Then Return i
            End If
        Next i
        Return -1
    End Function

    Protected Function ISTR_End(params As String) As String
        If Not running Then breakState = False
        StopRun(True)
        Return String.Empty
    End Function

    Protected Function ISTR_Dim(params As String) As String
        Dim hdr As String = chdr & ".ISTR_Dim"
        Dim args As HeaderAndArguments = Parser.GetHeaderAndArguments(params)
        If Not args.argumentsInBrackets Then
            Info(hdr, "missing brackets in " & params)
            Return SetError(ErrorId.INCORRECT_STATEMENT)
        End If
        If Not args.onlyNumericArguments Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        If variables.Contains(args.header) Then Return SetError(ErrorId.NAME_CONFLICT)
        Dim varTypeByName As VarType = GetVarTypeFromName(args.header)
        If varTypeByName = VarType.UNDEF Then
            Info(hdr, "bad variable name " & args.header)
            Return SetError(ErrorId.BAD_NAME)
        End If
        If args.argn = 0 Or args.argn > 3 Then
            Info(hdr, "bad matrix dimension count: " & args.argn.ToString())
            Return SetError(ErrorId.INCORRECT_STATEMENT)
        End If

        Dim sz(args.argn - 1) As Integer
        For i As Integer = 0 To args.argn - 1
            Try
                sz(i) = Convert.ToInt32(args.args(i))
            Catch ex As Exception
                Info(hdr, "bad matrix dimension definition " & args.args(i) & " in " & params)
                Return SetError(ErrorId.INCORRECT_STATEMENT)
            End Try
            If sz(i) = 0 And Not optionBase0 Then
                Info(hdr, "bad size value " & sz(i) & " in " & params)
                Return SetError(ErrorId.BAD_VALUE)
            End If
        Next

        'Dim arr As New ArrayVar(args.header, varTypeByName, sz, optionBase0)
        'arrays.Add(arr)
        variables.AddArray(args.header, varTypeByName, sz, optionBase0)
        Return String.Empty
    End Function

    Protected Function ISTR_Option(params As String) As String
        Dim args As String() = params.Split(" "c, StringSplitOptions.TrimEntries)
        If args(0) <> "BASE" Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        If Not running Then Return SetError(ErrorId.CANT_DO_THAT)
        If args(1) = "0" Then
            optionBase0 = True
        ElseIf args(1) = "1" Then
            optionBase0 = False
        Else
            Return SetError(ErrorId.INCORRECT_STATEMENT)
        End If
        Return String.Empty
    End Function

    Protected Function ISTR_If(params As String) As String
        If String.IsNullOrEmpty(params) Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        Dim i As Integer = params.IndexOf(GetTokenCodeStr(IstrId._THEN))
        If i <= 0 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        Dim condStr, ln1Str, ln2Str As String

        Try
            condStr = params.Substring(0, i).Trim() ' condition string
            ln1Str = params.Substring(i + 2).Trim()
        Catch ex As Exception
            Return SetError(ErrorId.INCORRECT_STATEMENT)
        End Try
        If String.IsNullOrEmpty(ln1Str) Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        Dim condVar As Variable = EvalExpression(condStr)
        If errorCode Then Return GetErrorMessage()
        Dim trueCondition As Boolean = (condVar.value <> 0.0)

        i = ln1Str.IndexOf(GetTokenCodeStr(IstrId._ELSE))
        If i < 0 Then ' non c'è ELSE
            If trueCondition Then Return ISTR_Goto(ln1Str)
            Return String.Empty
        End If
        ' c'è ELSE
        Try
            ln2Str = ln1Str.Substring(i + 2).Trim()
            ln1Str = ln1Str.Substring(0, i)
        Catch ex As Exception
            Return SetError(ErrorId.INCORRECT_STATEMENT)
        End Try
        If trueCondition Then Return ISTR_Goto(ln1Str)
        Return ISTR_Goto(ln2Str)
    End Function

    Protected Function ISTR_Go(params As String) As String
        If String.IsNullOrEmpty(params) Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        Dim args As String() = params.Split(" "c, StringSplitOptions.TrimEntries)
        If args.Length <> 2 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        If args(0) = GetTokenCodeStr(IstrId._TO) Then
            ISTR_Goto(args(1))
        ElseIf args(0) = GetTokenCodeStr(IstrId._SUB) Then
            ISTR_Gosub(args(1))
        Else
            Return SetError(ErrorId.INCORRECT_STATEMENT)
        End If
        Return String.Empty
    End Function

    Protected Function ISTR_Goto(params As String) As String
        Dim ln As Integer
        Try
            ln = Convert.ToInt32(params)
        Catch ex As Exception
            Return SetError(ErrorId.BAD_LINE_NUMBER)
        End Try
        nextLine = code.GetLine(ln)
        If nextLine Is Nothing Then
            Return SetError(ErrorId.BAD_LINE_NUMBER)
        End If
        Return String.Empty
    End Function

    Protected Function ISTR_Gosub(params As String) As String
        Dim ln As Integer
        Try
            ln = Convert.ToInt32(params)
        Catch ex As Exception
            Return SetError(ErrorId.BAD_LINE_NUMBER)
        End Try
        nextLine = code.GetLine(ln)
        If nextLine Is Nothing Then
            Return SetError(ErrorId.BAD_LINE_NUMBER)
        End If
        gosubCalls.Add(currentLine)
        Return String.Empty
    End Function

    Protected Function ISTR_Return(params As String) As String
        If gosubCalls.Count = 0 Then Return SetError(ErrorId.CANT_DO_THAT)
        Dim ln As CodeLine = gosubCalls.Last()
        nextLine = ln.nextLine
        gosubCalls.Remove(ln)
        Return String.Empty
    End Function

    Protected Function ISTR_OnGotoGosub(params As String) As String
        Dim hdr As String = chdr & "ISTR_OnGotoGosub"
        Dim isGosub As Boolean
        Dim i As Integer
        Dim tkstr As String

        ' riconoscimento caso tra ON-GOTO / ON-GOSUB
        tkstr = GetTokenCodeStr(IstrId.GO_TO)
        i = params.IndexOf(tkstr)
        If i = -1 Then
            tkstr = GetTokenCodeStr(IstrId.GO_SUB)
            i = params.IndexOf(tkstr)
            If i = -1 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
            isGosub = True
        Else
            isGosub = False
        End If

        ' estrazione parti: espressione ed elenco numeri di linea
        Dim expr As String = params.Substring(0, i).Trim()
        Dim numList As String = params.Substring(i + 2).Trim()

        ' valutazione espressione e conversione a intero
        Dim v As Variable = EvalExpression(expr)
        If v Is Nothing Then Return SetError(ErrorId.SYNTAX_ERROR)
        If v.type = VarType.STRNG Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        Try
            i = v.GetInteger()
        Catch ex As Exception
            Err(hdr, "can't convert expression value in integer")
            Return SetError(ErrorId.SIMULATOR_ERROR)
        End Try

        ' estrazione numeri di linea
        Dim nums As Integer() = Parser.GetNumericParams(numList, ",")
        If nums Is Nothing Then Return SetError(ErrorId.BAD_VALUE)
        If nums.Length = 0 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        If i < 1 Or i > nums.Length Then Return SetError(ErrorId.BAD_VALUE)

        ' determinazione numero di linea di destinazione 
        Dim ln As Integer = nums(i - 1)
        nextLine = code.GetLine(ln)
        If nextLine Is Nothing Then
            Return SetError(ErrorId.BAD_LINE_NUMBER)
        End If
        If isGosub Then gosubCalls.Add(currentLine)
        Return String.Empty
    End Function

    Protected Function ISTR_For(params As String) As String
        Dim hdr As String = chdr & ".ISTR_For"

        ' ricerca argomenti intorno a "TO"
        Dim toStr As String = GetTokenCodeStr(IstrId._TO)
        If Not params.Contains(toStr) Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        Dim args As String() = params.Split(toStr, StringSplitOptions.TrimEntries)
        If args.Length <> 2 Then Return SetError(ErrorId.INCORRECT_STATEMENT)

        ' decodifica iniziale istruzione 
        Dim assing As String = args(0) ' V = X
        Dim tail As String = args(1) ' Y [STEP Z]
        Dim i As Integer = assing.IndexOf("=")
        If i <= 0 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        Dim varName As String = assing.Substring(0, i).Trim()

        ' controllo esistenza corrispondente NEXT
        Dim nextIstr As String = GetTokenCodeStr(IstrId._NEXT) & " " & varName
        Dim nl As CodeLine = FindCodeLineWithHeader(nextIstr, currentLine.nextLine)
        If nl Is Nothing Then
            Info(hdr, "missing NEXT for program line " & currentLine.ToString)
            Return SetError(ErrorId.FOR_NEXT)
        End If

        ' assegnazione iniziale variabile
        ISTR_Let(assing)
        If errorCode Then Return GetErrorMessage()

        Dim f As ForCycle
        f.beginLine = currentLine
        f.var = variables.GetVariable(varName)

        ' decodifica valore finale e step
        i = tail.IndexOf(GetTokenCodeStr(IstrId._STEP))
        Dim toExpr, stepExpr As String
        If i = -1 Then ' nessuno step
            toExpr = tail.Trim()
            stepExpr = Nothing
        Else
            toExpr = tail.Substring(0, i).Trim()
            stepExpr = tail.Substring(i + 3).Trim()
        End If
        Dim tempVar As Variable
        tempVar = EvalExpression(toExpr)
        If errorCode Then Return GetErrorMessage()
        f.endValue = tempVar.GetDouble()
        If stepExpr Is Nothing Then
            f.stepValue = 1.0
        Else
            tempVar = EvalExpression(stepExpr)
            If errorCode Then Return GetErrorMessage()
            If tempVar.GetDouble() = 0 Then Return (SetError(ErrorId.BAD_VALUE))
            f.stepValue = tempVar.GetDouble()
        End If

        ' registra ciclo FOR e procede
        forLoops.Add(f)
        Return String.Empty
    End Function

    Protected Function ISTR_Next(params As String) As String
        Dim var As Variable = variables.GetVariable(params)
        If var Is Nothing Or forLoops.Count = 0 Then Return SetError(ErrorId.FOR_NEXT)
        Dim f As ForCycle = forLoops.Last()
        If Not f.var.Equals(var) Then Return SetError(ErrorId.FOR_NEXT)
        Dim v As Double = var.GetDouble() + f.stepValue
        var.value = v
        If (f.stepValue > 0 And v <= f.endValue) Or (f.stepValue < 0 And v >= f.endValue) Then
            nextLine = f.beginLine.nextLine
        Else ' end of cycle
            forLoops.Remove(f)
        End If
        Return String.Empty
    End Function

    Protected Function ISTR_Read(params As String) As String
        If String.IsNullOrEmpty(params) Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        Dim args As String() = params.Split(","c)
        Dim v, dv As Variable
        Dim vt As VarType
        For Each arg As String In args
            v = variables.GetVariable(arg)
            If v Is Nothing Then
                vt = GetVarTypeFromName(arg)
                If vt = VarType.UNDEF Then Return SetError(ErrorId.DATA_ERROR)
                v = New Variable(arg, vt)
                variables.Add(v)
            Else
                vt = v.type
            End If
            dv = ReadNextData(vt)
            If errorCode Then Return GetErrorMessage()
            v.value = dv.value
        Next
        Return String.Empty
    End Function

    Protected Function ReadNextData(ByRef expectedType As VarType) As Variable
        If currDataLine Is Nothing Then
            SetError(ErrorId.DATA_ERROR)
            Return Nothing
        End If
        If currDataIndex = 0 Then
            Dim ha As HeaderAndArguments = GetHeaderAndArguments(currDataLine.content, ",")
            If ha.argumentsInBrackets Then
                SetError(ErrorId.INCORRECT_STATEMENT)
                Return Nothing
            End If
            currDataArgs = ha.args
        End If
        Dim expr As String = currDataArgs(currDataIndex)

        currDataIndex += 1
        If currDataIndex >= currDataArgs.Length Then ' argomenti di linea esauriti
            currDataLine = FindCodeLineWithHeader(IstrId.DATA, currDataLine.nextLine)
            currDataIndex = 0
        End If

        Dim vres As Variable
        If expectedType = VarType.FLOAT Then
            vres = Parser.ExtractNumericConstant(expr)
            If vres Is Nothing Then SetError(ErrorId.STR_NUM_MISMATCH)
        Else
            vres = Parser.ExtractStringConstant(expr)
            If vres Is Nothing Then vres = New Variable(VarType.STRNG, expr)
        End If
        Return vres
    End Function

    Protected Function ISTR_Restore(params As String) As String
        Dim ln As Integer
        'If dataLineSet.Count = 0 Then Return SetError(ErrorId.DATA_ERROR)
        If String.IsNullOrEmpty(params) Then
            'currDataLine = dataLineSet.First()
            currDataLine = FindCodeLineWithHeader(IstrId.DATA, code.GetFirstLine())
        Else
            Try
                ln = Convert.ToInt32(params)
            Catch ex As Exception
                Return SetError(ErrorId.BAD_LINE_NUMBER)
            End Try
            currDataLine = code.GetLine(ln)
            If currDataLine Is Nothing Then currDataLine = FindCodeLineWithHeader(IstrId.DATA, currDataLine.nextLine)
            If currDataLine Is Nothing Then Return SetError(ErrorId.DATA_ERROR)
        End If
        currDataIndex = 0
        Return String.Empty
    End Function

    Protected Function ISTR_Random(params As String) As String
        If String.IsNullOrEmpty(params) Then
            rnd = New Random()
        Else
            Dim var As Variable = EvalExpression(params)
            If errorCode Then Return GetErrorMessage()
            rnd = New Random(var.GetDouble())
        End If
        Return String.Empty
    End Function

    Protected Function ISTR_Input(params As String) As String
        Dim hdr As String = chdr & ".ISTR_Input"
        If params.Chars(0) = "#"c Then Return SetError(ErrorId.UNIMPLEMENTED)

        Dim promptStr As String = "? "
        Dim varList As String
        Dim i As Integer = params.IndexOf(":")
        If i > 0 Then
            promptStr = params.Substring(0, i)
            varList = params.Substring(i + 1)
        ElseIf i = 0 Then
            Return SetError(ErrorId.INCORRECT_STATEMENT)
        Else
            varList = params
        End If
        Dim var_list As String() = varList.Split(",")
        Dim value_list As String()
        Dim inpErr As Boolean = False

        ' arguments sequence read trial
        Do
            ' text input
            'InputCompletedEvent.Reset()
            runtimeTextInputReady = False
            inputStr = String.Empty
            cnsl.StartTextInput(promptStr) ' will affect global variable inputStr
            'InputCompletedEvent.WaitOne()
            While Not runtimeTextInputReady
                Application.DoEvents()
                Threading.Thread.Sleep(100)
            End While
            cnsl.NewLine()

            value_list = inputStr.Split(",")
            If value_list.Length <> var_list.Length Then
                inpErr = True
            Else
                ' variables assignment
                Dim v As Variable
                Dim vt As VarType
                For i = 0 To var_list.Length - 1
                    v = variables.GetVariable(var_list(i))
                    If v Is Nothing Then
                        vt = GetVarTypeFromName(var_list(i))
                        v = New Variable(var_list(i), vt)
                        variables.Add(v)
                    Else
                        vt = v.type
                    End If
                    If vt = VarType.FLOAT Then
                        If Not Information.IsNumeric(value_list(i)) Then
                            inpErr = True
                            Exit For
                        End If
                        Try
                            v.value = Convert.ToDouble(value_list(i))
                        Catch ex As Exception
                            Warn(hdr, "Bad float value '" & value_list(i) & "'")
                            v.value = 0.0
                        End Try
                    Else
                        v.value = New String(value_list(i))
                    End If
                Next
                If i = var_list.Length And Not inpErr Then Exit Do
            End If
            If inpErr Then
                SetWarning(WarnId.INPUT_ERROR)
                promptStr = "TRY AGAIN: "
                inpErr = False
            End If
        Loop

        Return String.Empty
    End Function

    Protected Function ISTR_Def(params As String) As String
        ' TODO
        Return SetError(ErrorId.UNIMPLEMENTED)
    End Function

    Protected Function ISTR_Call_(params As String) As String
        Dim i, v1, v2, v3 As Integer
        Dim hdr As String = chdr & ".ISTR_Call"

        ' estrae sottostringhe: nome routine e argomenti tra parentesi
        Dim parts As HeaderAndArguments = Parser.GetHeaderAndArguments(params)
        Dim args As Variable()

        With parts
            If String.IsNullOrEmpty(.header) Then Return SetError(ErrorId.INCORRECT_STATEMENT)
            If .argn > 0 And Not .argumentsInBrackets Then Return SetError(ErrorId.INCORRECT_STATEMENT)
            args = EvalMultipleExpression(parts.args)
            Select Case .header
                Case "CLEAR"
                    cnsl.Clear()
                Case "SCREEN"
                    If .argn <> 1 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                    v1 = args(0).GetInteger()
                    If v1 < 1 Or v1 > 16 Then Return SetError(ErrorId.BAD_VALUE)
                    machine.video.SetScreenColor(v1)
                Case "COLOR"
                    If .argn < 3 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                    For i = 0 To .argn - 1
                        If args(i).type <> VarType.FLOAT Then Return SetError(ErrorId.STR_NUM_MISMATCH)
                    Next
                    v1 = args(0).GetInteger()
                    v2 = args(1).GetInteger()
                    v3 = args(2).GetInteger()
                    machine.video.SetGroupColor(v1, v2, v3)
                Case "HCHAR"
                    If .argn < 3 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                    For i = 0 To .argn - 1
                        If args(i).type <> VarType.FLOAT Then Return SetError(ErrorId.STR_NUM_MISMATCH)
                    Next
                    v1 = args(0).GetInteger()
                    v2 = args(1).GetInteger()
                    v3 = args(2).GetInteger() Mod 255
                    If .argn = 3 Then
                        Call_HChar(v1, v2, v3)
                    Else
                        Call_HChar(v1, v2, v3, args(3).GetInteger())
                    End If
                Case "VCHAR"
                    If .argn < 3 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                    For i = 0 To .argn - 1
                        If args(i).type <> VarType.FLOAT Then Return SetError(ErrorId.STR_NUM_MISMATCH)
                    Next
                    v1 = args(0).GetInteger()
                    v2 = args(1).GetInteger()
                    v3 = args(2).GetInteger() Mod 255
                    If .argn = 3 Then
                        Call_VChar(v1, v2, v3)
                    Else
                        Call_VChar(v1, v2, v3, args(3).GetInteger())
                    End If
                Case "GCHAR"
                    If .argn <> 3 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                    Dim varName As String = .args(2)
                    If GetVarTypeFromName(varName) = VarType.STRNG Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                    For i = 0 To 1
                        If args(i).type <> VarType.FLOAT Then Return SetError(ErrorId.STR_NUM_MISMATCH)
                    Next
                    v1 = args(0).GetInteger()
                    v2 = args(1).GetInteger()
                    v3 = machine.video.GetChar(args(0).value, args(1).value)
                    variables.SetValue(varName, v3, True)
                Case "CHAR"
                    If .argn <> 2 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                    If args(0).type <> VarType.FLOAT Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                    If args(1).type <> VarType.STRNG Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                    v1 = args(0).GetInteger()
                    Dim hexs As String = args(1).GetString()
                    Dim nybbles(15) As Byte
                    For i = 0 To 15
                        If i < hexs.Length Then nybbles(i) = Convert.ToByte(hexs.Chars(i), 16) Else nybbles(i) = 0
                    Next
                    Dim bytes(7) As Byte
                    For i = 0 To 7
                        bytes(i) = (nybbles(i * 2) << 4) + nybbles(i * 2 + 1)
                    Next
                    machine.video.SetCharShape(Convert.ToByte(v1), bytes, True)
                    'If Not running Then machine.video.RestoreDefaultCharShapes(True)
                Case "SOUND"
                    If .argn < 3 Or .argn > 9 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                    'If Not .onlyNumericArguments Then Return SetError(ErrorId.STR_NUM_MISMATCH)
                    Dim dur As Integer = args(0).GetInteger()
                    Dim d As Integer = dur
                    If d < 0 Then d = -d
                    If d < TiSound.MIN_TIME Or d > TiSound.MAX_TIME Then Return SetError(ErrorId.BAD_VALUE)
                    Dim vParams((.argn - 2) >> 1) As VoiceParameter
                    Dim n As Integer = 0
                    i = 1
                    Do ' legge coppie di parametri [frequenza, volume]
                        vParams(n).frequency = args(i).GetInteger()
                        i += 1
                        If i = .argn Then Return SetError(ErrorId.INCORRECT_STATEMENT) ' missing volume
                        vParams(n).volume = args(i).GetInteger()
                        i += 1
                        n += 1
                    Loop While i < .argn
                    Call_Sound(dur, vParams)
                    If errorCode Then Return GetErrorMessage()
                Case "KEY"
                    If .argn <> 3 Then Return SetError(ErrorId.INCORRECT_STATEMENT)
                    Dim mode As Integer
                    Try
                        mode = Convert.ToInt32(.args(0))
                    Catch ex As Exception
                        Return SetError(ErrorId.INCORRECT_STATEMENT)
                    End Try
                    Dim keyVarName As String = .args(1)
                    Dim statusVarName As String = .args(2)
                    Call_Key(mode, keyVarName, statusVarName)
                    If errorCode Then Return GetErrorMessage()
                Case "JOYST"
                    ' TODO
                    SetWarning(WarnId.UNIMPLEMENTED)
                Case Else
                    Return SetError(ErrorId.INCORRECT_STATEMENT)
            End Select
        End With
        Return Nothing
    End Function

    ' TODO: mode implementation
    Protected Sub Call_Key(ByRef mode As Integer, ByRef keyVarName As String, ByRef statusVarName As String)
        Dim hdr As String = chdr & ".Call_Key"
        If mode < 0 Or mode > 5 Then
            SetError(ErrorId.BAD_VALUE)
            Return
        End If
        If String.IsNullOrEmpty(keyVarName) Then
            SetError(ErrorId.INCORRECT_STATEMENT)
            Return
        End If
        If GetVarTypeFromName(keyVarName) <> VarType.FLOAT Then
            SetError(ErrorId.INCORRECT_STATEMENT)
            Return
        End If
        If String.IsNullOrEmpty(statusVarName) Then
            SetError(ErrorId.INCORRECT_STATEMENT)
            Return
        End If
        If GetVarTypeFromName(statusVarName) <> VarType.FLOAT Then
            SetError(ErrorId.INCORRECT_STATEMENT)
            Return
        End If
        Dim key As Byte = cnsl.GetKeyPressed()
        If Not variables.SetValue(keyVarName, CInt(key), True) Then
            Warn(hdr, "troubles creating numeric variable " & keyVarName)
            SetError(ErrorId.INCORRECT_STATEMENT)
            Return
        End If
        Dim status As Integer
        If key = 0 Then
            status = 0
        Else
            If key = lastKey Then status = -1 Else status = 1
        End If
        If Not variables.SetValue(statusVarName, status, True) Then
            Warn(hdr, "troubles creating numeric variable " & statusVarName)
            SetError(ErrorId.INCORRECT_STATEMENT)
            Return
        End If
        lastKey = key
    End Sub

    Protected Sub Call_Sound(ByRef duration As Integer, ByRef params As VoiceParameter())
        Dim vCount As Integer = 1 + ((params.Length - 1) >> 1)
        If vCount < 1 Then Return
        Dim waitSoundEnd As Boolean = True
        Dim wForm As WaveForm
        Dim refFreq, freq As Integer
        If duration < 0 Then
            duration = -duration
            waitSoundEnd = False
        End If
        refFreq = 5 ' default value for noise frequency
        For i = 0 To vCount - 1
            freq = params(i).frequency
            If i = 2 Then refFreq = freq
            If freq < 0 Then
                If freq < -4 Then ' values -1, -2, -3, -4
                    wForm = WaveForm.TRI
                    If freq = -4 Then freq = refFreq Else freq = 233 * (5 + freq)
                ElseIf freq < -8 Then ' values -5, -6, -7, -8
                    wForm = WaveForm.NOISE
                    If freq = -8 Then freq = refFreq Else freq = 233 * (9 + freq)
                Else ' other negative values
                    SetError(ErrorId.BAD_VALUE)
                    Return
                End If
            Else ' positive values
                If freq < TiSound.MIN_FREQ Or freq > TiSound.MAX_FREQ Then
                    SetError(ErrorId.BAD_VALUE)
                    Return
                End If
                wForm = WaveForm.TRI
            End If
            machine.sound.PlayTone(i, wForm, freq, duration, params(i).volume)
        Next
        If waitSoundEnd Then Threading.Thread.Sleep(duration)
    End Sub

    Protected Sub Call_HChar(row As Integer, col As Integer, chr As Byte, Optional rep As Integer = 1)
        Dim i As Integer = 0
        row -= 1
        col -= 1
        While i < rep
            machine.video.PutChar(row, col, chr)
            col += 1
            If col > 31 Then
                row += 1
                If row > 23 Then Return
                col = 0
            End If
            i += 1
        End While
        machine.video.Invalidate()
    End Sub

    Protected Sub Call_VChar(row As Integer, col As Integer, chr As Byte, Optional rep As Integer = 1)
        Dim i As Integer = 0
        row -= 1
        col -= 1
        While i < rep
            machine.video.PutChar(row, col, chr)
            row += 1
            If row > 23 Then
                col += 1
                If col > 31 Then Return
                row = 0
            End If
            i += 1
        End While
        machine.video.Invalidate()
    End Sub

#End Region

#Region "Operators"

    Protected Function OPER_Product(arg1 As Variable, arg2 As Variable) As Variable
        Dim v1 As Double = Convert.ToDouble(arg1.value)
        Dim v2 As Double = Convert.ToDouble(arg2.value)
        Return New Variable(v1 * v2)
    End Function

    Protected Function OPER_Division(arg1 As Variable, arg2 As Variable) As Variable
        Dim v1 As Double = Convert.ToDouble(arg1.value)
        Dim v2 As Double = Convert.ToDouble(arg2.value)
        If v2 = 0.0 Then
            SetError(WarnId.NUMBER_TOO_BIG)
            Return New Variable(MAX_FLOAT)
        End If
        Return New Variable(v1 / v2)
    End Function

    Protected Function OPER_Sum(arg1 As Variable, arg2 As Variable) As Variable
        Dim v1, v2 As Double
        v2 = Convert.ToDouble(arg2.value)
        If arg1 Is Nothing Then Return New Variable(v2)
        v1 = Convert.ToDouble(arg1.value)
        Return New Variable(v1 + v2)
    End Function

    Protected Function OPER_Subtraction(arg1 As Variable, arg2 As Variable) As Variable
        Dim v1, v2 As Double
        v2 = Convert.ToDouble(arg2.value)
        If arg1 Is Nothing Then Return New Variable(-v2)
        v1 = Convert.ToDouble(arg1.value)
        Return New Variable(v1 - v2)
    End Function

    Protected Function OPER_Power(arg1 As Variable, arg2 As Variable) As Variable
        Dim v1 As Double = Convert.ToDouble(arg1.value)
        Dim v2 As Double = Convert.ToDouble(arg2.value)
        Return New Variable(v1 ^ v2)
    End Function

    Protected Function OPER_EqualsTo(arg1 As Variable, arg2 As Variable) As Variable
        Dim rval As Integer = 0
        If arg1.type = VarType.STRNG Then
            Dim s1 As String = CType(arg1.value, String)
            Dim s2 As String = CType(arg2.value, String)
            If s1 = s2 Then rval = -1
        Else
            Dim v1 As Double = Convert.ToDouble(arg1.value)
            Dim v2 As Double = Convert.ToDouble(arg2.value)
            If v1 = v2 Then rval = -1
        End If
        Return New Variable(rval)
    End Function

    Protected Function OPER_DiffersFrom(arg1 As Variable, arg2 As Variable) As Variable
        Dim rval As Integer = 0
        Select Case arg1.type
            Case VarType.STRNG
                Dim s1 As String = CType(arg1.value, String)
                Dim s2 As String = CType(arg2.value, String)
                If s1 <> s2 Then rval = -1
            Case VarType.FLOAT
                Dim v1 As Double = Convert.ToDouble(arg1.value)
                Dim v2 As Double = Convert.ToDouble(arg2.value)
                If v1 <> v2 Then rval = -1
            Case Else ' varType.UNDEF
                SetError(ErrorId.STR_NUM_MISMATCH)
                Return Nothing
        End Select
        Return New Variable(rval)
    End Function

    Protected Function OPER_LessThan(arg1 As Variable, arg2 As Variable) As Variable
        Dim rval As Integer = 0
        Select Case arg1.type
            Case VarType.STRNG
                Dim s1 As String = CType(arg1.value, String)
                Dim s2 As String = CType(arg2.value, String)
                If s1 < s2 Then rval = -1
            Case VarType.FLOAT
                Dim v1 As Double = Convert.ToDouble(arg1.value)
                Dim v2 As Double = Convert.ToDouble(arg2.value)
                If v1 < v2 Then rval = -1
            Case Else ' varType.UNDEF
                SetError(ErrorId.STR_NUM_MISMATCH)
                Return Nothing
        End Select
        'Return New Variable(VarType.INTGR, rval)
        Return New Variable(rval)
    End Function

    Protected Function OPER_MoreThan(arg1 As Variable, arg2 As Variable) As Variable
        Dim rval As Integer = 0
        Select Case arg1.type
            Case VarType.STRNG
                Dim s1 As String = CType(arg1.value, String)
                Dim s2 As String = CType(arg2.value, String)
                If s1 > s2 Then rval = -1
            Case VarType.FLOAT
                Dim v1 As Double = Convert.ToDouble(arg1.value)
                Dim v2 As Double = Convert.ToDouble(arg2.value)
                If v1 > v2 Then rval = -1
            Case Else ' varType.UNDEF
                SetError(ErrorId.STR_NUM_MISMATCH)
                Return Nothing
        End Select
        Return New Variable(rval)
    End Function

    Protected Function OPER_LessOrEqualThan(arg1 As Variable, arg2 As Variable) As Variable
        Dim rval As Integer = 0
        Select Case arg1.type
            Case VarType.STRNG
                Dim s1 As String = CType(arg1.value, String)
                Dim s2 As String = CType(arg2.value, String)
                If s1 <= s2 Then rval = -1
            Case VarType.FLOAT
                Dim v1 As Double = Convert.ToDouble(arg1.value)
                Dim v2 As Double = Convert.ToDouble(arg2.value)
                If v1 <= v2 Then rval = -1
            Case Else ' varType.UNDEF
                SetError(ErrorId.STR_NUM_MISMATCH)
                Return Nothing
        End Select
        Return New Variable(rval)
    End Function

    Protected Function OPER_MoreOrEqualThan(arg1 As Variable, arg2 As Variable) As Variable
        Dim rval As Integer = 0
        Select Case arg1.type
            Case VarType.STRNG
                Dim s1 As String = CType(arg1.value, String)
                Dim s2 As String = CType(arg2.value, String)
                If s1 >= s2 Then rval = -1
            Case VarType.FLOAT
                Dim v1 As Double = Convert.ToDouble(arg1.value)
                Dim v2 As Double = Convert.ToDouble(arg2.value)
                If v1 >= v2 Then rval = -1
            Case Else ' varType.UNDEF
                SetError(ErrorId.STR_NUM_MISMATCH)
                Return Nothing
        End Select
        Return New Variable(rval)
    End Function

    Protected Function OPER_Concat(arg1 As Variable, arg2 As Variable) As Variable
        Dim s1 As String = CType(arg1.value, String)
        Dim s2 As String = CType(arg2.value, String)
        Return New Variable(s1 & s2)
    End Function

#End Region

#Region "Functions"
    Private Function FUN_Sgn(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then
            Return SetError(ErrorId.STR_NUM_MISMATCH)
        End If
        If v.value > 0 Then
            v.value = 1.0
        ElseIf v.value < 0 Then
            v.value = -1.0
        Else
            v.value = 0.0
        End If
        functionResult.CopyContentOf(v)
        Return Nothing
    End Function

    Private Function FUN_Abs(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        If v.value < 0 Then v.value = -v.value
        functionResult.CopyContentOf(v)
        Return Nothing
    End Function

    Private Function FUN_Int(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        v.value = Int(CType(v.value, Single))
        functionResult.CopyContentOf(v)
        Return Nothing
    End Function

    Private Function FUN_Atn(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then
            Return SetError(ErrorId.STR_NUM_MISMATCH)
        End If
        v.value = Math.Atan(v.value)
        functionResult.CopyContentOf(v)
        Return Nothing
    End Function

    Private Function FUN_Len(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.FLOAT Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        'functionResult = New Variable(VarType.FLOAT, CType(v.value, String).Length)
        functionResult.SetSimpleVarContent(VarType.FLOAT, v.ValueToString().Length)
        Return Nothing
    End Function

    Private Function FUN_Chr(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        'functionResult = New Variable(VarType.STRNG, Chr(v.value).ToString)
        functionResult.SetSimpleVarContent(VarType.STRNG, Chr(v.GetInteger()).ToString)
        Return Nothing
    End Function

    Private Function FUN_Asc(arg As String) As String
        If String.IsNullOrEmpty(arg) Then Return SetError(ErrorId.BAD_ARGUMENT)
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.FLOAT Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        'functionResult = New Variable(VarType.FLOAT, Convert.ToDouble(Asc(v.GetString().Chars(0))))
        functionResult.SetSimpleVarContent(VarType.FLOAT, Asc(v.GetString().Chars(0)))
        Return Nothing
    End Function

    Private Function FUN_Cos(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        v.value = Math.Cos(v.GetDouble())
        functionResult.CopyContentOf(v)
        Return Nothing
    End Function

    Private Function FUN_Sin(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        v.value = Math.Sin(v.GetDouble())
        functionResult.CopyContentOf(v)
        Return Nothing
    End Function

    Private Function FUN_Tan(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        v.value = Math.Tan(v.GetDouble())
        functionResult.CopyContentOf(v)
        Return Nothing
    End Function

    Private Function FUN_Log(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        If v.GetDouble() = 0.0 Then Return SetError(ErrorId.BAD_VALUE)
        v.value = Math.Log(v.GetDouble())
        functionResult.CopyContentOf(v)
        Return Nothing
    End Function

    Private Function FUN_Exp(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        v.value = Math.Exp(v.GetDouble())
        functionResult.CopyContentOf(v)
        Return Nothing
    End Function

    Private Function FUN_Sqr(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        v.value = Math.Sqrt(v.GetDouble())
        functionResult.CopyContentOf(v)
        Return Nothing
    End Function

    Private Function FUN_Rnd(arg As String) As String
        functionResult.SetSimpleVarContent(VarType.FLOAT, rnd.NextDouble())
        Return Nothing
    End Function

    ' funzione atipica, solo per istruzione PRINT 
    Private Function FUN_Tab(arg As String) As String
        If Not printIstrEval Then Return SetError(ErrorId.INCORRECT_STATEMENT)
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        cnsl.Tab(v.GetInteger())
        functionResult.SetSimpleVarContent(VarType.STRNG, String.Empty)
        Return Nothing
    End Function

    Private Function FUN_Val(arg As String) As String
        Dim val As Double
        Try
            val = Convert.ToDouble(arg)
        Catch ex As Exception
            Return SetError(ErrorId.BAD_ARGUMENT)
        End Try
        'functionResult = New Variable(VarType.FLOAT, val)
        functionResult.SetSimpleVarContent(VarType.FLOAT, val)
        Return Nothing
    End Function

    Private Function FUN_Str(arg As String) As String
        Dim v As Variable = EvalExpression(arg)
        If errorCode Then Return GetErrorMessage()
        If v.type = VarType.STRNG Then Return SetError(ErrorId.STR_NUM_MISMATCH)
        'functionResult = New Variable(VarType.STRNG, v.GetString())
        functionResult.SetSimpleVarContent(VarType.STRNG, v.GetString())
        Return Nothing
    End Function

    Private Function FUN_Seg(arg As String) As String
        Dim args As Variable() = ExtractArgsFromString(arg, ","c, 3, "NNS")
        If errorCode Then Return GetErrorMessage()

        Dim str As String = args(0).GetString()
        If String.IsNullOrEmpty(str) Then Return SetError(ErrorId.BAD_ARGUMENT)

        Dim fromIdx, len As Integer
        fromIdx = args(1).GetInteger()
        len = args(2).GetInteger()
        If fromIdx > str.Length Or len = 0 Then Return String.Empty

        Dim v As New Variable(VarType.STRNG) With {.value = str.Substring(fromIdx - 1, len)}
        functionResult.CopyContentOf(v)
        Return Nothing
    End Function

    Private Function FUN_Pos(arg As String) As String
        Dim args As Variable() = ExtractArgsFromString(arg, ","c, 3, "SSN")
        If errorCode Then Return GetErrorMessage()

        Dim str1 As String = args(0).GetString()
        Dim str2 As String = args(1).GetString()
        Dim pos As Integer = args(2).GetInteger()
        If pos <= 0 Then Return SetError(ErrorId.BAD_VALUE)
        Dim v As New Variable(VarType.FLOAT)
        If pos >= str1.Length Then
            v.value = 0.0
        Else
            v.value = Convert.ToDouble(str1.IndexOf(str2, pos - 1))
        End If
        functionResult.CopyContentOf(v)
        Return Nothing
    End Function
#End Region

End Class