[<AutoOpen>]
module Deep.AsyncExtensions

open System.Threading.Tasks
open Microsoft.FSharp

type Control.AsyncBuilder with

    member async.Bind(t : Task<'T>, f : 'T -> Async<'R>) : Async<'R> = 
        async.Bind(Async.AwaitTask t, f)

    member async.Bind(t : Task, f : unit -> Async<unit>) : Async<unit> =
        async.Bind(Async.AwaitTask t, f)

    member async.ReturnFrom(t : Task<'T>) : Async<'T> =
        async.ReturnFrom(Async.AwaitTask t)

    member async.ReturnFrom(t : Task) : Async<unit> =
        async.ReturnFrom(Async.AwaitTask t)