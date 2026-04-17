' ==========================================================
' YouTube System V6.3 - Fixed Hebrew Encoding
' ==========================================================

Option Explicit

Dim http, action, url, encodedAction

If WScript.Arguments.Count = 0 Then WScript.Quit

action = WScript.Arguments(0)
encodedAction = UTF8EncodeForUrl(action)
url = "http://localhost:3000/" & encodedAction

On Error Resume Next
Set http = CreateObject("MSXML2.XMLHTTP")
http.Open "GET", url, False
http.Send
Set http = Nothing
WScript.Quit

' פונקציה חדשה וחסינה לקידוד עברית ב-UTF8
Function UTF8EncodeForUrl(sStr)
    Dim x, hexVal, asciiCode, out
    For x = 1 To Len(sStr)
        asciiCode = AscW(Mid(sStr, x, 1))
        If asciiCode < 0 Then asciiCode = asciiCode + 65536
        
        ' תווים בטוחים (A-Z, a-z, 0-9)
        If (asciiCode >= 48 And asciiCode <= 57) Or _
           (asciiCode >= 65 And asciiCode <= 90) Or _
           (asciiCode >= 97 And asciiCode <= 122) Then
            out = out & ChrW(asciiCode)
        Else
            ' טיפול ברווחים
            If asciiCode = 32 Then
                out = out & "%20"
            Else
                ' קידוד UTF-8 רב-בייטי לעברית
                hexVal = Hex(asciiCode)
                If asciiCode < &H80 Then
                    out = out & "%" & Right("0" & Hex(asciiCode), 2)
                ElseIf asciiCode < &H800 Then
                    out = out & "%" & Hex(&HC0 Or (asciiCode \ &H40))
                    out = out & "%" & Hex(&H80 Or (asciiCode And &H3F))
                Else
                    out = out & "%" & Hex(&HE0 Or (asciiCode \ &H1000))
                    out = out & "%" & Hex(&H80 Or ((asciiCode \ &H40) And &H3F))
                    out = out & "%" & Hex(&H80 Or (asciiCode And &H3F))
                End If
            End If
        End If
    Next
    UTF8EncodeForUrl = out
End Function