module Deep.Log

open System
open System.Globalization

type LoggerStatus =
| Success of string
| Warning of string
| Error of string

module LoggerStatuses =
    let ok = Success "OK"
    let passed = Success "PASSED"
    let success = Success "SUCCESS"
    let done' = Success "DONE"
    let finished = Success "FINISHED"
    let warning = Warning "WARNING"
    let error = Error "ERROR"
    let failed = Error "FAILED"

type ILogger =
    abstract member Log : string -> ILogger
    abstract member LogWith : string -> ILogger

    abstract member Status : string * ConsoleColor -> ILogger
    abstract member Status : LoggerStatus -> ILogger

    abstract member LogSuccess : string -> ILogger
    abstract member LogSuccess : string * string -> ILogger
    abstract member LogWarning : string -> ILogger
    abstract member LogWarning : string * string -> ILogger
    abstract member LogError : string -> ILogger
    abstract member LogError : string * string -> ILogger

type EmptyLogger() =
    interface ILogger with
        member l.Log(message) = l
        member l.LogWith(message) = l

        member l.Status (text : string, color : ConsoleColor) = l
        member l.Status (status : LoggerStatus) = l

        member l.LogSuccess(message) = l
        member l.LogSuccess(message, status : string) = l
        member l.LogWarning(message) = l 
        member l.LogWarning(message, status : string) = l
        member l.LogError(message) = l
        member l.LogError(message, status : string) = l
        

type ConsoleLogger() as l =

    let displayStatusText (color : ConsoleColor) (text : string) = 
        Console.ForegroundColor <- ConsoleColor.White
        Console.Write("[")
        Console.ForegroundColor <- ConsoleColor.Green
        Console.Write("{0}", text)
        Console.ForegroundColor <- ConsoleColor.White
        Console.WriteLine("]")
        Console.ResetColor()

    let displayStatus = function
    | Success text -> text |> displayStatusText ConsoleColor.Green
    | Warning text -> text |> displayStatusText ConsoleColor.Yellow
    | Error text -> text |> displayStatusText ConsoleColor.Red

    let logger = l :> ILogger

    let displayCurrentTime () =
        let currentColor = Console.ForegroundColor
        Console.ForegroundColor <- ConsoleColor.DarkGray
        let now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
        Console.Write($"{now}\t\t")
        Console.ForegroundColor <- currentColor

    interface ILogger with
        member l.Log(message) =
            displayCurrentTime()
            Console.WriteLine(message)
            l

        member l.LogWith(message) =
            displayCurrentTime()
            Console.Write($"{message}\t\t")
            l

        member l.Status(text : string, color : ConsoleColor) =
            text |> displayStatusText color 
            l

        member l.Status(status) =
            displayStatus status
            l

        member l.LogSuccess(message) =
            logger.LogWith(message).Status(LoggerStatuses.success)

        member l.LogSuccess(message, status : string) =
            logger.LogWith(message).Status(Success status)

        member l.LogWarning(message) =
            logger.LogWith(message).Status(LoggerStatuses.warning)

        member l.LogWarning(message, status : string) =
            logger.LogWith(message).Status(Warning status)

        member l.LogError(message) =
            logger.LogWith(message).Status(LoggerStatuses.error)

        member l.LogError(message, status : string) =
            logger.LogWith(message).Status(Error status)