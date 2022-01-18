'Imports System.Runtime.InteropServices
'Imports CoreAudio
Imports NAudio.Wave

Public Enum WaveForm
    SQR = SampleProviders.SignalGeneratorType.Square
    SIN = SampleProviders.SignalGeneratorType.Sin
    TRI = SampleProviders.SignalGeneratorType.Triangle
    SWT = SampleProviders.SignalGeneratorType.SawTooth
    NOISE = SampleProviders.SignalGeneratorType.White
End Enum
Public Structure VoiceParameter
    Dim frequency As Integer
    Dim volume As Byte
End Structure

Public Class TiSound
    Implements IDisposable

    Public Const MIN_VOL As Byte = 30
    Public Const MAX_VOL As Byte = 0
    Public Const MIN_FREQ As Integer = 110
    Public Const MAX_FREQ As Integer = 15000
    Public Const MIN_TIME As Integer = 1
    Public Const MAX_TIME As Integer = 4250

    Protected Const SMP_RATE As Integer = 44100
    Protected Const BEEP_FREQ As Integer = 1400
    Protected Const BEEP_TIME As Integer = 160
    Protected Const ERROR_BEEP_FREQ As Integer = 220
    Protected Const ERROR_BEEP_TIME As Integer = 160
    Protected Const BASE_VOLUME As Single = 0.4

    Protected wavePlayer(3) As WaveOutEvent
    Protected rnd As Random
    Private disposedValue As Boolean

    Sub New()
        For i As Integer = 0 To 3
            wavePlayer(i) = New WaveOutEvent()
        Next i
        rnd = New Random()
    End Sub

    ''' <summary>
    ''' Play a tone with specified parameters on the specified "voice".
    ''' There are 4 available voices, numbered from 0 to 3.
    ''' </summary>
    ''' <param name="voiceNum">voice number: 0 to 3 included</param>
    ''' <param name="wForm">wave form type</param>
    ''' <param name="freq">signal frerquency</param>
    ''' <param name="dur">duration [millisecs]</param>
    ''' <param name="vol">volume: 0 (maximum) to MIN_VOL (minimum)</param>
    Public Sub PlayTone(ByRef voiceNum As Integer,
                        ByRef wForm As WaveForm,
                        ByRef freq As Integer,
                        ByRef dur As Integer,
                        Optional vol As Byte = 0)

        If voiceNum < 0 Or voiceNum > 3 Then Return
        If freq < MIN_FREQ Or freq > MAX_FREQ Then Return
        If vol > MIN_VOL Then vol = MIN_VOL

        Dim sg As New SampleProviders.SignalGenerator()
        With sg
            .Frequency = freq
            .Gain = (CSng(MIN_VOL - vol) / CSng(MIN_VOL)) * BASE_VOLUME
            .Type = wForm
        End With
        Dim wp As WaveOutEvent = wavePlayer(voiceNum)
        If wp.PlaybackState = PlaybackState.Playing Then wp.Stop()
        wp.Init(sg.Take(TimeSpan.FromMilliseconds(dur)))
        wp.Play()
    End Sub

    Public Sub StopAllTones()
        For i As Integer = 0 To 3
            'If wavePlayer(i).PlaybackState = PlaybackState.Playing Then wavePlayer(i).Stop()
            wavePlayer(i).Stop()
        Next
    End Sub

    Public Sub Tone(frequency As Integer, duration As Integer)
        PlayTone(0, WaveForm.TRI, frequency, duration)
    End Sub

    Public Sub Beep()
        PlayTone(0, WaveForm.TRI, BEEP_FREQ, BEEP_TIME)
    End Sub

    Public Sub ErrorBeep()
        PlayTone(0, WaveForm.SQR, ERROR_BEEP_FREQ, ERROR_BEEP_TIME)
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: eliminare lo stato gestito (oggetti gestiti)
                For i As Integer = 0 To 3
                    wavePlayer(i).Dispose()
                Next i
            End If

            ' TODO: liberare risorse non gestite (oggetti non gestiti) ed eseguire l'override del finalizzatore
            ' TODO: impostare campi di grandi dimensioni su Null
            disposedValue = True
        End If
    End Sub

    '

    ' ' TODO: eseguire l'override del finalizzatore solo se 'Dispose(disposing As Boolean)' contiene codice per liberare risorse non gestite
    ' Protected Overrides Sub Finalize()
    '     ' Non modificare questo codice. Inserire il codice di pulizia nel metodo 'Dispose(disposing As Boolean)'
    '     Dispose(disposing:=False)
    '     MyBase.Finalize()
    ' End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Non modificare questo codice. Inserire il codice di pulizia nel metodo 'Dispose(disposing As Boolean)'
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub
End Class


