'
' Visual Basic .NET Parser
'
' Copyright (C) 2004, Microsoft Corporation. All rights reserved.
'
' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
' EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
' MERCHANTIBILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
'

''' <summary>
''' A character that denotes the type of something.
''' </summary>
Public Enum TypeCharacter
    ''' <summary>No type character</summary>
    None

    ''' <summary>The String symbol '$'.</summary>
    StringSymbol

    ''' <summary>The Integer symbol '%'.</summary>
    IntegerSymbol

    ''' <summary>The Long symbol '&amp;'.</summary>
    LongSymbol

    ''' <summary>The Short character 'S'.</summary>
    ShortChar

    ''' <summary>The Integer character 'I'.</summary>
    IntegerChar

    ''' <summary>The Long character 'L'.</summary>
    LongChar

    ''' <summary>The Single symbol '!'.</summary>
    SingleSymbol

    ''' <summary>The Double symbol '#'.</summary>
    DoubleSymbol

    ''' <summary>The Decimal symbol '@'.</summary>
    DecimalSymbol

    ''' <summary>The Single character 'F'.</summary>
    SingleChar

    ''' <summary>The Double character 'R'.</summary>
    DoubleChar

    ''' <summary>The Decimal character 'D'.</summary>
    DecimalChar
End Enum