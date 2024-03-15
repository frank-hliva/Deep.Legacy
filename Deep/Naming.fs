module Deep.Naming

open System.IO
open System

type TNameFinder = string -> bool

let uniqueName (findName : TNameFinder) (originalName : string) =

    let nameByCounter originalName = function
    | 0 -> originalName
    | counter -> sprintf "%s-%i" originalName counter

    let rec uniqueName (findName : TNameFinder) (counter : int) (originalName : string) =
        let newName = counter |> nameByCounter originalName
        if newName |> findName
        then originalName |> uniqueName findName (counter + 1)
        else newName
    
    originalName |> uniqueName findName 0

type TFileNameFinder = string -> string -> bool

let uniqueFileName (findName : TFileNameFinder) (originalFileName : string) =
    let name = originalFileName |> Path.GetFileNameWithoutExtension
    let ext = originalFileName |> Path.GetExtension
    sprintf "%s%s" (name |> uniqueName (findName ext)) ext